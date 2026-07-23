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

    private struct ColdSpellEntry
    {
        public int startDay;   // 1-indexed winter day, start of the Cold ramp stage
        public int rampDays;   // Cold, ramping up
        public int peakDays;   // Frozen, the peak of the spell
        public int taperDays;  // Cold, easing back down

        public int TotalDays => rampDays + peakDays + taperDays;
        public int EndDay => startDay + TotalDays - 1;
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

    [Header("Cold Spells")]
    [Tooltip("Each cold spell is a contiguous cold snap: it ramps from Cold up to Frozen, then eases " +
             "back down through Cold before returning to a normal (Chilly) day. Days outside any spell " +
             "default to Chilly. Number of spells scheduled per winter.")]
    [SerializeField] private int minColdSpellsPerWinter = 2;
    [SerializeField] private int maxColdSpellsPerWinter = 5;

    [Tooltip("Days spent at the Cold stage on the way up to Frozen, and again (rolled independently) " +
             "on the way back down — a spell need not be symmetric.")]
    [SerializeField] private int minColdStageDays = 1;
    [SerializeField] private int maxColdStageDays = 2;

    [Tooltip("Days spent at the Frozen peak in the middle of a spell.")]
    [SerializeField] private int minFrozenStageDays = 1;
    [SerializeField] private int maxFrozenStageDays = 2;

    [SerializeField] private int minGapBetweenColdSpells = 2;

    [Tooltip("Relative weight of a spell landing in each quarter of winter (index 0 = first quarter, " +
             "3 = last). Higher = more spells that quarter. Defaults skew spells toward late winter, " +
             "so cold spells get more frequent as the season progresses.")]
    [SerializeField] private float q1SpellWeight = 1f;
    [SerializeField] private float q2SpellWeight = 2f;
    [SerializeField] private float q3SpellWeight = 3f;
    [SerializeField] private float q4SpellWeight = 4f;

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
    /// Generates the cold day schedule for the winter that just started, as a set of contiguous
    /// cold spells (Cold ramp → Frozen peak → Cold taper) rather than independent per-day rolls —
    /// a real cold snap always eases in and out through Cold rather than jumping straight in/out of
    /// Frozen. Keyed by absolute day so GetColdDayType() is a plain O(1) lookup with no winter-day
    /// math at query time — the anchor (season start) is derived once here via GetCurrentWinterDay(),
    /// the same helper storms use, so it's correct whether called on the transition tick or
    /// mid-winter (new game started partway through winter). Days outside any spell are left
    /// unscheduled and fall back to Chilly via GetColdDayType()'s default.
    /// TODO (Fimbulwinter / Age progression): scale spell count/severity harsher per Age here.
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

        List<ColdSpellEntry> spells = PlaceColdSpells(seasonLengthDays);

        foreach (var spell in spells)
        {
            int day = spell.startDay; // 1-indexed
            for (int i = 0; i < spell.rampDays; i++)
                _coldDaySchedule[seasonStartAbsoluteDay + (day++ - 1)] = ColdDayType.Cold;
            for (int i = 0; i < spell.peakDays; i++)
                _coldDaySchedule[seasonStartAbsoluteDay + (day++ - 1)] = ColdDayType.Frozen;
            for (int i = 0; i < spell.taperDays; i++)
                _coldDaySchedule[seasonStartAbsoluteDay + (day++ - 1)] = ColdDayType.Cold;
        }

        Debug.Log($"[StormScheduler] Scheduled {spells.Count} cold spell(s) for this winter:");
        foreach (var s in spells)
            Debug.Log($"  Days {s.startDay}–{s.EndDay} | Cold {s.rampDays}d → Frozen {s.peakDays}d → Cold {s.taperDays}d");
    }

    /// <summary>
    /// Randomly places cold spells across the winter, biased toward later quarters via
    /// q1-q4SpellWeight so spells get more frequent as the season progresses. Each spell is a
    /// contiguous Cold-ramp → Frozen-peak → Cold-taper block. Uses rejection sampling against
    /// already-placed spells (respecting minGapBetweenColdSpells) rather than the tight slot-packing
    /// GenerateStormSchedule uses — spell count/length here is small relative to season length, and
    /// an occasional dropped spell (logged) is an acceptable trade for simplicity.
    /// </summary>
    private List<ColdSpellEntry> PlaceColdSpells(int seasonLengthDays)
    {
        var spells = new List<ColdSpellEntry>();
        int spellCount = UnityEngine.Random.Range(minColdSpellsPerWinter, maxColdSpellsPerWinter + 1);

        float[] quarterWeights = { q1SpellWeight, q2SpellWeight, q3SpellWeight, q4SpellWeight };
        float totalWeight = quarterWeights[0] + quarterWeights[1] + quarterWeights[2] + quarterWeights[3];
        if (totalWeight <= 0f) totalWeight = 1f;

        const int maxAttemptsPerSpell = 20;

        for (int i = 0; i < spellCount; i++)
        {
            int rampDays  = UnityEngine.Random.Range(minColdStageDays, maxColdStageDays + 1);
            int peakDays  = UnityEngine.Random.Range(minFrozenStageDays, maxFrozenStageDays + 1);
            int taperDays = UnityEngine.Random.Range(minColdStageDays, maxColdStageDays + 1);
            int spellLength = rampDays + peakDays + taperDays;

            int quarter      = PickWeightedQuarter(quarterWeights, totalWeight);
            int quarterStart = quarter * seasonLengthDays / 4 + 1;
            int quarterEnd   = (quarter + 1) * seasonLengthDays / 4;

            bool placed = false;
            for (int attempt = 0; attempt < maxAttemptsPerSpell && !placed; attempt++)
            {
                int latestStart = quarterEnd - spellLength + 1;
                if (latestStart < quarterStart) break; // spell doesn't fit in this quarter at all

                int startDay = UnityEngine.Random.Range(quarterStart, latestStart + 1);
                var candidate = new ColdSpellEntry
                {
                    startDay  = startDay,
                    rampDays  = rampDays,
                    peakDays  = peakDays,
                    taperDays = taperDays
                };

                if (candidate.startDay < 1 || candidate.EndDay > seasonLengthDays) continue;

                bool overlaps = false;
                foreach (var existing in spells)
                {
                    if (candidate.startDay <= existing.EndDay + minGapBetweenColdSpells &&
                        candidate.EndDay + minGapBetweenColdSpells >= existing.startDay)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    spells.Add(candidate);
                    placed = true;
                }
            }

            if (!placed)
                Debug.LogWarning($"[StormScheduler] Could not place cold spell {i + 1}/{spellCount} (quarter {quarter + 1}) without overlap — skipped.");
        }

        spells.Sort((a, b) => a.startDay.CompareTo(b.startDay));
        return spells;
    }

    private static int PickWeightedQuarter(float[] weights, float totalWeight)
    {
        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return i;
        }
        return weights.Length - 1;
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
