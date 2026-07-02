using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a random storm schedule at the start of each winter and exposes
/// it to SeasonManager (wood multiplier) and CalendarManager (event data).
///
/// Storms use 1-indexed winter day numbers (day 1 = first ConsumeFirewood tick
/// after the season change). GetCurrentDayWoodMultiplier() reads directly from
/// SeasonManager because it is called before CalendarManager has shifted its
/// window on each OnNewDay tick.
///
/// Initialization order in GameSceneBootstrap:
///   SeasonManager → CalendarManager → StormScheduler
/// </summary>
public class StormScheduler : MonoBehaviour, ISaveable
{
    public static StormScheduler Instance { get; private set; }

    // ── Storm data ────────────────────────────────────────────────────────────

    public enum StormSeverity { Light, Heavy }

    private struct StormEntry
    {
        public int           startDay;  // 1-indexed winter day
        public int           duration;  // number of days the storm lasts
        public StormSeverity severity;  // derived from duration at generation time

        public int EndDay => startDay + duration - 1;
    }

    // ── Serialised parameters ─────────────────────────────────────────────────

    [Header("Schedule")]
    [SerializeField] private int minStormsPerWinter    = 1;
    [SerializeField] private int maxStormsPerWinter    = 4;
    [SerializeField] private int minStormDuration      = 1;
    [SerializeField] private int maxStormDuration      = 3;
    [SerializeField] private int minGapBetweenStorms   = 3;

    [Header("Wood Multipliers")]
    [SerializeField] private float stormLightWoodMultiplier = 1.5f;
    [SerializeField] private float stormHeavyWoodMultiplier = 2.5f;

    [Header("Cold Day Weights")]
    [Tooltip("Cold/Frozen roll chance per season quarter (x = Cold, y = Frozen; index 0 = first quarter, 3 = last). " +
             "Literal probabilities, not normalized — whatever's left over (1 - Cold - Frozen) is a Chilly day " +
             "(no cold effect), matching the winter shade with no separate indicator needed.")]
    [SerializeField] private Vector2 q1ColdWeights = new Vector2(0.30f, 0.00f); // Cold, Frozen
    [SerializeField] private Vector2 q2ColdWeights = new Vector2(0.45f, 0.15f);
    [SerializeField] private Vector2 q3ColdWeights = new Vector2(0.40f, 0.40f);
    [SerializeField] private Vector2 q4ColdWeights = new Vector2(0.30f, 0.60f);

    // ── Runtime state ─────────────────────────────────────────────────────────

    private readonly List<StormEntry> _stormSchedule = new List<StormEntry>();
    // Cold day type per winter, keyed by absolute DayNightManager day number (unlike _stormSchedule,
    // which is keyed by 1-indexed winter day). A plain per-day lookup — no range math needed at query time.
    private readonly Dictionary<int, ColdDayType> _coldDaySchedule = new Dictionary<int, ColdDayType>();
    private bool _isWinter;

    // ─────────────────────────────────────────────────────────────────────────

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
    /// Subscribe to season and calendar events.
    /// Must be called after CalendarManager.Initialize() in the bootstrap sequence.
    /// </summary>
    public void Initialize()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged += OnSeasonChanged;
        else
            Debug.LogError("StormScheduler: SeasonManager is null during Initialize — storm multipliers will not fire.");

        if (CalendarManager.Instance != null)
            CalendarManager.Instance.OnCalendarUpdated += OnCalendarUpdated;
        else
            Debug.LogError("StormScheduler: CalendarManager is null during Initialize — storm events will not appear on calendar.");

        // Sync with the season that is already active when Initialize is called.
        // Skip schedule generation when loading a save — LoadSaveData() will reconstruct
        // the schedule from the already-loaded calendar days instead.
        if (SeasonManager.Instance != null)
        {
            _isWinter = SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
            bool isNewGame = GameManager.Instance == null || !GameManager.Instance.ShouldLoadSave;
            if (_isWinter && isNewGame)
            {
                int seasonLen = SeasonManager.Instance.daysPerSeason;
                GenerateStormSchedule();
                GenerateColdDaySchedule(seasonLen);
                // CalendarManager already fired OnCalendarUpdated during its own Initialize(),
                // before we subscribed — so we missed the handshake. Write storm/cold data directly
                // now using the same re-entrancy guard the event handler uses.
                WriteWinterCalendarDataAndNotify();
            }
        }
    }

    private void OnDestroy()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
        if (CalendarManager.Instance != null)
            CalendarManager.Instance.OnCalendarUpdated -= OnCalendarUpdated;
    }

    // ── Season events ─────────────────────────────────────────────────────────

    private void OnSeasonChanged(SeasonManager.Season newSeason)
    {
        _isWinter = newSeason == SeasonManager.Season.Winter;

        if (_isWinter)
        {
            int seasonLen = SeasonManager.Instance != null ? SeasonManager.Instance.daysPerSeason : 30;
            GenerateStormSchedule();
            GenerateColdDaySchedule(seasonLen);
        }
        else
        {
            _stormSchedule.Clear();
            _coldDaySchedule.Clear(); // Chilly is the default for summer — no schedule needed.
        }
    }

    // ── Calendar events ───────────────────────────────────────────────────────

    private void OnCalendarUpdated()
    {
        if (!_isWinter) return;
        WriteWinterCalendarDataAndNotify();
    }

    /// <summary>
    /// Writes storm and cold-day data into the calendar window and notifies once.
    /// Shared by Initialize() (mid-winter new game) and OnCalendarUpdated() (daily shift/rebuild).
    /// </summary>
    private void WriteWinterCalendarDataAndNotify()
    {
        if (CalendarManager.Instance == null) return;
        if (_stormSchedule.Count == 0 && _coldDaySchedule.Count == 0) return;

        // Unsubscribe before writing so the subsequent NotifyUpdated() call does not
        // re-enter this handler. Resubscribe afterwards so future daily shifts work.
        CalendarManager.Instance.OnCalendarUpdated -= OnCalendarUpdated;
        if (_stormSchedule.Count > 0) WriteStormEventsToCalendar();
        if (_coldDaySchedule.Count > 0) WriteColdDayTypesToCalendar();
        CalendarManager.Instance.NotifyUpdated();
        CalendarManager.Instance.OnCalendarUpdated += OnCalendarUpdated;
    }

    // ── Wood multiplier (called from SeasonManager.ConsumeFirewood) ───────────

    /// <summary>
    /// Returns the wood consumption multiplier for the current winter day.
    /// 1.0 on clear days; stormLightWoodMultiplier or stormHeavyWoodMultiplier during storms.
    /// Called inside SeasonManager.OnNewDay BEFORE CalendarManager shifts its window,
    /// so this reads from SeasonManager directly rather than CalendarManager.currentDay.
    /// </summary>
    public float GetCurrentDayWoodMultiplier()
    {
        if (!_isWinter || _stormSchedule.Count == 0)
            return 1f;

        int currentWinterDay = GetCurrentWinterDay();

        foreach (var storm in _stormSchedule)
        {
            if (currentWinterDay >= storm.startDay && currentWinterDay <= storm.EndDay)
                return storm.severity == StormSeverity.Heavy
                    ? stormHeavyWoodMultiplier
                    : stormLightWoodMultiplier;
        }

        return 1f;
    }

    // ── Simulator range query ─────────────────────────────────────────────────

    /// <summary>
    /// Returns every absolute day in [startDay, endDay] that has a storm, with its wood multiplier.
    /// startDay/endDay are absolute DayNightManager day numbers.
    /// The conversion to 1-indexed winter days is done relative to the current winter day and
    /// current absolute day — call this before SeasonManager.AdvanceDays() to get the correct mapping.
    /// Returns an empty list if not winter, schedule is empty, or DayNightManager is unavailable.
    /// </summary>
    public List<(int day, float woodMultiplier)> GetStormDaysInRange(int startDay, int endDay)
    {
        var result = new List<(int day, float woodMultiplier)>();
        if (!_isWinter || _stormSchedule.Count == 0) return result;
        if (DayNightManager.Instance == null)
        {
            Debug.LogWarning("StormScheduler.GetStormDaysInRange: DayNightManager unavailable — returning no storm days.");
            return result;
        }

        int currentWinterDay = GetCurrentWinterDay();
        int currentAbsDay    = DayNightManager.Instance.CurrentAbsoluteDay;
        // absolute_day = currentAbsDay + (winterDay - currentWinterDay)
        // winterDay    = currentWinterDay + (absolute_day - currentAbsDay)
        int winterStartDay = currentWinterDay + (startDay - currentAbsDay);
        int winterEndDay   = currentWinterDay + (endDay   - currentAbsDay);

        foreach (var storm in _stormSchedule)
        {
            int overlapStart = Mathf.Max(storm.startDay, winterStartDay);
            int overlapEnd   = Mathf.Min(storm.EndDay,   winterEndDay);
            for (int winterDay = overlapStart; winterDay <= overlapEnd; winterDay++)
            {
                int absDay = currentAbsDay + (winterDay - currentWinterDay);
                float multiplier = storm.severity == StormSeverity.Heavy
                    ? stormHeavyWoodMultiplier
                    : stormLightWoodMultiplier;
                result.Add((absDay, multiplier));
            }
        }

        return result;
    }

    // ── Cold day type ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the scheduled cold day type for an absolute DayNightManager day number.
    /// Falls back to Chilly if the day has no entry (summer days, or days outside the
    /// currently-generated winter schedule) — Chilly is the universal default, not an "unknown" state.
    /// </summary>
    public ColdDayType GetColdDayType(int absoluteDay)
    {
        return _coldDaySchedule.TryGetValue(absoluteDay, out ColdDayType type) ? type : ColdDayType.Chilly;
    }

    /// <summary>
    /// Rolls whether a single winter day is Cold or Frozen. Weights are literal probabilities
    /// (not normalized to sum to 1) — whatever isn't covered by Cold + Frozen is left unscheduled,
    /// which GetColdDayType() reports as Chilly (no cold effect) via its default fallback. This is
    /// the same sparse-schedule shape storms use: most days simply have no entry.
    /// Weighting skews harsher as the season progresses (four quarters, each with its own weights).
    /// Frozen is excluded entirely in the first quarter regardless of configured weight.
    /// TODO (Fimbulwinter / Age progression): scale weights harsher per Age here — see handoff note.
    /// </summary>
    private ColdDayType? RollColdDayType(int dayIndex, int seasonLengthDays)
    {
        int quarter = Mathf.Clamp(dayIndex * 4 / Mathf.Max(1, seasonLengthDays), 0, 3);
        Vector2 weights = quarter switch
        {
            0 => q1ColdWeights,
            1 => q2ColdWeights,
            2 => q3ColdWeights,
            _ => q4ColdWeights
        };

        float coldWeight   = weights.x;
        float frozenWeight = quarter == 0 ? 0f : weights.y; // hard rule: no Frozen in the first quarter

        float roll = UnityEngine.Random.value;
        if (roll < coldWeight) return ColdDayType.Cold;
        if (roll < coldWeight + frozenWeight) return ColdDayType.Frozen;
        return null; // leftover probability — no schedule entry, falls back to Chilly (no cold effect)
    }

    /// <summary>
    /// Generates the cold day schedule for the winter that just started, one roll per day.
    /// Sparse — only days that roll Cold or Frozen get an entry (see RollColdDayType). Keyed by
    /// absolute day so GetColdDayType() is a plain O(1) lookup with no winter-day math at query
    /// time — the anchor (season start) is derived once here via GetCurrentWinterDay(), the same
    /// helper storms use, so it's correct whether called on the transition tick or mid-winter
    /// (new game started partway through winter).
    /// </summary>
    private void GenerateColdDaySchedule(int seasonLengthDays)
    {
        _coldDaySchedule.Clear();

        if (DayNightManager.Instance == null)
        {
            Debug.LogWarning("StormScheduler: DayNightManager unavailable — cannot generate cold day schedule.");
            return;
        }

        int currentAbsDay        = DayNightManager.Instance.CurrentAbsoluteDay;
        int currentWinterDay     = GetCurrentWinterDay(); // 0 on the transition tick itself
        int seasonStartAbsoluteDay = currentAbsDay - currentWinterDay;

        for (int dayIndex = 0; dayIndex < seasonLengthDays; dayIndex++)
        {
            ColdDayType? type = RollColdDayType(dayIndex, seasonLengthDays);
            if (type.HasValue)
                _coldDaySchedule[seasonStartAbsoluteDay + dayIndex] = type.Value;
        }
    }

    /// <summary>
    /// Writes the scheduled cold day type into every winter day currently visible in the
    /// calendar window. Unlike storms, cold day type is never concealed at the data layer on
    /// fogged days — DayEntryUI conceals it visually instead (see Change 5) — because every
    /// winter day has a real type (no "no cold day" state to hide behind hasUnknownEvent).
    /// </summary>
    private void WriteColdDayTypesToCalendar()
    {
        if (CalendarManager.Instance == null || DayNightManager.Instance == null) return;

        CalendarDayData[] days         = CalendarManager.Instance.Days;
        int               currentAbsDay = DayNightManager.Instance.CurrentAbsoluteDay;

        for (int i = 0; i < days.Length; i++)
        {
            CalendarDayData day = days[i];
            if (day == null || day.season != SeasonManager.Season.Winter) continue;

            day.coldDayType = GetColdDayType(currentAbsDay + i);
        }
    }

    // ── Schedule generation ───────────────────────────────────────────────────

    private void GenerateStormSchedule()
    {
        _stormSchedule.Clear();

        int seasonLen  = SeasonManager.Instance != null ? SeasonManager.Instance.daysPerSeason : 30;
        int stormCount = UnityEngine.Random.Range(minStormsPerWinter, maxStormsPerWinter + 1);

        // Pick all durations up front so we know the total space required before placing anything.
        int[] durations = new int[stormCount];
        int totalStormDays = 0;
        for (int i = 0; i < stormCount; i++)
        {
            durations[i] = UnityEngine.Random.Range(minStormDuration, maxStormDuration + 1);
            totalStormDays += durations[i];
        }

        int totalGapDays  = (stormCount - 1) * minGapBetweenStorms;
        int totalRequired = totalStormDays + totalGapDays;
        int slack         = seasonLen - totalRequired;

        if (slack < 0)
        {
            Debug.LogError(
                $"[StormScheduler] Cannot fit {stormCount} storm(s) into {seasonLen} days. " +
                $"Required {totalRequired} (storms: {totalStormDays}d, gaps: {totalGapDays}d) " +
                $"but only {seasonLen} available. " +
                $"Reduce maxStormsPerWinter, maxStormDuration, or minGapBetweenStorms in the inspector.");
            return;
        }

        // Distribute the slack randomly across the (stormCount + 1) padding slots:
        // slot 0 = free days before the first storm
        // slot i = free days between storm i-1 and storm i  (i = 1..stormCount-1)
        // slot n = free days after the last storm
        int[] padding = new int[stormCount + 1];
        for (int i = 0; i < slack; i++)
            padding[UnityEngine.Random.Range(0, padding.Length)]++;

        // Build the schedule from padding + fixed structure.
        int day = 1 + padding[0]; // 1-indexed start
        for (int i = 0; i < stormCount; i++)
        {
            _stormSchedule.Add(new StormEntry
            {
                startDay = day,
                duration = durations[i],
                severity = durations[i] >= 2 ? StormSeverity.Heavy : StormSeverity.Light
            });

            if (i < stormCount - 1)
                day += durations[i] + minGapBetweenStorms + padding[i + 1];
        }

        Debug.Log(
            $"[StormScheduler] Scheduled {_stormSchedule.Count} storm(s) for this winter " +
            $"(season {seasonLen}d, used {totalRequired}d, {slack}d slack):");
        foreach (var s in _stormSchedule)
            Debug.Log($"  Days {s.startDay}–{s.EndDay} | {s.severity} | {s.duration}d");
    }

    // ── Calendar writing ──────────────────────────────────────────────────────

    private void WriteStormEventsToCalendar()
    {
        if (CalendarManager.Instance == null) return;

        CalendarDayData[] days    = CalendarManager.Instance.Days;
        int               today   = CalendarManager.Instance.currentDay; // 0 on transition day, 1+ otherwise

        foreach (var storm in _stormSchedule)
        {
            for (int d = storm.startDay; d <= storm.EndDay; d++)
            {
                int calIdx = d - today;
                if (calIdx < 0 || calIdx >= days.Length) continue;

                CalendarDayData day = days[calIdx];
                if (day == null || day.season != SeasonManager.Season.Winter) continue;

                if (day.isFogged)
                {
                    day.hasUnknownEvent = true;
                }
                else
                {
                    day.events.Add(new CalendarEventData
                    {
                        eventType  = CalendarEventType.Storm,
                        eventName  = storm.severity == StormSeverity.Heavy ? "Heavy Storm" : "Light Storm",
                        isRevealed = true
                    });
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 1-indexed current winter day. Safe to call during ConsumeFirewood (post-decrement).
    /// </summary>
    private static int GetCurrentWinterDay()
    {
        if (SeasonManager.Instance == null) return 0;
        return SeasonManager.Instance.daysPerSeason - SeasonManager.Instance.GetDaysUntilSeasonChange();
    }

    // ── ISaveable ─────────────────────────────────────────────────────────────

    // Storm events and cold day types are both embedded in the saved CalendarDayData, so no
    // separate fields are written. PopulateSaveData is a no-op; LoadSaveData reconstructs
    // _stormSchedule and _coldDaySchedule from the already-loaded calendar days so that
    // GetCurrentDayWoodMultiplier()/GetColdDayType() (both read live, e.g. by
    // SettlementManager.ConsumeFirewood each day) use the correct data rather than an
    // empty/freshly-generated schedule after a load.
    public void PopulateSaveData(SaveData data) { }

    public void LoadSaveData(SaveData data)
    {
        _stormSchedule.Clear();
        _coldDaySchedule.Clear();

        if (SeasonManager.Instance != null)
            _isWinter = SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;

        if (!_isWinter || CalendarManager.Instance == null) return;

        CalendarDayData[] days = CalendarManager.Instance.Days;
        int currentWinterDay = GetCurrentWinterDay();
        int currentAbsDayForColdLoad = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentAbsoluteDay : -1;

        // Cold day type has no "unknown" concealment (see WriteColdDayTypesToCalendar), so it can
        // be reconstructed directly from every winter day's stored value, fogged or not. Only
        // Cold/Frozen get an entry — Chilly stays unscheduled, matching the sparse generation model.
        if (currentAbsDayForColdLoad >= 0)
        {
            for (int i = 0; i < days.Length; i++)
            {
                CalendarDayData day = days[i];
                if (day == null || day.season != SeasonManager.Season.Winter) continue;
                if (day.coldDayType == ColdDayType.Chilly) continue;
                _coldDaySchedule[currentAbsDayForColdLoad + i] = day.coldDayType;
            }
        }

        StormEntry? active = null;
        for (int i = 0; i < days.Length; i++)
        {
            CalendarDayData day = days[i];
            if (day == null || day.season != SeasonManager.Season.Winter || day.isFogged)
            {
                if (active.HasValue) { _stormSchedule.Add(active.Value); active = null; }
                continue;
            }

            bool hasStorm = false;
            StormSeverity sev = StormSeverity.Light;
            foreach (var ev in day.events)
            {
                if (ev.eventType == CalendarEventType.Storm)
                {
                    hasStorm = true;
                    if (ev.eventName == "Heavy Storm") sev = StormSeverity.Heavy;
                    break;
                }
            }

            int winterDay = currentWinterDay + i;
            if (hasStorm)
            {
                if (!active.HasValue)
                    active = new StormEntry { startDay = winterDay, duration = 1, severity = sev };
                else
                {
                    var s = active.Value;
                    s.duration++;
                    active = s;
                }
            }
            else if (active.HasValue)
            {
                _stormSchedule.Add(active.Value);
                active = null;
            }
        }
        if (active.HasValue) _stormSchedule.Add(active.Value);
    }
}
