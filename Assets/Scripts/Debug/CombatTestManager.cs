using UnityEngine;

/// <summary>
/// Drop into a test scene to spawn a player character and enemies for combat testing.
///
/// Required scene GameObjects:
///   - PlayerController     (singleton, empty GameObject with PlayerController component)
///   - WeaponDatabase       (with availableWeapons and availableShields assigned)
///   - WoundManager         (singleton)
///   - Camera               (with CameraController, playerTarget can start null)
///
/// Optional:
///   - JarlManager          (only needed if succession flow should fire on death)
///
/// Quick setup:
///   1. Create an empty scene, add the required singletons above.
///   2. Add an empty GameObject, attach CombatTestManager.
///   3. Assign villagerPrefab, at least one enemy prefab in enemyTypes.
///   4. Hit Play — player spawns automatically, use the on-screen panel to spawn enemies.
/// </summary>
public class CombatTestManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject villagerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 2f;

    [Header("Ally Villagers (optional)")]
    [Tooltip("Prefab for AI-controlled ally villagers. Useful for testing villager reactive blocking.")]
    [SerializeField] private GameObject allyPrefab;
    [SerializeField] private Transform[] allySpawnPoints;

    [Header("Enemies")]
    [SerializeField] private EnemySpawnConfig[] enemyTypes;
    [Tooltip("Optional fixed spawn positions. If empty, enemies spawn in a ring around the player.")]
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private int defaultSpawnCount = 3;
    private int enemySpawnedAmount;

    [System.Serializable]
    public class EnemySpawnConfig
    {
        public string label = "Enemy";
        public GameObject prefab;
        public bool giveWeapon;
        public bool giveShield;
    }

    private Villager _player;
    private int _spawnCount;

    private void Start()
    {
        _spawnCount = defaultSpawnCount;
        enemySpawnedAmount = 0;
        SpawnPlayer();
    }

    // ── Player ────────────────────────────────────────────────────────────────

    public void SpawnPlayer()
    {
        if (_player != null)
            Destroy(_player.gameObject);

        if (villagerPrefab == null)
        {
            Debug.LogError("[CombatTest] No villager prefab assigned.");
            return;
        }

        Vector3 pos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        var go = Instantiate(villagerPrefab, pos, Quaternion.identity);
        _player = go.GetComponent<Villager>();

        if (_player == null)
        {
            Debug.LogError("[CombatTest] Villager prefab has no Villager component.");
            return;
        }

        _player.isJarl = true;

        var ia = go.GetComponent<ItemAttachment>();
        if (ia != null)
        {
            ia.TakeWeaponFromArmory();
            //ia.GiveRandomShield();
            //ia.GiveRandomTorch();
        }

        _player.ApplySkillBonuses();
        _player.Init();

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetControlTarget(_player);

        _player.OnDeath += HandlePlayerDeath;

        Debug.Log("[CombatTest] Player spawned.");
    }

    private void HandlePlayerDeath()
    {
        if (respawnOnDeath)
            Invoke(nameof(SpawnPlayer), respawnDelay);
    }

    // ── Allies ────────────────────────────────────────────────────────────────

    public void SpawnAllies()
    {
        if (allyPrefab == null || allySpawnPoints == null || allySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[CombatTest] No ally prefab or spawn points assigned.");
            return;
        }

        for(int i = 0; i < _spawnCount; i++)
        {
            var go = Instantiate(allyPrefab, allySpawnPoints[i].position, Quaternion.identity);
            var villager = go.GetComponent<Villager>();
            if (villager == null) continue;

            var ia = go.GetComponent<ItemAttachment>();
            if (ia != null)
            {
                ia.TakeWeaponFromArmory();
                //ia.GiveRandomShield();
            }

            villager.ApplySkillBonuses();
            villager.Init();

            // Set raid follow AI — allies follow the player character
            var ai = go.GetComponent<VillagerAIBase>();
            if (ai != null && _player != null)
            {
                ai.SetRaidMode(true, _player.transform);
                PlayerController.Instance?.RegisterRaidAlly(ai);
            }
        }

        Debug.Log($"[CombatTest] Spawned {allySpawnPoints.Length} allies.");
    }

    public void ClearAllies()
    {
        PlayerController.Instance?.ClearRaidAllies();
        foreach (var v in FindObjectsByType<Villager>())
        {
            if (v != _player)
                Destroy(v.gameObject);
        }
    }

    // ── Enemies ───────────────────────────────────────────────────────────────

    public void SpawnEnemyType(int index)
    {
        if (index < 0 || index >= enemyTypes.Length || enemyTypes[index].prefab == null)
        {
            Debug.LogWarning($"[CombatTest] Enemy type index {index} is invalid or has no prefab.");
            return;
        }

        for (int i = 0; i < _spawnCount; i++)
        {
            var go = Instantiate(enemyTypes[index].prefab, GetSpawnPoint(i), Quaternion.identity);
            go.GetComponent<Enemy>().InitializeEnemyStats();
            go.gameObject.name = $"{enemyTypes[index].label} ({enemySpawnedAmount+ 1})";
            enemySpawnedAmount++;
            /*
            var ia = go.GetComponent<ItemAttachment>();
            if (ia != null)
            {
                if (enemyTypes[index].giveWeapon)
                    ia.GiveWeaponByName("DSword");

                if (enemyTypes[index].giveShield)
                    ia.GiveRandomShield();
            }
            */
        }
            

        Debug.Log($"[CombatTest] Spawned {_spawnCount}x {enemyTypes[index].label}.");


    }

    public void ClearEnemies()
    {
        foreach (var e in FindObjectsByType<Enemy>())
            Destroy(e.gameObject);
    }

    private Vector3 GetSpawnPoint(int index)
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
            return enemySpawnPoints[index % enemySpawnPoints.Length].position;

        // Spread in a ring around the player spawn if no explicit points set
        Vector3 centre = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        float angle = (360f / Mathf.Max(_spawnCount, 1)) * index * Mathf.Deg2Rad;
        return centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f;
    }

    // ── Fights ─────────────────────────────────────────────────

    public void PlaySetFight()
    {
        ClearEnemies();
        ClearAllies();
        SpawnEnemyType(0);
        SpawnAllies();
    }

    // ── On-screen debug panel ─────────────────────────────────────────────────

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 230, 700));
        GUILayout.BeginVertical("box");

        GUILayout.Label("─── COMBAT TEST ───");
        GUILayout.Space(4);

        DrawPlayerStatus();

        GUILayout.Space(8);

        // Spawn count
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Count: {_spawnCount}", GUILayout.Width(80));
        if (GUILayout.Button("−", GUILayout.Width(30)) && _spawnCount > 1) _spawnCount--;
        if (GUILayout.Button("+", GUILayout.Width(30))) _spawnCount++;
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label("Spawn Enemies:");
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (GUILayout.Button(enemyTypes[i].label))
                SpawnEnemyType(i);
        }

        GUILayout.Space(4);
        if (GUILayout.Button("Clear Enemies")) ClearEnemies();

        GUILayout.Space(4);
        if (GUILayout.Button("Test Fight")) PlaySetFight();

        if (allyPrefab != null)
        {
            GUILayout.Space(4);
            GUILayout.Label("Allies:");
            if (GUILayout.Button("Spawn Allies")) SpawnAllies();
            if (GUILayout.Button("Clear Allies")) ClearAllies();
        }

        GUILayout.Space(8);
        if (GUILayout.Button("Respawn Player")) SpawnPlayer();

        GUILayout.Space(6);
        int enemyCount = FindObjectsByType<Enemy>().Length;
        GUILayout.Label($"Enemies alive: {enemyCount}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawPlayerStatus()
    {
        if (_player == null)
        {
            GUILayout.Label("Player: not spawned");
            return;
        }

        if (_player.IsDead())
        {
            GUILayout.Label("Player: DEAD");
            return;
        }

        GUILayout.Label($"HP:     {_player.currentHealth:F0} / {_player.maxHealth:F0}");
        GUILayout.Label($"Wounds: {_player.activeWounds.Count} / {WoundManager.MAX_WOUNDS}");

        var cc = _player.GetComponent<CharacterBase>();
        if (cc == null) return;

        // Block / parry state
        string blockLabel;
        if (cc.isParrying)       blockLabel = "PARRYING";
        else if (cc.isBlocking)  blockLabel = "BLOCKING";
        else                     blockLabel = "—";
        GUILayout.Label($"Block:  {blockLabel}");

        // Shield
        if (cc.shield != null)
        {
            if (cc.shield.IsBroken)
                GUILayout.Label("Shield: BROKEN");
            else
                GUILayout.Label($"Shield: {cc.shield.CurrentDurability} / {cc.shield.maxDurability} dur");
        }
        else
        {
            GUILayout.Label("Shield: none");
        }

        // Weapon
        GUILayout.Label(cc.weapon != null ? $"Weapon: {cc.weapon.itemName}" : "Weapon: none");
    }
}
