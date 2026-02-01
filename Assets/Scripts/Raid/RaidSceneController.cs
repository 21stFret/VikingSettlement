using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    [Header("Enemy Prefabs")]
    public GameObject draugrPrefab;
    public GameObject raiderPrefab;
    public GameObject warriorPrefab;
    public GameObject berserkerPrefab;
    public GameObject archerPrefab;
    public GameObject wolfPrefab;

    [Header("Raid State")]
    [SerializeField] private bool raidActive = false;
    [SerializeField] private int enemiesRemaining = 0;
    [SerializeField] private int partyMembersAlive = 0;

    [Header("Loot Settings")]
    [Tooltip("Loot dropped per enemy killed")]
    public List<LootTableEntry> lootTable = new List<LootTableEntry>();

    [Header("Player Control")]
    [Tooltip("Reference to PlayerController (will auto-find if not set)")]
    public PlayerController playerController;

    // Tracking
    private List<Villager> raidParty = new List<Villager>();
    private List<Enemy> spawnedEnemies = new List<Enemy>();
    private List<ResourceLoot> collectedLoot = new List<ResourceLoot>();
    private List<Villager> casualties = new List<Villager>();

    // Events
    public event System.Action OnRaidVictory;
    public event System.Action OnRaidDefeat;
    public event System.Action<int> OnEnemyCountChanged;
    public event System.Action<int> OnPartyCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Debug.Log($"RaidSceneController.Start() - RaidManager exists: {RaidManager.Instance != null}");
        if (RaidManager.Instance != null)
        {
            Debug.Log($"RaidSceneController.Start() - IsOnRaid: {RaidManager.Instance.IsOnRaid}, Party count: {RaidManager.Instance.RaidParty?.Count ?? 0}");
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
        raidParty = new List<Villager>(RaidManager.Instance.RaidParty);

        // Spawn party
        SpawnParty();

        // Spawn enemies
        SpawnEnemies(destination.enemyCount, destination.primaryEnemyType);

        Debug.Log($"Battle started! Party: {partyMembersAlive}, Enemies: {enemiesRemaining}");
    }

    /// <summary>
    /// Spawn raid party at designated points
    /// </summary>
    private void SpawnParty()
    {
        Debug.Log($"SpawnParty() - raidParty count: {raidParty?.Count ?? 0}, spawn points: {partySpawnPoints?.Count ?? 0}");

        partyMembersAlive = 0;
        Villager playerControlled = null;

        // First pass: spawn all villagers and find who should be player-controlled
        // Priority: Jarl > first villager in list
        for (int i = 0; i < raidParty.Count; i++)
        {
            Villager villager = raidParty[i];
            if (villager == null) continue;

            // Get spawn point (cycle through if more party than points)
            Transform spawnPoint = partySpawnPoints.Count > 0
                ? partySpawnPoints[i % partySpawnPoints.Count]
                : transform;

            // Move villager to spawn point
            villager.transform.position = spawnPoint.position;
            villager.gameObject.SetActive(true);

            // Subscribe to death
            villager.OnDeath += () => OnPartyMemberDied(villager);

            partyMembersAlive++;

            // Determine who gets player control (Jarl takes priority)
            if (playerControlled == null || villager.isJarl)
            {
                playerControlled = villager;
            }
        }

        // Second pass: set up AI for followers (everyone except player-controlled)
        foreach (var villager in raidParty)
        {
            if (villager == null || villager == playerControlled) continue;

            VillagerAI ai = villager.GetComponent<VillagerAI>();
            if (ai != null)
            {
                ai.SetRaidMode(true, playerControlled.transform);
            }
        }

        // Set up player control
        SetupPlayerControl(playerControlled);

        OnPartyCountChanged?.Invoke(partyMembersAlive);
    }

    /// <summary>
    /// Set up player control for a villager in the raid
    /// </summary>
    private void SetupPlayerControl(Villager villager)
    {
        Debug.Log($"SetupPlayerControl() - villager: {villager?.villagerName ?? "NULL"}, assigned controller: {playerController != null}");

        if (villager == null)
        {
            Debug.LogError("SetupPlayerControl called with null villager!");
            return;
        }

        // Find PlayerController if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            Debug.Log($"SetupPlayerControl() - FindFirstObjectByType result: {playerController != null}");
        }

        if (playerController != null)
        {
            playerController.SetControlTarget(villager);
            Debug.Log($"Player control set to {villager.villagerName} for raid");
        }
        else
        {
            Debug.LogError("No PlayerController found in raid scene! Make sure the raid scene has a PlayerController object.");
        }
    }

    /// <summary>
    /// Spawn enemies at designated points
    /// </summary>
    private void SpawnEnemies(int count, Enemy.EnemyType enemyType)
    {
        spawnedEnemies.Clear();
        enemiesRemaining = 0;

        GameObject prefab = GetEnemyPrefab(enemyType);
        if (prefab == null)
        {
            Debug.LogError($"No prefab found for enemy type: {enemyType}");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // Get spawn point (cycle through if more enemies than points)
            Transform spawnPoint = enemySpawnPoints.Count > 0
                ? enemySpawnPoints[i % enemySpawnPoints.Count]
                : transform;

            // Add some randomness to spawn position
            Vector3 spawnPos = spawnPoint.position + new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0
            );

            // Spawn enemy
            GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            Enemy enemy = enemyObj.GetComponent<Enemy>();

            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
                enemiesRemaining++;

                // Subscribe to death
                enemy.OnDeath += () => OnEnemyDied(enemy);
            }
        }

        OnEnemyCountChanged?.Invoke(enemiesRemaining);
    }

    /// <summary>
    /// Get the prefab for an enemy type
    /// </summary>
    private GameObject GetEnemyPrefab(Enemy.EnemyType type)
    {
        switch (type)
        {
            case Enemy.EnemyType.Draugr: return draugrPrefab;
            case Enemy.EnemyType.Raider: return raiderPrefab;
            case Enemy.EnemyType.Warrior: return warriorPrefab;
            case Enemy.EnemyType.Berserker: return berserkerPrefab;
            case Enemy.EnemyType.Archer: return archerPrefab;
            case Enemy.EnemyType.Wolf: return wolfPrefab;
            default: return draugrPrefab;
        }
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

        // Generate loot
        GenerateLoot(enemy);

        Debug.Log($"Enemy killed! {enemiesRemaining} remaining.");

        // Check victory
        if (enemiesRemaining <= 0)
        {
            Victory();
        }
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
    /// Generate loot from killed enemy
    /// </summary>
    private void GenerateLoot(Enemy enemy)
    {
        foreach (var entry in lootTable)
        {
            if (Random.value <= entry.dropChance)
            {
                float amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                collectedLoot.Add(new ResourceLoot
                {
                    resourceType = entry.resourceType,
                    amount = amount
                });

                Debug.Log($"Loot: +{amount} {entry.resourceType}");
            }
        }
    }

    #endregion

    #region Battle End

    /// <summary>
    /// Victory - all enemies defeated
    /// </summary>
    public void Victory()
    {
        if (!raidActive) return;
        raidActive = false;

        Debug.Log($"VICTORY! Collected {collectedLoot.Count} loot items. Casualties: {casualties.Count}");

        OnRaidVictory?.Invoke();

        // End raid with results
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.EndRaid(RaidResult.Victory, collectedLoot, casualties);
        }
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

        Debug.Log($"RETREAT! Collected {collectedLoot.Count} loot. Casualties: {casualties.Count}");

        // End raid
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.EndRaid(RaidResult.Retreat, collectedLoot, casualties);
        }
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
            return RaidManager.Instance.GetTimeDilationStatus();
        }
        return "";
    }

    /// <summary>
    /// Manually add loot (from chests, etc.)
    /// </summary>
    public void AddLoot(ResourceType type, float amount)
    {
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
