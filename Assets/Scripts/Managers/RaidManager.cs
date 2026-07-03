using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages offensive raids - leaving settlement, time tracking, and return.
/// On return, stores a PendingRaidResults snapshot; GameManager loads the pre-raid
/// autosave then calls ApplyPendingResults() to patch loot/casualties/time on top.
///
/// Settlement time lost is determined solely by RaidDestinationData.travelTimeHours
/// (there and back), not by how long the player spends fighting.
/// </summary>
public class RaidManager : MonoBehaviour
{
    public static RaidManager Instance { get; private set; }

    [Header("Raid Settings")]
    [Tooltip("Available raid destinations (ScriptableObject assets)")]
    public List<RaidDestinationData> raidDestinations = new List<RaidDestinationData>();

    [Header("Current Raid State")]
    [SerializeField] private bool isOnRaid = false;
    [SerializeField] private RaidDestinationData currentRaid;
    [SerializeField] private float raidStartTime;
    [SerializeField] private List<Villager> raidParty = new List<Villager>();
    private int _raidStartAbsoluteDay;
    private float _raidStartTimeOfDay;

    [Header("Raid Chain")]
    [Tooltip("Placeholder flat cost (in game-days) added to totalTimeAway for every hop after the first. Will be replaced by real per-location distances.")]
    [SerializeField] private float hopCost = 1f;
    private float totalTimeAway;
    private RaidResult lastLegResult;
    private readonly List<RaidDestinationData> visitedThisTrip = new List<RaidDestinationData>();
    private readonly List<ResourceLoot> tripLoot = new List<ResourceLoot>();
    private readonly List<Villager> tripCasualties = new List<Villager>();

    [Header("Scene Names")]
    [Tooltip("Name of the main settlement scene")]
    public string settlementSceneName = "Demo Scene";

    // Events
    public event Action<RaidDestinationData> OnRaidStarted;
    public event Action<RaidReport> OnRaidEnded;
    public event Action<SettlementReport> OnReturnedToSettlement;
    public event Action<LegReport> OnLegResolved;

    // Properties
    public bool IsOnRaid => isOnRaid;
    public RaidDestinationData CurrentRaid => currentRaid;
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

    public bool StartRaid(RaidDestinationData destination, List<Villager> party)
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

        totalTimeAway = destination.GetOneWayGameDays();
        visitedThisTrip.Clear();
        visitedThisTrip.Add(destination);
        tripLoot.Clear();
        tripCasualties.Clear();
        lastLegResult = default;

        // Pause ticks so no production runs during the autosave write
        PauseSettlement();

        // Capture time anchor immediately before the autosave snapshot
        _raidStartAbsoluteDay = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentAbsoluteDay : 0;
        _raidStartTimeOfDay   = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentTimeOfDay   : 0f;

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

        Debug.Log($"Starting raid to {destination.destinationName} with {raidParty.Count} villagers. Settlement loses {destination.GetGameDaysPassed():F1} days.");
        OnRaidStarted?.Invoke(destination);

        if (!string.IsNullOrEmpty(destination.sceneName))
            LoadScene(destination.sceneName);

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
        if (!isOnRaid || currentRaid == null)
        {
            Debug.LogWarning("Not currently on a raid!");
            return;
        }

        loot = loot ?? new List<ResourceLoot>();
        casualties = casualties ?? new List<Villager>();

        tripLoot.AddRange(loot);
        tripCasualties.AddRange(casualties);
        foreach (var casualty in casualties)
            raidParty.Remove(casualty);

        float raidRealTime   = Time.time - raidStartTime;
        // Core formula: D1 + hopCost x hops-after-first (already folded into totalTimeAway
        // by ContinueRaid) + D_current (one-way trip home from wherever we are now).
        // Zero-hop case: totalTimeAway == D1, currentRaid == first destination -> D1 + D1 == 2xD1 (matches old GetGameDaysPassed()).
        float gameDaysPassed = totalTimeAway + currentRaid.GetOneWayGameDays();

        RaidReport raidReport = new RaidReport
        {
            destination      = currentRaid,
            result           = result,
            realTimeElapsed  = raidRealTime,
            gameDaysPassed   = gameDaysPassed,
            loot             = new List<ResourceLoot>(tripLoot),
            casualties       = new List<Villager>(tripCasualties),
            survivors        = new List<Villager>(raidParty)
        };

        Debug.Log($"Raid chain ended: {result}. Total settlement days: {gameDaysPassed:F2}. Legs visited: {visitedThisTrip.Count}. Loot: {raidReport.loot.Count}, Casualties: {raidReport.casualties.Count}");

        OnRaidEnded?.Invoke(raidReport);
        ReturnToSettlement(raidReport);
    }

    /// <summary>
    /// Resolves a non-terminal leg (Victory or Retreat) — folds loot/casualties into the
    /// running trip totals and lets the player choose to keep sailing or go home.
    /// Does not touch settlement/autosave state.
    /// </summary>
    public void ResolveLeg(RaidResult result, List<ResourceLoot> loot, List<Villager> casualties)
    {
        if (!isOnRaid || currentRaid == null)
        {
            Debug.LogWarning("ResolveLeg called with no active raid!");
            return;
        }

        loot = loot ?? new List<ResourceLoot>();
        casualties = casualties ?? new List<Villager>();

        tripLoot.AddRange(loot);
        tripCasualties.AddRange(casualties);
        foreach (var casualty in casualties)
            raidParty.Remove(casualty);

        lastLegResult = result;

        var legReport = new LegReport
        {
            destination         = currentRaid,
            result               = result,
            legLoot              = loot,
            legCasualties        = casualties,
            tripLootSoFar        = new List<ResourceLoot>(tripLoot),
            tripCasualtiesSoFar  = new List<Villager>(tripCasualties),
            totalTimeAwaySoFar   = totalTimeAway,
            canContinue          = GetAvailableChainDestinations().Count > 0
        };

        Debug.Log($"Leg resolved at {currentRaid.destinationName}: {result}. Time away so far: {totalTimeAway:F2} days. Can continue: {legReport.canContinue}");
        OnLegResolved?.Invoke(legReport);
    }

    /// <summary>
    /// "Keep Sailing" — commits to the next leg of the chain, adding the placeholder hopCost
    /// and loading the new destination's scene. Party carries over unchanged.
    /// </summary>
    public bool ContinueRaid(RaidDestinationData nextDestination)
    {
        if (!isOnRaid || currentRaid == null) return false;
        if (nextDestination == null || visitedThisTrip.Contains(nextDestination)) return false;

        totalTimeAway += hopCost;
        currentRaid = nextDestination;
        visitedThisTrip.Add(nextDestination);

        // Restart the per-leg real-time fight-limit clock — without this, the new leg would
        // inherit elapsed time from the previous leg(s) and could time out immediately.
        raidStartTime = Time.time;

        Debug.Log($"Continuing raid chain to {nextDestination.destinationName}. Total time away so far: {totalTimeAway:F2} days (excluding return).");

        if (!string.IsNullOrEmpty(nextDestination.sceneName))
            LoadScene(nextDestination.sceneName);

        return true;
    }

    /// <summary>
    /// "Go Home" — finalizes the trip using the last leg's result. Loot/casualties for that
    /// leg were already folded into the trip totals by ResolveLeg.
    /// </summary>
    public void GoHome()
    {
        if (!isOnRaid || currentRaid == null) return;
        EndRaid(lastLegResult);
    }

    private void ReturnToSettlement(RaidReport raidReport)
    {
        // Package all results as data — scene-local managers are about to be destroyed
        pendingRaidResults = new PendingRaidResults
        {
            hasPendingResults    = true,
            gameDaysPassed       = raidReport.gameDaysPassed,
            loot                 = raidReport.loot ?? new List<ResourceLoot>(),
            casualtyIds          = raidReport.casualties?
                .Where(v => v != null)
                .Select(v => v.uniqueId)
                .ToList() ?? new List<string>(),
            survivorHealth       = new Dictionary<string, float>(),
            raidPartyIds         = raidParty
                .Where(v => v != null)
                .Select(v => v.uniqueId)
                .ToList(),
            raidStartAbsoluteDay = _raidStartAbsoluteDay,
            raidStartTimeOfDay   = _raidStartTimeOfDay
        };

        foreach (var survivor in raidReport.survivors)
        {
            if (survivor != null && !string.IsNullOrEmpty(survivor.uniqueId))
                pendingRaidResults.survivorHealth[survivor.uniqueId] = survivor.currentHealth;
        }

        isOnRaid    = false;
        currentRaid = null;

        // Destroy DDOL raid party villagers — autosave recreates them at pre-raid state;
        // ApplyPendingResults patches casualties/health via uniqueId after load
        foreach (var villager in raidParty)
        {
            if (villager != null)
                Destroy(villager.gameObject);
        }
        raidParty.Clear();

        // Trip fully over — clear chain state
        totalTimeAway = 0f;
        visitedThisTrip.Clear();
        tripLoot.Clear();
        tripCasualties.Clear();
        lastLegResult = default;

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
        LoadScene(settlementSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
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

            // B25: FloorToInt avoids simulating a phantom extra day; partial day handled inside SimulateTime
            int daysToSimulate = Mathf.FloorToInt(pending.gameDaysPassed);
            SettlementReport report = SettlementSimulator.SimulateTime(daysToSimulate, absentVillagers, pending.gameDaysPassed, pending.raidStartAbsoluteDay);
            ApplySettlementReport(report);
            OnReturnedToSettlement?.Invoke(report);
        }

        // 5. Advance game time
        // B25: CeilToInt was causing season clock to advance one extra day — Floor + partial remainder is accurate
        int days = Mathf.FloorToInt(pending.gameDaysPassed);
        DayNightManager.Instance?.AdvanceDays(days);
        SeasonManager.Instance?.AdvanceDays(days);


        // 7. Restore time of day and date to post-raid position
        int   endAbsoluteDay = pending.raidStartAbsoluteDay + Mathf.FloorToInt(pending.gameDaysPassed);
        float endTimeOfDay   = pending.raidStartTimeOfDay + (pending.gameDaysPassed % 1f);
        if (endTimeOfDay >= 1f) { endAbsoluteDay++; endTimeOfDay -= 1f; }
        DayNightManager.Instance?.SetDateTime(endAbsoluteDay, endTimeOfDay);

        // Rebuild calendar window from the new time position. DayNightManager.AdvanceDays and
        // SeasonManager.AdvanceDays are intentionally silent (no OnNewDay fired), so
        // CalendarManager never shifts on its own during raid return.
        CalendarManager.Instance?.RefreshCalendarData();

        // 8. Restore correct weather for this day
        // Player lands at the correct time of day and weather state — if it was storming all raid, it's still storming on return
        bool isStormDay    = StormScheduler.Instance != null
            && StormScheduler.Instance.GetCurrentDayWoodMultiplier() > 1f;
        bool isWinterReturn = SeasonManager.Instance != null
            && SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
        int coldLevel = (int)CalendarManager.Instance?.GetCurrentDayData().coldDayType;
        WeatherManager.Instance?.ApplyWeatherForDay(isStormDay, isWinterReturn, coldLevel);

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

        if (SettlementManager.Instance == null) return;

        // Skill gains accrued during the simulation (deferred so the day loop never mutates live state).
        foreach (var gain in report.skillGains)
        {
            Villager v = SettlementManager.Instance.GetVillagerById(gain.villagerId);
            if (v == null || v.IsDead()) continue;
            for (int i = 0; i < gain.completions; i++)
                v.skills.ImproveSkill(gain.jobType);
        }

        // Per-villager outcomes — report.villagerOutcomes only ever contains villagers the simulator
        // seeded from "present" (non-raid-party) villagers, so raid-party members never appear here.
        int deaths = 0;
        foreach (var outcome in report.villagerOutcomes)
        {
            Villager v = SettlementManager.Instance.GetVillagerById(outcome.villagerId);
            if (v == null) continue;

            if (outcome.died)
            {
                if (!v.IsDead())
                {
                    v.currentHealth = 0f;
                    v.Die(); // fires JarlManager succession, RemoveWorker, UnregisterVillager, etc.
                    v.deathCause = outcome.deathCause; // simulator-computed cause overrides Die()'s own guess,
                                                        // since live isHungry/isCold/lastDamageWasCombat
                                                        // reflect pre-raid state, not what happened during the sim
                    deaths++;
                }
                continue;
            }

            if (v.IsDead()) continue;

            if (outcome.healthDelta < 0f)
                v.TakeDamage(-outcome.healthDelta, null, true);
            else if (outcome.healthDelta > 0f)
                v.Heal(outcome.healthDelta);

            if (outcome.moraleDelta != 0f)
                v.ChangeMorale(outcome.moraleDelta);
        }

        Debug.Log($"Settlement report applied: {report.events.Count} events, {deaths} deaths while away.");
    }

    #endregion

    #region Public API

    public List<RaidDestinationData> GetAvailableRaids() => raidDestinations;

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

    /// <summary>
    /// Settlement days lost for the current raid — fixed by destination, not fight duration.
    /// </summary>
    public float GetProjectedGameDays()
    {
        if (!isOnRaid || currentRaid == null) return 0f;
        return currentRaid.GetGameDaysPassed();
    }

    public string GetRaidStatus()
    {
        if (!isOnRaid || currentRaid == null) return "";
        float days      = currentRaid.GetGameDaysPassed();
        float remaining = GetRaidTimeRemaining();
        return currentRaid.realTimeLimit > 0f
            ? $"Settlement: {days:F1} days away | Fight remaining: {remaining:F0}s"
            : $"Settlement: {days:F1} days away";
    }

    public bool IsRaidTimeExpired()
    {
        if (!isOnRaid || currentRaid == null) return false;
        return currentRaid.realTimeLimit > 0f && Time.time - raidStartTime >= currentRaid.realTimeLimit;
    }

    public void Retreat() => EndRaid(RaidResult.Retreat);

    /// <summary>
    /// Destinations still available to hop to this trip (excludes everything already visited).
    /// </summary>
    public List<RaidDestinationData> GetAvailableChainDestinations()
        => raidDestinations.Where(d => d != null && !visitedThisTrip.Contains(d)).ToList();

    /// <summary>
    /// Flat placeholder cost (in game-days) charged for hopping to any next destination.
    /// </summary>
    public float GetHopCostDisplay() => hopCost;

    #endregion
}

#region Data Classes

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
    public RaidDestinationData destination;
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
        float roundTripHours = destination != null ? destination.travelTimeHours * 2f : gameDaysPassed * 24f;
        return $"{gameDaysPassed:F1} days passed ({roundTripHours:F0}h round trip)";
    }
}

/// <summary>
/// Fired when a non-terminal leg (Victory or Retreat) resolves mid-chain.
/// Distinct from RaidReport, which only fires when the whole trip ends.
/// </summary>
[System.Serializable]
public class LegReport
{
    public RaidDestinationData destination;
    public RaidResult result;

    [Header("This Leg")]
    public List<ResourceLoot> legLoot = new List<ResourceLoot>();
    public List<Villager> legCasualties = new List<Villager>();

    [Header("Trip So Far")]
    public List<ResourceLoot> tripLootSoFar = new List<ResourceLoot>();
    public List<Villager> tripCasualtiesSoFar = new List<Villager>();
    public float totalTimeAwaySoFar;
    public bool canContinue;
}

[System.Serializable]
public class SettlementReport
{
    public int daysPassed;
    public float exactDaysPassed;
    public Dictionary<ResourceType, float> resourceChanges = new Dictionary<ResourceType, float>();
    public List<VillagerOutcome> villagerOutcomes = new List<VillagerOutcome>();
    public List<SkillGain> skillGains = new List<SkillGain>();
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

/// <summary>
/// Per-villager result of a settlement simulation — replaces the old pooled villagerDamage/moraleChange
/// even-split so deaths and health/morale deltas apply to the specific villager the simulation affected.
/// </summary>
[System.Serializable]
public class VillagerOutcome
{
    public string villagerId;
    public float healthDelta;    // ignored if died == true
    public float moraleDelta;
    public bool died;
    public DeathCause deathCause; // Combat / Cold / Starvation / Other only — simulator never assigns OldAge
}

[System.Serializable]
public class SkillGain
{
    public string villagerId;
    public JobType jobType;
    public int completions;
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
    public int raidStartAbsoluteDay;
    public float raidStartTimeOfDay;
}

#endregion
