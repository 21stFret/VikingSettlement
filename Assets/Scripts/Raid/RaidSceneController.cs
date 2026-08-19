using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Controls the raid battle scene - spawning, victory conditions, timer
/// Attach to a manager object in your raid scene
/// </summary>
public class RaidSceneController : MonoBehaviour
{
    public static RaidSceneController Instance { get; private set; }

    [Header("Spawn Points")]
    [Tooltip("Where raid party members spawn")]
    public List<Transform> partySpawnPoints = new List<Transform>();

    [Tooltip("Where enemies spawn")]
    public List<Transform> enemySpawnPoints = new List<Transform>();

    [Header("Raid Templates")]
    public GameObject fishingVillagePrefab;
    public GameObject fishingSpawnParent;


    [Header("Raid State")]
    [SerializeField] private bool raidActive = false;
    [SerializeField] private int enemiesRemaining = 0;
    private int startingEnemyCount;
    [SerializeField] private int partyMembersAlive = 0;

    [Header("Player Control")]
    [Tooltip("Reference to PlayerController (will auto-find if not set)")]
    public PlayerController playerController;

    // Tracking
    private List<Villager> raidParty = new List<Villager>();
    private List<Enemy> spawnedEnemies = new List<Enemy>();
    private List<ResourceLoot> collectedLoot = new List<ResourceLoot>();
    private List<Villager> casualties = new List<Villager>();

    // Track exact delegate instances (rather than a single lambda) so each can be precisely
    // unsubscribed in OnDestroy without affecting the others.
    private readonly Dictionary<Villager, System.Action> _deathHandlers = new Dictionary<Villager, System.Action>();

    // Events
    public event System.Action OnRaidVictory;
    public event System.Action OnRaidDefeat;
    public event System.Action<int> OnEnemyCountChanged;
    public event System.Action<int> OnPartyCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        foreach (var kvp in _deathHandlers)
        {
            if (kvp.Key != null)
                kvp.Key.OnDeath -= kvp.Value;
        }
        _deathHandlers.Clear();
    }

    private void Start()
    {
        Debug.Log($"RaidSceneController.Start() - RaidManager exists: {RaidManager.Instance != null}");
        if (RaidManager.Instance != null)
        {
            Debug.Log($"RaidSceneController.Start() - IsOnRaid: {RaidManager.Instance.IsOnRaid}");
        }
        Debug.Log($"RaidSceneController.Start() - Spawn points: {partySpawnPoints?.Count ?? 0}, PlayerController assigned: {playerController != null}");

        // Auto-start raid when scene loads
        if (RaidManager.Instance != null && RaidManager.Instance.IsOnRaid)
        {
            StartBattle();
        }
        else
        {
            Debug.LogWarning("RaidSceneController: No active raid found. Testing mode?");
        }
    }

    private void Update()
    {
        if (!raidActive) return;

        // Check for timeout
        if (RaidManager.Instance != null && RaidManager.Instance.IsRaidTimeExpired())
        {
            Debug.Log("Raid timed out!");
            ForceRetreat();
        }
    }

    #region Battle Setup

    /// <summary>
    /// Initialize and start the battle
    /// </summary>
    public void StartBattle()
    {
        if (RaidManager.Instance == null)
        {
            Debug.LogError("No RaidManager found!");
            return;
        }

        raidActive = true;
        collectedLoot.Clear();
        casualties.Clear();

        // Get raid info
        var destination = RaidManager.Instance.CurrentRaid;

        // Spawn the raid destination template (e.g., fishing village)
        switch(destination.locationType)
        {
            case LocationType.Fishing:
                if (fishingVillagePrefab != null)
                {
                    fishingVillagePrefab.SetActive(true);
                    enemySpawnPoints.Clear();
                    enemySpawnPoints = fishingSpawnParent.GetComponentsInChildren<Transform>().ToList();
                }
                else
                {
                    Debug.LogWarning("No fishing village prefab assigned in RaidSceneController.");
                }
                break;
            // Add other location types as needed
            default:
                Debug.LogWarning($"Unknown location type: {destination.locationType}");
                break;
        }

        // Spawn party
        SpawnParty();

        // Spawn enemies
        SpawnEnemies(destination.enemyCount, destination.enemyPrefabs, destination.spawnAll);

        Debug.Log($"Battle started! Party: {partyMembersAlive}, Enemies: {enemiesRemaining}");
    }

    /// <summary>
    /// Spawn raid party at designated points. Respawns fresh Villager instances from
    /// RaidManager's party snapshot (rather than assuming they already exist as
    /// DontDestroyOnLoad'd GameObjects carried over from the previous scene) and wires up
    /// player control / raid-ally AI via RaidManager, shared logic for every scene that spawns
    /// the party.
    /// </summary>
    private void SpawnParty()
    {
        raidParty = RaidManager.Instance.SpawnPartyAtPoints(partySpawnPoints);
        Debug.Log($"SpawnParty() - raidParty count: {raidParty?.Count ?? 0}, spawn points: {partySpawnPoints?.Count ?? 0}");

        partyMembersAlive = 0;
        foreach (var villager in raidParty)
        {
            if (villager == null) continue;

            // Subscribe to death — store the exact delegate so it can be unsubscribed in OnDestroy
            System.Action deathHandler = () => OnPartyMemberDied(villager);
            _deathHandlers[villager] = deathHandler;
            villager.OnDeath += deathHandler;

            partyMembersAlive++;
        }

        RaidManager.Instance.AssignPlayerControl(raidParty, playerController);

        OnPartyCountChanged?.Invoke(partyMembersAlive);
    }

    /// <summary>
    /// Spawn enemies at designated points.
    /// spawnAll: one of each prefab in the list, ignoring count.
    /// !spawnAll: pick randomly from the list, count times.
    /// </summary>
    private void SpawnEnemies(int count, List<GameObject> prefabs, bool spawnAll)
    {
        spawnedEnemies.Clear();
        enemiesRemaining = 0;

        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogError("RaidSceneController: No enemy prefabs assigned on the raid destination.");
            return;
        }

        List<GameObject> toSpawn = spawnAll
            ? new List<GameObject>(prefabs)
            : BuildRandomList(prefabs, count);

        for (int i = 0; i < toSpawn.Count; i++)
        {
            Transform spawnPoint = enemySpawnPoints.Count > 0
                ? enemySpawnPoints[i % enemySpawnPoints.Count]
                : transform;

            Vector3 spawnPos = spawnPoint.position + new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0
            );

            GameObject enemyObj = Instantiate(toSpawn[i], spawnPos, Quaternion.identity);
            Enemy enemy = enemyObj.GetComponent<Enemy>();

            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
                enemiesRemaining++;
                enemy.OnDeath += () => OnEnemyDied(enemy);
            }
        }

        startingEnemyCount = enemiesRemaining;
        OnEnemyCountChanged?.Invoke(enemiesRemaining);
    }

    private List<GameObject> BuildRandomList(List<GameObject> prefabs, int count)
    {
        var result = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
            result.Add(prefabs[Random.Range(0, prefabs.Count)]);
        return result;
    }

    #endregion

    #region Battle Events

    /// <summary>
    /// Called when an enemy dies
    /// </summary>
    private void OnEnemyDied(Enemy enemy)
    {
        enemiesRemaining--;
        OnEnemyCountChanged?.Invoke(enemiesRemaining);
    }

    private IEnumerator StopRecording()
    {
        yield return new WaitForSeconds(0.5f);
        CombatRecorder.Instance.StopRecording();
    }

    /// <summary>
    /// Called when a party member dies
    /// </summary>
    private void OnPartyMemberDied(Villager villager)
    {
        partyMembersAlive--;
        casualties.Add(villager);
        OnPartyCountChanged?.Invoke(partyMembersAlive);

        Debug.Log($"Party member {villager.villagerName} died! {partyMembersAlive} remaining.");

        // Check if Jarl died (game over) or all party dead
        if (villager.isJarl)
        {
            Debug.Log("The Jarl has fallen!");
            Defeat();
        }
        else if (partyMembersAlive <= 0)
        {
            Defeat();
        }
    }

    /// <summary>
    /// Loot rolled once when the camp is cleared. Represents what you
    /// could already see waiting at the camp (RaidDestinationData.lootTable / GetPotentialLoot),
    /// as opposed to enemy "pocket" loot which is hidden until you kill them.
    /// </summary>
    private void RollDestinationLoot(float percentage)
    {
        var destination = RaidManager.Instance?.CurrentRaid;
        if (destination == null) return;

        foreach (var entry in destination.lootTable)
        {
            if (Random.value <= entry.dropChance)
            {
                float amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                amount *= percentage;
                AddLoot(entry.resourceType, amount);

                Debug.Log($"Camp loot: +{amount} {entry.resourceType}");
            }
        }
    }

    #endregion

    #region Battle End

    public void LeaveRaid()
    {
        if (enemiesRemaining <= 0)
        {
            Victory();
        }
        else
        {
            ForceRetreat();
        }
    }

    /// <summary>
    /// Fraction of the starting enemy count that has been cleared, used to scale destination
    /// loot on both Victory and ForceRetreat. 1 = full clear, 0 = nothing killed yet.
    /// </summary>
    private float GetClearPercentage()
    {
        if (startingEnemyCount <= 0) return 1f;
        return 1f - (float)enemiesRemaining / startingEnemyCount;
    }

    /// <summary>
    /// Victory - all enemies defeated
    /// </summary>
    public void Victory()
    {
        if (!raidActive) return;
        raidActive = false;
        GameManager.Instance.IsGameActive = false;

        RollDestinationLoot(GetClearPercentage());

        Debug.Log($"VICTORY! Collected {collectedLoot.Count} loot items. Casualties: {casualties.Count}");

        OnRaidVictory?.Invoke();
        CleanupLegRemnants();

        // Resolve this leg — player chooses Keep Sailing or Go Home next
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.ResolveLeg(RaidResult.Victory, collectedLoot, casualties);
        }

        StartCoroutine(StopRecording());
    }

    /// <summary>
    /// Defeat - Jarl died or all party dead
    /// </summary>
    public void Defeat()
    {
        if (!raidActive) return;
        raidActive = false;

        Debug.Log($"DEFEAT! Casualties: {casualties.Count}");

        OnRaidDefeat?.Invoke();

        // End raid with results (keep any loot collected)
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.EndRaid(RaidResult.Defeat, collectedLoot, casualties);
        }
    }

    /// <summary>
    /// Retreat - player chose to leave or timeout
    /// </summary>
    public void ForceRetreat()
    {
        if (!raidActive) return;
        raidActive = false;

        RollDestinationLoot(GetClearPercentage());

        Debug.Log($"RETREAT! Collected {collectedLoot.Count} loot. Casualties: {casualties.Count}");
        CleanupLegRemnants();

        // Resolve this leg — player chooses Keep Sailing or Go Home next
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.ResolveLeg(RaidResult.Retreat, collectedLoot, casualties);
        }
    }

    /// <summary>
    /// Destroys leftover enemy corpses immediately when a leg resolves. Enemy.Die() only
    /// schedules Destroy(gameObject, 5f) — a delayed self-destruct — so a corpse killed just
    /// before the leg ends is often still alive (still ticking/tweening) when the player acts.
    ///
    /// "Keep Sailing" reloads this exact same scene (every raid destination shares one scene)
    /// while the old instance is still alive — LoadingScreenManager holds allowSceneActivation
    /// false for several seconds during the fade, so anything left running keeps running in the
    /// background that whole time. A leftover corpse mid-fade (already observed logging a DOTween
    /// "target destroyed" warning from its delayed-destroy timer) sitting through that window
    /// froze the Editor solid on a chain hop (2026-08-11). Raid-party casualties are deliberately
    /// left alone here — VillagerAIBase.Update() already no-ops once dead, and their Villager
    /// reference/name needs to stay valid for the trip-summary screen, which can read it several
    /// legs later.
    /// </summary>
    private void CleanupLegRemnants()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        spawnedEnemies.Clear();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get current enemy count
    /// </summary>
    public int GetEnemiesRemaining() => enemiesRemaining;

    /// <summary>
    /// Get current party count
    /// </summary>
    public int GetPartyAlive() => partyMembersAlive;

    /// <summary>
    /// Get raid time status for UI
    /// </summary>
    public string GetTimeStatus()
    {
        if (RaidManager.Instance != null)
        {
            return RaidManager.Instance.GetRaidStatus();
        }
        return "";
    }

    /// <summary>
    /// Returns collected loot combined by resource type — one entry per type with totals summed.
    /// Use this for display. The raw per-drop breakdown stays in collectedLoot for debugging.
    /// </summary>
    public List<ResourceLoot> GetCombinedLoot()
    {
        var totals = new Dictionary<ResourceType, float>();
        foreach (var item in collectedLoot)
        {
            totals.TryGetValue(item.resourceType, out float current);
            totals[item.resourceType] = current + item.amount;
        }
        var result = new List<ResourceLoot>(totals.Count);
        foreach (var kv in totals)
            result.Add(new ResourceLoot { resourceType = kv.Key, amount = kv.Value });
        return result;
    }

    /// <summary>
    /// Manually add loot (from chests, etc.)
    /// </summary>
    public void AddLoot(ResourceType type, float amount)
    {
        // Apply Raiding Ships runestone bonus
        if (RunestoneManager.Instance != null)
        {
            amount *= RunestoneManager.Instance.GetRaidLootMultiplier();
        }

        amount = Mathf.CeilToInt(amount);

        collectedLoot.Add(new ResourceLoot { resourceType = type, amount = amount });
    }

    #endregion
}

[System.Serializable]
public class LootTableEntry
{
    public ResourceType resourceType = ResourceType.Iron;
    [Range(0f, 1f)]
    public float dropChance = 0.5f;
    public int minAmount = 1;
    public int maxAmount = 3;
}
