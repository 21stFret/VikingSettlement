using System;
using System.Collections.Generic;
using UnityEngine;

// ── Enums ────────────────────────────────────────────────────────────────────────

public enum RaidThreatLevel { None, Low, Medium, High }

public enum CalendarEventType
{
    Storm,
    MonsterSighting,
    Earthquake,
    RaidThreat
}

// ── Data structs ─────────────────────────────────────────────────────────────────

[Serializable]
public class CalendarEventData
{
    public CalendarEventType eventType;
    public string            eventName;
    public bool              isRevealed; // false when parent day is fogged
}

[Serializable]
public class CalendarDayData
{
    public SeasonManager.Season    season;
    public bool                    isFogged;
    public bool                    isSeasonTransition; // true on the first day of a new season
    public RaidThreatLevel         raidThreat;
    public List<CalendarEventData> events;
    public bool                    hasUnknownEvent;    // fogged day with something scheduled
}

// ── Manager ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Data-layer calendar: projects a 30-day window anchored to today.
/// Index 0 = today, index 29 = furthest visible day.
///
/// Subscribes only to DayNightManager.OnNewDay. Must be initialized AFTER
/// SeasonManager so it reads post-transition season state on shared day ticks.
///
/// UI panels subscribe to OnCalendarUpdated (C2).
/// Event scheduling (storms, raids, etc.) hooks in via the events list in C3+.
/// </summary>
public class CalendarManager : MonoBehaviour, ISaveable
{
    public static CalendarManager Instance { get; private set; }

    // Season lengths are read from SeasonManager.daysPerSeason (single shared value for Age 1).
    // TODO: Age progression will set summer and winter lengths separately via SeasonManager.

    // ── Exposed state (read via properties) ──────────────────────────────────────
    [Header("State (Read-Only)")]
    [SerializeField] private int _currentDay;
    [SerializeField] private SeasonManager.Season _currentSeason;
    [SerializeField] private int _daysUntilSeasonEnd;
    [SerializeField] private int _godiLevel;
    [SerializeField] private int _scoutedRange = 15;

    /// <summary>Day within the current season (0-indexed: 0 = first day of season).</summary>
    public int currentDay => _currentDay;
    /// <summary>Season that is active today.</summary>
    public SeasonManager.Season currentSeason => _currentSeason;
    /// <summary>How many more day ticks until the current season ends.</summary>
    public int daysUntilSeasonEnd => _daysUntilSeasonEnd;
    /// <summary>Godi knowledge level 0-3.</summary>
    public int godiLevel => _godiLevel;
    /// <summary>Number of calendar entries that are visible (not fogged).</summary>
    public int scoutedRange => _scoutedRange;

    // ── Calendar window ───────────────────────────────────────────────────────────

    private CalendarDayData[] _days = new CalendarDayData[30];

    /// <summary>30-entry calendar window. Index 0 = today, index 29 = furthest day.</summary>
    public CalendarDayData[] Days => _days;

    // ── Events ────────────────────────────────────────────────────────────────────

    /// <summary>Fires after every daily shift or full rebuild. UI subscribes here.</summary>
    public event Action OnCalendarUpdated;

    // ─────────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Subscribe and build the initial calendar.
    /// Call this AFTER SeasonManager.Initialize() in the game bootstrap sequence.
    /// </summary>
    public void Initialize()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay += OnNewDay;
        else
            Debug.LogWarning("CalendarManager: DayNightManager not found during Initialize!");

        RefreshCalendarData();
    }

    private void OnDestroy()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay -= OnNewDay;
    }

    // ── Day tick ──────────────────────────────────────────────────────────────────

    private void OnNewDay()
    {
        // Shift the window forward by one day.
        // SeasonManager.OnNewDay has already fired (subscribed first), so season state is current.
        for (int i = 0; i < 29; i++)
            _days[i] = _days[i + 1];

        SyncCurrentState();

        _days[29] = GenerateDayData(29);

        // Re-stamp fog on all entries: indices all shifted by one so boundary moved.
        for (int i = 0; i < 30; i++)
            _days[i].isFogged = i >= _scoutedRange;

        OnCalendarUpdated?.Invoke();
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rebuild the full 30-day calendar from scratch.
    /// Call on load-game and after Godi upgrades.
    /// </summary>
    public void RefreshCalendarData()
    {
        SyncCurrentState();
        for (int i = 0; i < 30; i++)
            _days[i] = GenerateDayData(i);
        OnCalendarUpdated?.Invoke();
    }

    /// <summary>
    /// Fires OnCalendarUpdated without rebuilding data.
    /// Called by event schedulers (StormScheduler, etc.) after writing events into Days entries,
    /// so the UI sees the final state in one pass rather than triggering a full rebuild.
    /// </summary>
    public void NotifyUpdated() => OnCalendarUpdated?.Invoke();

    /// <summary>
    /// Set the Godi level (0-3), recalculate scouted range, and rebuild the calendar.
    /// </summary>
    public void UpgradeGodi(int level)
    {
        _godiLevel    = Mathf.Clamp(level, 0, 3);
        _scoutedRange = _godiLevel switch
        {
            0 => 15,
            1 => 20,
            2 => 25,
            3 => 30,
            _ => 15
        };
        RefreshCalendarData();
    }

    // ── Internal ──────────────────────────────────────────────────────────────────

    private void SyncCurrentState()
    {
        if (SeasonManager.Instance == null) return;

        _currentSeason      = SeasonManager.Instance.GetCurrentSeason();
        _daysUntilSeasonEnd = SeasonManager.Instance.GetDaysUntilSeasonChange();

        // Day within current season, 0-indexed (0 = first day of season).
        int seasonLen = GetSeasonLength(_currentSeason);
        _currentDay   = Mathf.Max(0, seasonLen - _daysUntilSeasonEnd);
    }

    private CalendarDayData GenerateDayData(int index)
    {
        var entry = new CalendarDayData
        {
            events          = new List<CalendarEventData>(),
            raidThreat      = RaidThreatLevel.None,
            hasUnknownEvent = false,
            isFogged        = index >= _scoutedRange
        };

        // Walk forward through season blocks to determine which season index falls in.
        SeasonManager.Season simSeason = _currentSeason;
        int daysLeft = _daysUntilSeasonEnd; // remaining days in current season

        if (index <= daysLeft)
        {
            // Still inside the current season (daysLeft = remaining ticks including today's).
            entry.season             = simSeason;
            entry.isSeasonTransition = false;
        }
        else
        {
            // Past the current season — advance into subsequent seasons.
            // offset 0 = first day of the new season (index == daysLeft + 1).
            int offset       = index - daysLeft - 1;
            simSeason        = FlipSeason(simSeason);
            bool isTransition = offset == 0; // first day of the incoming season

            while (offset >= GetSeasonLength(simSeason))
            {
                offset       -= GetSeasonLength(simSeason);
                simSeason     = FlipSeason(simSeason);
                isTransition  = offset == 0;
            }

            entry.season             = simSeason;
            entry.isSeasonTransition = isTransition;
        }

        return entry;
    }

    private static SeasonManager.Season FlipSeason(SeasonManager.Season s) =>
        s == SeasonManager.Season.Summer
            ? SeasonManager.Season.Winter
            : SeasonManager.Season.Summer;

    // TODO: Age progression will set summer and winter lengths separately via SeasonManager.
    // For now both seasons share daysPerSeason.
    private int GetSeasonLength(SeasonManager.Season _) =>
        Mathf.Max(1, SeasonManager.Instance != null ? SeasonManager.Instance.daysPerSeason : 30);

    #region ISaveable implementation
    public void PopulateSaveData(SaveData data)
    {
        data.calendarDays = _days;
        data.currentGodiLevel = _godiLevel;
    }

    public void LoadSaveData(SaveData data)
    {
        _godiLevel = data.currentGodiLevel;
        _scoutedRange = _godiLevel switch
        {
            0 => 15,
            1 => 20,
            2 => 25,
            3 => 30,
            _ => 15
        };
        _days = data.calendarDays ?? new CalendarDayData[30];
        SyncCurrentState();
        OnCalendarUpdated?.Invoke();
    }
    #endregion
}
