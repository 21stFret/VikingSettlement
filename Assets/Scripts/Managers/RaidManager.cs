using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages offensive raids - leaving settlement, time tracking, and return.
/// On return, stores a PendingRaidResults snapshot; GameManager loads the pre-raid
/// autosave then calls ApplyPendingResults() to patch loot/casualties/time on top.
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

    public bool HasPendingRaidResults => pendingRaidResults != null && pendingRaidResults.hasPendingResults;

    private PendingRaidResults pendingRaidResults;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.LogWarning("RaidManager singleton is being destroyed! This should not happen during a raid.");
            Instance = null;
        }
    }

    #region Starting a Raid

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

        currentRaid = destination;
        raidParty = new List<Villager>(party);
        raidStartTime = Time.time;
        isOnRaid = true;

        // Pause ticks so no production runs during the autosave write
        PauseSettlement();

        // Snapshot full pre-raid settlement state — this is what we restore on return
        SaveManager.Instance?.SaveAuto();

        // Move raid party to DontDestroyOnLoad so they survive the scene transition
        foreach (var villager in raidParty)
        {
            if (villager != null)
            {
                villager.isOnRaid = true;
                villager.UnassignJob();
                villager.transform.SetParent(null);
                DontDestroyOnLoad(villager.gameObject);
            }
        }

        Debug.Log($"Starting raid to {destination.destinationName} with {raidParty.Count} villagers.");
        OnRaidStarted?.Invoke(destination);

        if (!string.IsNullOrEmpty(destination.sceneName))
            SceneManager.LoadScene(destination.sceneName);

        return true;
    }

    private void PauseSettlement()
    {
        if (GameTickManager.Instance != null)
            GameTickManager.Instance.SetPaused(true);
    }

    #endregion

    #region Ending a Raid

    public void EndRaid(RaidResult result, List<ResourceLoot> loot = null, List<Villager> casualties = null)
    {
        if (!isOnRaid)
        {
            Debug.LogWarning("Not currently on a raid!");
            return;
        }

        float raidRealTime = Time.time - raidStartTime;
        float gameDaysPassed = currentRaid.CalculateGameDaysPassed(raidRealTime);

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

        if (casualties != null)
        {
            foreach (var casualty in casualties)
                raidReport.survivors.Remove(casualty);
        }

        Debug.Log($"Raid ended: {result}. Real time: {raidRealTime:F1}s, Game days passed: {gameDaysPassed:F2}. Loot: {loot?.Count ?? 0}, Casualties: {casualties?.Count ?? 0}");

        OnRaidEnded?.Invoke(raidReport);
        ReturnToSettlement(raidReport);
    }

    private void ReturnToSettlement(RaidReport raidReport)
    {
        // Package all results as data — scene-local managers are about to be destroyed
        pendingRaidResults = new PendingRaidResults
        {
            hasPendingResults = true,
            gameDaysPassed = raidReport.gameDaysPassed,
            loot = raidReport.loot ?? new List<ResourceLoot>(),
            casualtyIds = raidReport.casualties?
                .Where(v => v != null)
                .Select(v => v.uniqueId)
                .ToList() ?? new List<string>(),
            survivorHealth = new Dictionary<string, float>(),
            raidPartyIds = raidParty
                .Where(v => v != null)
                .Select(v => v.uniqueId)
                .ToList()
        };

        foreach (var survivor in raidReport.survivors)
        {
            if (survivor != null && !string.IsNullOrEmpty(survivor.uniqueId))
                pendingRaidResults.survivorHealth[survivor.uniqueId] = survivor.currentHealth;
        }

        isOnRaid = false;
        currentRaid = null;

        // Destroy DDOL raid party villagers — autosave recreates them at pre-raid state;
        // ApplyPendingResults patches casualties/health via uniqueId after load
        foreach (var villager in raidParty)
        {
            if (villager != null)
                Destroy(villager.gameObject);
        }
        raidParty.Clear();

        Debug.Log($"Raid return: {pendingRaidResults.casualtyIds.Count} casualties, {pendingRaidResults.survivorHealth.Count} survivors, {raidReport.gameDaysPassed:F2} days. Waiting for results UI.");

        GameManager.Instance.PrepareRaidReturn();
        // Scene load is deferred — RaidResultsUI shows results, player clicks to return
    }

    /// <summary>
    /// Called by the raid results UI button to load the settlement scene.
    /// Separated from ReturnToSettlement so LoadScene isn't called mid-event-chain.
    /// </summary>
    public void LoadSettlementScene()
    {
        SceneManager.LoadScene(settlementSceneName);
    }

    #endregion

    #region Applying Pending Results

    /// <summary>
    /// Called by GameManager.InitializeGameAfterDelay after the autosave is loaded.
    /// Applies loot, casualties, survivor health, settlement simulation, and time advance.
    /// </summary>
    public void ApplyPendingResults()
    {
        if (pendingRaidResults == null || !pendingRaidResults.hasPendingResults) return;

        var pending = pendingRaidResults;
        pendingRaidResults = null; // clear before applying to prevent double-apply on error

        // 1. Loot
        if (ResourceManager.Instance != null)
        {
            foreach (var lootItem in pending.loot)
                ResourceManager.Instance.AddResource(lootItem.resourceType, lootItem.amount);
        }

        if (SettlementManager.Instance != null)
        {
            // 2. Kill casualties by uniqueId (autosave restored them as living)
            foreach (var id in pending.casualtyIds)
            {
                Villager v = SettlementManager.Instance.GetVillagerById(id);
                if (v != null && !v.IsDead())
                    v.Die();
            }

            // 3. Set survivor health to post-raid values (autosave had pre-raid health)
            foreach (var kvp in pending.survivorHealth)
            {
                Villager v = SettlementManager.Instance.GetVillagerById(kvp.Key);
                if (v != null)
                    v.currentHealth = Mathf.Clamp(kvp.Value, 0f, v.maxHealth);
            }

            // 4. Settlement simulation — what happened at home while the party was away
            var absentVillagers = pending.raidPartyIds
                .Select(id => SettlementManager.Instance.GetVillagerById(id))
                .Where(v => v != null)
                .ToList();

            int daysToSimulate = Mathf.CeilToInt(pending.gameDaysPassed);
            SettlementReport report = SettlementSimulator.SimulateTime(daysToSimulate, absentVillagers, pending.gameDaysPassed);
            ApplySettlementReport(report);
            OnReturnedToSettlement?.Invoke(report);
        }

        // 5. Advance game time
        int days = Mathf.CeilToInt(pending.gameDaysPassed);
        DayNightManager.Instance?.AdvanceDays(days);
        SeasonManager.Instance?.AdvanceDays(days);

        // 6. Resume settlement tick (paused in StartRaid)
        ResumeSettlement();

        Debug.Log($"Raid results applied: {pending.loot.Count} loot, {pending.casualtyIds.Count} casualties, {days} days simulated.");
    }

    private void ApplySettlementReport(SettlementReport report)
    {
        if (ResourceManager.Instance == null) return;

        foreach (var change in report.resourceChanges)
        {
            if (change.Value >= 0)
                ResourceManager.Instance.AddResource(change.Key, change.Value);
            else
                ResourceManager.Instance.SpendResource(change.Key, -change.Value);
        }

        if (SettlementManager.Instance != null && report.villagerDamage > 0)
        {
            var villagers = SettlementManager.Instance.GetAllVillagers();
            float damagePerVillager = report.villagerDamage / Mathf.Max(1, villagers.Count);

            foreach (var villager in villagers)
            {
                if (villager != null && !villager.IsDead() && !villager.isOnRaid)
                    villager.TakeDamage(damagePerVillager, null, true);
            }
        }

        if (SettlementManager.Instance != null)
        {
            foreach (var villager in SettlementManager.Instance.GetAllVillagers())
            {
                if (villager != null && !villager.IsDead())
                    villager.ChangeMorale(report.moraleChange);
            }
        }

        Debug.Log($"Settlement report applied: {report.events.Count} events occurred while away.");
    }

    private void ResumeSettlement()
    {
        if (GameTickManager.Instance != null)
            GameTickManager.Instance.SetPaused(false);
    }

    #endregion

    #region Public API

    public List<RaidDestination> GetAvailableRaids() => raidDestinations;

    public Villager AddVillagerToRaidParty(Villager villager)
    {
        if (villager != null && !raidParty.Contains(villager))
        {
            raidParty.Add(villager);
            return villager;
        }
        return null;
    }

    public float GetRaidTimeRemaining()
    {
        if (!isOnRaid || currentRaid == null) return 0f;
        return Mathf.Max(0f, currentRaid.realTimeLimit - (Time.time - raidStartTime));
    }

    public float GetRaidTimeElapsed()
    {
        if (!isOnRaid) return 0f;
        return Time.time - raidStartTime;
    }

    public float GetProjectedGameDays()
    {
        if (!isOnRaid || currentRaid == null) return 0f;
        return currentRaid.CalculateGameDaysPassed(Time.time - raidStartTime);
    }

    public string GetTimeDilationStatus()
    {
        if (!isOnRaid || currentRaid == null) return "";

        float elapsed = GetRaidTimeElapsed();
        float projectedDays = GetProjectedGameDays();
        float remaining = GetRaidTimeRemaining();

        return $"Time: {elapsed:F0}s | Settlement: {projectedDays:F1} days | Remaining: {remaining:F0}s";
    }

    public bool IsRaidTimeExpired()
    {
        if (!isOnRaid || currentRaid == null) return false;
        return Time.time - raidStartTime >= currentRaid.realTimeLimit;
    }

    public void Retreat() => EndRaid(RaidResult.Retreat);

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
    public float realTimeLimit = 300f;

    [Tooltip("Maximum game-days that can pass (caps the time even if player is slow)")]
    public float maxGameDays = 5f;

    [Header("Difficulty")]
    public int recommendedPartySize = 3;
    public int enemyCount = 5;
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("If true, spawns one of each prefab in the list (ignores enemyCount). If false, picks randomly from the list enemyCount times.")]
    public bool spawnAll = false;

    [Header("Rewards")]
    public List<ResourceLoot> potentialLoot = new List<ResourceLoot>();

    public float CalculateGameDaysPassed(float realSecondsElapsed)
    {
        float gameSeconds = realSecondsElapsed * timeDilationMultiplier;
        float gameDays = gameSeconds / 120f;
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
    public float realTimeElapsed;
    public float gameDaysPassed;

    [Header("Results")]
    public List<ResourceLoot> loot = new List<ResourceLoot>();
    public List<Villager> casualties = new List<Villager>();
    public List<Villager> survivors = new List<Villager>();

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
    public int daysPassed;
    public float exactDaysPassed;
    public Dictionary<ResourceType, float> resourceChanges = new Dictionary<ResourceType, float>();
    public float villagerDamage;
    public float moraleChange;
    public List<SettlementEvent> events = new List<SettlementEvent>();

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

/// <summary>
/// Holds serialized raid outcome data between raid end and autosave load.
/// Lives only in RaidManager memory — never written to disk.
/// </summary>
public class PendingRaidResults
{
    public bool hasPendingResults;
    public float gameDaysPassed;
    public List<ResourceLoot> loot;
    public List<string> casualtyIds;
    public Dictionary<string, float> survivorHealth;
    public List<string> raidPartyIds;
}

#endregion
