using System;
using System.Collections;
using Cutscenes;
using UnityEngine;

/// <summary>
/// Central controller for the Holmgang game mode (Norse 1v1 duel).
///
/// Required singletons: PlayerController, WoundManager, WeaponDatabase, JarlManager
/// Recommended        : CutsceneManager (letterbox), DialogueManager (pre-fight lines)
///
/// Per-opponent CutsceneSO workflow (recommended — author in editor):
///   Reference the spawned opponent as: Type = SceneObject, Identifier = "HolmgangOpponent"
///   HolmgangManager names the spawned GameObject that before playing the cutscene.
///   Leave introductionCutscene null to use the procedural fallback instead.
/// </summary>
public class HolmgangManager : MonoBehaviour
{
    public static HolmgangManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private GameObject villagerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform playerHolmgangPosition;

    [Header("Opponents")]
    [SerializeField] private OpponentConfig[] opponentTypes;
    [SerializeField] private Transform opponentHolmgangPosition;
    [Tooltip("Off-screen position the opponent walks in from. Defaults beside the holmgang position.")]
    [SerializeField] private Transform opponentStartPosition;

    [Header("UI")]
    [SerializeField] private HolmgangUI holmgangUI;

    [Header("Procedural Fallback Timing")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float arrivalThreshold = 0.35f;
    [SerializeField] private float letterboxWaitSeconds = 0.65f;
    [SerializeField] private float facingPauseSeconds = 0.4f;
    [SerializeField] private float betweenLinesSeconds = 0.5f;
    [SerializeField] private float preCombatPauseSeconds = 1.2f;

    public GameObject islandColliders;
    public GameObject arenaColliders;

    [System.Serializable]
    public class OpponentConfig
    {
        public string label = "Opponent";
        [TextArea] public string description = "";
        public GameObject prefab;

        [Tooltip("Authored cutscene asset for the walk-in and pre-fight dialogue. " +
                 "Reference the opponent via SceneObject 'HolmgangOpponent'. " +
                 "Leave null to use the procedural fallback.")]
        public CutsceneSO introductionCutscene;

        [Header("Procedural Fallback Lines")]
        [TextArea] public string playerChallengeLine = "I claim this ground by iron and blood!";
        [TextArea] public string opponentTauntLine   = "You will not leave this field alive.";
    }

    // ── State ────────────────────────────────────────────────────────────────────

    private enum MatchState { Idle, Cinematic, Fighting, Won, Lost }

    private Villager    _player;
    private Enemy       _opponent;
    private EnemyAIBase _opponentAI;
    private MatchState  _state = MatchState.Idle;
    private int        _wins;
    private int        _losses;

    public bool CanChallenge => _state == MatchState.Idle
                             || _state == MatchState.Won
                             || _state == MatchState.Lost;

    // ── Lifecycle ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Ensure JarlManager has its PlayerController reference wired up
        JarlManager.Instance?.Init();

        SpawnPlayer();
        InitUI();
        AttackCooldownUI.Instance?.Init();
    }

    private void InitUI()
    {
        if (holmgangUI == null) return;

        var names = new string[opponentTypes.Length];
        var descs  = new string[opponentTypes.Length];
        for (int i = 0; i < opponentTypes.Length; i++)
        {
            names[i] = opponentTypes[i].label;
            descs[i] = opponentTypes[i].description;
        }

        holmgangUI.Populate(names, descs);
        holmgangUI.OnOpponentSelected += BeginHolmgang;
    }

    // ── Player ───────────────────────────────────────────────────────────────────

    public void SpawnPlayer()
    {
        if (_player != null)
            Destroy(_player.gameObject);

        if (villagerPrefab == null) { Debug.LogError("[Holmgang] No villager prefab assigned."); return; }

        Vector3 pos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        var go = Instantiate(villagerPrefab, pos, Quaternion.identity);
        _player = go.GetComponent<Villager>();

        if (_player == null) { Debug.LogError("[Holmgang] Villager prefab has no Villager component."); return; }

        var ia = go.GetComponent<ItemAttachment>();
        if (ia != null) ia.GiveStartingWeapon();

        _player.ApplySkillBonuses();
        _player.Init();

        // JarlManager.SetJarl handles: jarl flags, OnJarlStatusChanged, PlayerController,
        // VillagerAI disable, and camera target — all in the correct order.
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.SetJarl(_player, isInitial: true);
        }
        else
        {
            // Fallback for scenes without JarlManager
            _player.isJarl = true;
            PlayerController.Instance?.SetControlTarget(_player);
        }

        _player.OnDeath += HandlePlayerDeath;
        _state = MatchState.Idle;
    }

    private void HandlePlayerDeath()
    {
        if (_state != MatchState.Fighting) return;
        _losses++;
        _state = MatchState.Lost;
        Debug.Log("[Holmgang] Player defeated.");
    }

    // ── Holmgang entry ───────────────────────────────────────────────────────────

    public void BeginHolmgang(int opponentIndex)
    {
        if (!CanChallenge) return;

        if (opponentIndex < 0 || opponentIndex >= opponentTypes.Length
            || opponentTypes[opponentIndex].prefab == null)
        {
            Debug.LogWarning($"[Holmgang] Opponent index {opponentIndex} invalid or missing prefab.");
            return;
        }

        ClearOpponent();
        StartCoroutine(HolmgangSequence(opponentTypes[opponentIndex]));
    }

    // ── Shared setup ─────────────────────────────────────────────────────────────

    private IEnumerator HolmgangSequence(OpponentConfig config)
    {
        _state = MatchState.Cinematic;

        // Spawn opponent off-stage; name it so CutsceneSO can reference it as SceneObject
        Vector3 spawnPos = opponentStartPosition != null
            ? opponentStartPosition.position
            : GetDefaultOpponentStartPos();

        var go = Instantiate(config.prefab, spawnPos, Quaternion.identity);
        go.name = "HolmgangOpponent";

        _opponent = go.GetComponent<Enemy>();
        if (_opponent == null)
        {
            Debug.LogError("[Holmgang] Opponent prefab has no Enemy component.");
            Destroy(go);
            _state = MatchState.Idle;
            yield break;
        }

        _opponent.InitializeEnemyStats();

        _opponentAI = go.GetComponent<EnemyAIBase>();
        if (_opponentAI != null) _opponentAI.enabled = false;

        if (config.introductionCutscene != null)
            yield return StartCoroutine(PlayAuthoredCutscene(config));
        else
            yield return StartCoroutine(ProceduralSequence(config, go));

        // Re-enable enemy AI — fight begins
        if (_opponentAI != null) _opponentAI.enabled = true;
        _opponent.OnDeath += HandleOpponentDeath;
        _state = MatchState.Fighting;

        SwapColliders(true);

        Debug.Log($"[Holmgang] Duel started vs {config.label}.");
    }

    private void SwapColliders(bool value)
    {
        islandColliders.SetActive(!value);
        arenaColliders.SetActive(value);
    }

    private void HandleOpponentDeath()
    {
        if (_state != MatchState.Fighting) return;
        _wins++;
        _state = MatchState.Won;
        _opponent = null;
        Debug.Log("[Holmgang] Victory!");
        SwapColliders(false);

    }

    // ── Authored CutsceneSO path ─────────────────────────────────────────────────

    private IEnumerator PlayAuthoredCutscene(OpponentConfig config)
    {
        if (CutsceneManager.Instance == null) yield break;

        // Register player so CutsceneManager clears their movement state on cutscene end
        CutsceneManager.Instance.RegisterOverriddenVillager(_player);

        bool cutsceneDone = false;
        void OnEnded(CutsceneSO _) => cutsceneDone = true;

        CutsceneManager.Instance.OnCutsceneEnded += OnEnded;
        CutsceneManager.Instance.PlayCutscene(config.introductionCutscene);

        yield return new WaitUntil(() => cutsceneDone);
        CutsceneManager.Instance.OnCutsceneEnded -= OnEnded;
    }

    // ── Procedural fallback path ─────────────────────────────────────────────────

    private IEnumerator ProceduralSequence(OpponentConfig config, GameObject opponentGO)
    {
        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.EnterCinematicMode();
        else
            PlayerController.Instance?.SetInputEnabled(false);

        yield return new WaitForSeconds(letterboxWaitSeconds);

        // Walk both characters to their positions simultaneously via direct movement.
        // The player's VillagerAI is already disabled by JarlManager/PlayerController,
        // so we move the transform directly rather than through VillagerAI.
        Vector3 playerTarget   = playerHolmgangPosition   != null ? playerHolmgangPosition.position   : _player.transform.position;
        Vector3 opponentTarget = opponentHolmgangPosition != null ? opponentHolmgangPosition.position : opponentGO.transform.position + new Vector3(-3f, 0f);

        bool playerArrived = false, opponentArrived = false;

        StartCoroutine(WalkTransformTo(_player.transform,   playerTarget,   () => playerArrived   = true));
        StartCoroutine(WalkTransformTo(opponentGO.transform, opponentTarget, () => opponentArrived = true));

        yield return new WaitUntil(() => playerArrived && opponentArrived);

        FaceEachOther(_player.gameObject, opponentGO);
        yield return new WaitForSeconds(facingPauseSeconds);

        yield return StartCoroutine(ShowLine(GetPlayerName(), config.playerChallengeLine));
        yield return new WaitForSeconds(betweenLinesSeconds);

        yield return StartCoroutine(ShowLine(config.label, config.opponentTauntLine));
        yield return new WaitForSeconds(preCombatPauseSeconds);

        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.ExitCinematicMode();
        else
            PlayerController.Instance?.SetInputEnabled(true);
    }

    // ── Movement ─────────────────────────────────────────────────────────────────

    private IEnumerator WalkTransformTo(Transform t, Vector3 target, Action onArrived)
    {
        while (Vector2.Distance(t.position, target) > arrivalThreshold)
        {
            t.position = Vector3.MoveTowards(t.position, target, walkSpeed * Time.deltaTime);
            yield return null;
        }
        onArrived?.Invoke();
    }

    // ── Facing ───────────────────────────────────────────────────────────────────

    private void FaceEachOther(GameObject a, GameObject b)
    {
        bool bIsRight = b.transform.position.x > a.transform.position.x;
        SetFacing(a, bIsRight);
        SetFacing(b, !bIsRight);
    }

    private void SetFacing(GameObject go, bool faceRight)
    {
        go.GetComponent<CharacterBase>()?.SetFacingRight(faceRight);
    }

    // ── Dialogue ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowLine(string speakerName, string line)
    {
        if (DialogueManager.Instance == null || string.IsNullOrEmpty(line)) yield break;

        bool done = false;
        var so = ScriptableObject.CreateInstance<DialogueSO>();
        so.lines = new[] { new DialogueLine { speakerName = speakerName, text = line } };
        so.offersQuest = false;

        DialogueManager.Instance.StartDialogue(so, onComplete: () => done = true);
        yield return new WaitUntil(() => done);

        Destroy(so);
    }

    // ── Utility ──────────────────────────────────────────────────────────────────

    private string GetPlayerName()
    {
        if (JarlManager.Instance?.CurrentJarl != null)
            return JarlManager.Instance.CurrentJarl.villagerName;
        return _player != null ? _player.villagerName : "Jarl";
    }

    private Vector3 GetDefaultOpponentStartPos()
    {
        Vector3 c = opponentHolmgangPosition != null ? opponentHolmgangPosition.position : Vector3.zero;
        return c + new Vector3(6f, 0f);
    }

    public void ClearOpponent()
    {
        if (_opponent != null)
        {
            _opponent.OnDeath -= HandleOpponentDeath;
            Destroy(_opponent.gameObject);
            _opponent = null;
        }

        foreach (var e in FindObjectsByType<Enemy>())
            Destroy(e.gameObject);

        _state = MatchState.Idle;
    }

    // ── Debug overlay ─────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 210, 100));
        GUILayout.BeginVertical("box");
        GUILayout.Label($"Holmgang  W:{_wins}  L:{_losses}  [{_state}]");
        if (CanChallenge)
        {
            if (GUILayout.Button("Respawn Player"))  SpawnPlayer();
            if (GUILayout.Button("Clear Opponent")) ClearOpponent();
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
