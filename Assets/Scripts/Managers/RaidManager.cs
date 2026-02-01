using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages offensive raids - leaving settlement, time tracking, and return
/// </summary>
public class RaidManager : MonoBehaviour
{
    public static RaidManager Instance { get; private set; }

    [Header("Raid Settings")]
    [Tooltip("Available raid destinations")]
    public List<RaidDestination> raidDestinations = new List<RaidDestination>();

    [Header("Current Raid State")]
    [SerializeField] private bool isOnRaid = false;
    [SerializeField] private RaidDestination currentRaid;
    [SerializeField] private float raidStartTime;
    [SerializeField] private List<Villager> raidParty = new List<Villager>();
    [SerializeField] private List<Villager> homeVillagers = new List<Villager>(); // Villagers staying behind

    [Header("Scene Names")]
    [Tooltip("Name of the main settlement scene")]
    public string settlementSceneName = "Demo Scene";

    // Events
    public event Action<RaidDestination> OnRaidStarted;
    public event Action<RaidReport> OnRaidEnded;
    public event Action<SettlementReport> OnReturnedToSettlement;

    // Properties
    public bool IsOnRaid => isOnRaid;
    public RaidDestination CurrentRaid => currentRaid;
    public List<Villager> RaidParty => raidParty;
    private List<Villager> RaidPartyCopy;

    // Track raid survivors for reintegration (cleared after VillagerSpawner uses it)
    private HashSet<Villager> raidSurvivorsForReintegration = new HashSet<Villager>();

    // Store worker assignments before raid (villager uniqueId -> building data name)
    private Dictionary<string, string> savedWorkerAssignments = new Dictionary<string, string>();

    private void Awake()
    {
        Debug.Log($"RaidManager.Awake() - Instance is null: {Instance == null}, this: {GetInstanceID()}, scene: {gameObject.scene.name}");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"RaidManager: Initialized as singleton with DontDestroyOnLoad, ID: {GetInstanceID()}");
        }
        else
        {
            Debug.Log($"RaidManager: Duplicate found, destroying self. Existing ID: {Instance.GetInstanceID()}, this ID: {GetInstanceID()}");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Debug.Log($"RaidManager.OnDestroy() - ID: {GetInstanceID()}, Instance ID: {(Instance != null ? Instance.GetInstanceID().ToString() : "null")}");
        // Only clear Instance if this is the actual singleton being destroyed
        if (Instance == this)
        {
            Debug.LogWarning("RaidManager singleton is being destroyed! This should not happen during a raid.");
            Instance = null;
        }
    }

    #region Starting a Raid

    /// <summary>
    /// Start a raid with the given party to the specified destination
    /// </summary>
    public bool StartRaid(RaidDestination destination, List<Villager> party)
    {
        if (isOnRaid)
        {
            Debug.LogWarning("Already on a raid!");
            return false;
        }

        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("Cannot start raid with empty party!");
            return false;
        }

        if (destination == null)
        {
            Debug.LogWarning("No raid destination specified!");
            return false;
        }

        // Store raid info
        currentRaid = destination;
        raidParty = new List<Villager>(party);
        RaidPartyCopy = new List<Villager>(party);
        raidStartTime = Time.time;
        isOnRaid = true;

        // Pause settlement simulation
        PauseSettlement();

        // Save all worker assignments before the raid (for restoration on return)
        savedWorkerAssignments.Clear();
        if (SettlementManager.Instance != null)
        {
            foreach (var villager in SettlementManager.Instance.GetAllVillagers())
            {
                if (villager != null && villager.assignedBuilding != null && villager.assignedBuilding.data != null)
                {
                    savedWorkerAssignments[villager.uniqueId] = villager.assignedBuilding.data.name;
                }
            }
            Debug.Log($"Saved {savedWorkerAssignments.Count} worker assignments before raid");
        }

        // Mark raid party members as "on raid" and preserve across scene load
        foreach (var villager in raidParty)
        {
            if (villager != null)
            {
                villager.isOnRaid = true;
                villager.UnassignJob(); // Free up job while on raid

                // Unparent the villager first - DontDestroyOnLoad only works on root objects
                // If villager is child of a container that gets destroyed, they'd be destroyed too
                villager.transform.SetParent(null);

                // Preserve villager GameObject across scene transition
                DontDestroyOnLoad(villager.gameObject);
            }
        }

        // Preserve and hide villagers staying behind at the settlement
        homeVillagers.Clear();
        if (SettlementManager.Instance != null)
        {
            foreach (var villager in SettlementManager.Instance.GetAllVillagers())
            {
                if (villager != null && !raidParty.Contains(villager))
                {
                    homeVillagers.Add(villager);

                    // Unparent so DontDestroyOnLoad works
                    villager.transform.SetParent(null);

                    // Preserve across scene transition
                    DontDestroyOnLoad(villager.gameObject);

                    // Hide the villager while on raid
                    villager.gameObject.SetActive(false);
                }
            }
        }
        Debug.Log($"Preserved {homeVillagers.Count} villagers at settlement during raid.");

        Debug.Log($"Starting raid to {destination.destinationName} with {raidParty.Count} villagers.");

        OnRaidStarted?.Invoke(destination);

        // Load raid scene
        if (!string.IsNullOrEmpty(destination.sceneName))
        {
            SceneManager.LoadScene(destination.sceneName);
        }

        return true;
    }

    /// <summary>
    /// Pause settlement systems while on raid
    /// </summary>
    private void PauseSettlement()
    {
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(true);
        }
    }

    #endregion

    #region Ending a Raid

    /// <summary>
    /// End the current raid (called when raid is complete - win, lose, or retreat)
    /// </summary>
    public void EndRaid(RaidResult result, List<ResourceLoot> loot = null, List<Villager> casualties = null)
    {
        if (!isOnRaid)
        {
            Debug.LogWarning("Not currently on a raid!");
            return;
        }

        // Calculate real time elapsed
        float raidRealTime = Time.time - raidStartTime;

        // Calculate game days based on time dilation - FASTER = LESS TIME PASSED
        float gameDaysPassed = currentRaid.CalculateGameDaysPassed(raidRealTime);

        // Create raid report
        RaidReport raidReport = new RaidReport
        {
            destination = currentRaid,
            result = result,
            realTimeElapsed = raidRealTime,
            gameDaysPassed = gameDaysPassed,
            loot = loot ?? new List<ResourceLoot>(),
            casualties = casualties ?? new List<Villager>(),
            survivors = new List<Villager>(raidParty)
        };

        // Remove casualties from survivors
        if (casualties != null)
        {
            foreach (var casualty in casualties)
            {
                raidReport.survivors.Remove(casualty);
            }
        }

        Debug.Log($"Raid ended: {result}. Real time: {raidRealTime:F1}s, Game days passed: {gameDaysPassed:F2}. Loot: {loot?.Count ?? 0}, Casualties: {casualties?.Count ?? 0}");

        OnRaidEnded?.Invoke(raidReport);

        // Return to settlement
        ReturnToSettlement(raidReport);
    }

    /// <summary>
    /// Return to settlement and process time that passed
    /// </summary>
    private void ReturnToSettlement(RaidReport raidReport)
    {
        // Calculate what happened at settlement while away
        // Round up game days so at least partial days are simulated
        int daysToSimulate = Mathf.CeilToInt(raidReport.gameDaysPassed);

        SettlementReport settlementReport = SettlementSimulator.SimulateTime(
            daysToSimulate,
            raidParty, // Exclude raid party from settlement calculations
            raidReport.gameDaysPassed // Pass fractional days for more accurate resource calc
        );

        // Apply raid loot to resources
        if (ResourceManager.Instance != null && raidReport.loot != null)
        {
            foreach (var lootItem in raidReport.loot)
            {
                ResourceManager.Instance.AddResource(lootItem.resourceType, lootItem.amount);
            }
        }

        // Handle casualties
        if (raidReport.casualties != null)
        {
            foreach (var casualty in raidReport.casualties)
            {
                if (casualty != null && !casualty.IsDead())
                {
                    casualty.Die();
                }
            }
        }

        // Mark survivors as returned and clean up DontDestroyOnLoad status
        // Store survivors for VillagerSpawner to identify who needs repositioning
        raidSurvivorsForReintegration.Clear();
        foreach (var villager in raidReport.survivors)
        {
            if (villager != null)
            {
                raidSurvivorsForReintegration.Add(villager);
                villager.isOnRaid = false;

                // Disable raid mode AI
                VillagerAI ai = villager.GetComponent<VillagerAI>();
                if (ai != null)
                {
                    ai.SetRaidMode(false);
                }
            }
            Villager vil = RaidPartyCopy.Find(v => v == villager);
            if (vil != null)
            {
                villager.AssignJob(vil.currentJob, vil.assignedBuilding); // Reassign job after returning from raid
            }
        }

        // Apply settlement simulation results
        ApplySettlementReport(settlementReport);

        // Reset raid state
        isOnRaid = false;
        currentRaid = null;

        // Destroy casualties
        foreach (var casualty in raidReport.casualties)
        {
            if (casualty != null)
            {
                Destroy(casualty.gameObject);
            }
        }

        raidParty.Clear();
        RaidPartyCopy.Clear();

        // Reactivate home villagers that were hidden during the raid
        foreach (var villager in homeVillagers)
        {
            if (villager != null)
            {
                villager.gameObject.SetActive(true);
            }
        }
        Debug.Log($"Reactivated {homeVillagers.Count} villagers at settlement.");
        homeVillagers.Clear();

        // Resume settlement
        ResumeSettlement();

        OnReturnedToSettlement?.Invoke(settlementReport);

        // Load settlement scene - VillagerSpawner will handle reintegrating survivors
        SceneManager.LoadScene(settlementSceneName);
    }

    /// <summary>
    /// Apply the settlement report - resources, events, etc.
    /// </summary>
    private void ApplySettlementReport(SettlementReport report)
    {
        if (ResourceManager.Instance == null) return;

        // Apply net resource changes
        foreach (var change in report.resourceChanges)
        {
            if (change.Value >= 0)
            {
                ResourceManager.Instance.AddResource(change.Key, change.Value);
            }
            else
            {
                ResourceManager.Instance.SpendResource(change.Key, -change.Value);
            }
        }

        // Apply villager damage from events (true damage - abstract event damage)
        if (SettlementManager.Instance != null && report.villagerDamage > 0)
        {
            var villagers = SettlementManager.Instance.GetAllVillagers();
            float damagePerVillager = report.villagerDamage / Mathf.Max(1, villagers.Count);

            foreach (var villager in villagers)
            {
                if (villager != null && !villager.IsDead() && !villager.isOnRaid)
                {
                    villager.TakeDamage(damagePerVillager, null, true);
                }
            }
        }

        // Apply morale changes
        if (SettlementManager.Instance != null)
        {
            var villagers = SettlementManager.Instance.GetAllVillagers();
            foreach (var villager in villagers)
            {
                if (villager != null && !villager.IsDead())
                {
                    villager.ChangeMorale(report.moraleChange);
                }
            }
        }

        Debug.Log($"Settlement report applied: {report.events.Count} events occurred while away.");
    }

    /// <summary>
    /// Resume settlement systems after raid
    /// </summary>
    private void ResumeSettlement()
    {
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(false);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get list of available raid destinations
    /// </summary>
    public List<RaidDestination> GetAvailableRaids()
    {
        return raidDestinations;
    }

    public Villager AddVillagerToRaidParty(Villager villager)
    {
        if (villager != null && !raidParty.Contains(villager))
        {
            raidParty.Add(villager);
            return villager;
        }
        return null;
    }

    /// <summary>
    /// Get time remaining in current raid (if time-limited)
    /// </summary>
    public float GetRaidTimeRemaining()
    {
        if (!isOnRaid || currentRaid == null) return 0f;

        float elapsed = Time.time - raidStartTime;
        float maxTime = currentRaid.realTimeLimit;

        return Mathf.Max(0f, maxTime - elapsed);
    }

    /// <summary>
    /// Get real seconds elapsed since raid started
    /// </summary>
    public float GetRaidTimeElapsed()
    {
        if (!isOnRaid) return 0f;
        return Time.time - raidStartTime;
    }

    /// <summary>
    /// Get current projected game days that would pass if raid ended now
    /// </summary>
    public float GetProjectedGameDays()
    {
        if (!isOnRaid || currentRaid == null) return 0f;

        float elapsed = Time.time - raidStartTime;
        return currentRaid.CalculateGameDaysPassed(elapsed);
    }

    /// <summary>
    /// Get formatted text showing current time dilation status (for UI)
    /// </summary>
    public string GetTimeDilationStatus()
    {
        if (!isOnRaid || currentRaid == null) return "";

        float elapsed = GetRaidTimeElapsed();
        float projectedDays = GetProjectedGameDays();
        float remaining = GetRaidTimeRemaining();

        return $"Time: {elapsed:F0}s | Settlement: {projectedDays:F1} days | Remaining: {remaining:F0}s";
    }

    /// <summary>
    /// Check if raid time has expired
    /// </summary>
    public bool IsRaidTimeExpired()
    {
        if (!isOnRaid || currentRaid == null) return false;

        return Time.time - raidStartTime >= currentRaid.realTimeLimit;
    }

    /// <summary>
    /// Force retreat from raid (timeout or player choice)
    /// </summary>
    public void Retreat()
    {
        EndRaid(RaidResult.Retreat);
    }

    /// <summary>
    /// Check if a villager was a raid survivor (for reintegration positioning)
    /// </summary>
    public bool WasRaidSurvivor(Villager villager)
    {
        return raidSurvivorsForReintegration.Contains(villager);
    }

    /// <summary>
    /// Clear the raid survivors tracking after reintegration is complete
    /// </summary>
    public void ClearReintegrationTracking()
    {
        raidSurvivorsForReintegration.Clear();
    }

    /// <summary>
    /// Get the saved building assignment for a villager (by uniqueId)
    /// </summary>
    public string GetSavedWorkerAssignment(string villagerUniqueId)
    {
        if (savedWorkerAssignments.TryGetValue(villagerUniqueId, out string buildingName))
        {
            return buildingName;
        }
        return null;
    }

    /// <summary>
    /// Clear saved worker assignments after restoration
    /// </summary>
    public void ClearSavedWorkerAssignments()
    {
        savedWorkerAssignments.Clear();
    }

    #endregion
}

#region Data Classes

[System.Serializable]
public class RaidDestination
{
    public string destinationName = "Unknown Land";
    public string sceneName = "RaidScene";
    public string description = "A dangerous place to raid.";

    [Header("Time Dilation")]
    [Tooltip("How fast settlement time passes while on raid (10 = 1 real second = 10 game seconds)")]
    public float timeDilationMultiplier = 10f;

    [Tooltip("Real-time limit in seconds (0 = no limit)")]
    public float realTimeLimit = 300f; // 5 minutes default

    [Tooltip("Maximum game-days that can pass (caps the time even if player is slow)")]
    public float maxGameDays = 5f;

    [Header("Difficulty")]
    public int recommendedPartySize = 3;
    public int enemyCount = 5;
    public Enemy.EnemyType primaryEnemyType = Enemy.EnemyType.Draugr;

    [Header("Rewards")]
    public List<ResourceLoot> potentialLoot = new List<ResourceLoot>();

    /// <summary>
    /// Calculate how many game-days passed based on real time elapsed
    /// </summary>
    public float CalculateGameDaysPassed(float realSecondsElapsed)
    {
        // Convert real seconds to game seconds using dilation
        float gameSeconds = realSecondsElapsed * timeDilationMultiplier;

        // Convert game seconds to game days (assuming 120 second day cycle like DayNightManager)
        float gameDays = gameSeconds / 120f;

        // Cap at maximum
        return Mathf.Min(gameDays, maxGameDays);
    }
}

[System.Serializable]
public class ResourceLoot
{
    public ResourceType resourceType;
    public float amount;
}

public enum RaidResult
{
    Victory,
    Defeat,
    Retreat
}

[System.Serializable]
public class RaidReport
{
    public RaidDestination destination;
    public RaidResult result;

    [Header("Time")]
    public float realTimeElapsed;    // Actual seconds spent in raid
    public float gameDaysPassed;     // Calculated game days based on time dilation

    [Header("Results")]
    public List<ResourceLoot> loot = new List<ResourceLoot>();
    public List<Villager> casualties = new List<Villager>();
    public List<Villager> survivors = new List<Villager>();

    /// <summary>
    /// Get a formatted string showing time efficiency
    /// </summary>
    public string GetTimeEfficiencyText()
    {
        float maxDays = destination?.maxGameDays ?? 5f;
        float efficiency = 1f - (gameDaysPassed / maxDays);
        return $"{gameDaysPassed:F1} days passed ({efficiency * 100:F0}% time saved)";
    }
}

[System.Serializable]
public class SettlementReport
{
    public int daysPassed;           // Whole days (for event count)
    public float exactDaysPassed;    // Fractional days (for resource calculations)
    public Dictionary<ResourceType, float> resourceChanges = new Dictionary<ResourceType, float>();
    public float villagerDamage;
    public float moraleChange;
    public List<SettlementEvent> events = new List<SettlementEvent>();

    /// <summary>
    /// Get summary text for UI
    /// </summary>
    public string GetSummaryText()
    {
        int netPositive = 0;
        int netNegative = 0;
        foreach (var change in resourceChanges)
        {
            if (change.Value > 0) netPositive++;
            else if (change.Value < 0) netNegative++;
        }

        return $"{exactDaysPassed:F1} days passed. {events.Count} events. Net resources: +{netPositive}/-{netNegative}";
    }
}

[System.Serializable]
public class SettlementEvent
{
    public string eventName;
    public string description;
    public SettlementEventType eventType;
}

public enum SettlementEventType
{
    Positive,
    Negative,
    Neutral
}

#endregion
