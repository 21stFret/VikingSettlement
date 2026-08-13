# Viking Settlement — Bug & Gap Registry
_Deep audit: 2026-06-27. Mark bugs FIXED with date when resolved._
_P0 = breaks correctness. P1 = wrong values. P2 = state desync. P3 = code health/performance._

---

## P0 — Breaks Correctness

_All P0 bugs resolved as of 2026-06-27._

---

## P1 — Wrong Simulation Values

### B52 — StormScheduler frozen `_isWinter` misses season-crossing raids (StormScheduler / SettlementSimulator)
`GetStormDaysInRange()` gates on `_isWinter`, which only updates via `OnSeasonChanged` — an event that `RaidManager.ApplyPendingResults()` never fires (day/season advancement during raid return is intentionally silent, see fixed-bug entry for calendar/storm sync below). `_isWinter` therefore still reflects the season *at raid departure* when the simulator runs. A raid that departs in Fall and returns after the Fall→Winter boundary has crossed will have `_isWinter == false` for the whole call, so `GetStormDaysInRange` returns an empty list and the simulated firewood consumption skips storm multipliers for the days that were actually in winter by the time the party returned.
**Fix:** Derive the season for each simulated day from `SeasonManager.daysPerSeason`/season order relative to `raidStartAbsoluteDay + dayIndex`, instead of the single frozen `_isWinter` flag, so a season boundary crossed mid-raid is accounted for.

---

## P2 — State Desync

### B21 — `ExitDialoguePause` always restores to Playing, ignores `stateBeforeDialogue` (PauseManager)
`stateBeforeDialogue` is stored in `EnterDialoguePause` but `ExitDialoguePause` always sets `currentState = PauseState.Playing`. If dialogue fires during strategic pause, strategic pause is silently dropped.

---

## P3 — Code Health / Performance

### B1 — New-game premature save before any managers or villagers exist (GameManager)
`InitializeGameAfterDelay` saves to slot before Bootstrap runs, before any villagers have been spawned. Bootstrap saves again after spawning. The first write is wasted.

### B2 — `LoadAutosave` doesn't set `CurrentSlot`; auto-save never starts (GameManager)
If called cold (app restart → Continue), `CurrentSlot` remains 0. `StartAutoSave` guards on `CurrentSlot > 0` — auto-save never activates for that session.

### B3 — `JarlManager.Init()` called twice in Bootstrap (GameSceneBootstrap)
Lines 51 and 56 of Bootstrap both call `JarlManager.Instance?.Init()`. Idempotent but a clear copy-paste artifact.

### B10 — Dead `seasonMultiplier = 0.5f` branch in `ConsumeFirewood` (SeasonManager)
`ConsumeFirewood` is only called when `currentSeason == Winter`, so `seasonMultiplier` is always 1.0. The 0.5f summer branch can never execute. Remove the ternary and hard-code 1.0f, or add a comment documenting the intentional guard.

### B11 — OnGUI debug panel enabled by default in all builds (SettlementManager)
`showPopulationUI = true` at line 38. `OnGUI` renders every frame. Must disable before shipping.

### B12 — `ResourceManager.OnResourceAdded` is a static event; accumulates ghost subscribers
Static events are never cleared across scene loads. Any scene-local subscriber that doesn't unsubscribe in `OnDestroy` becomes a ghost listener.
**Fix:** Convert to a normal instance event.

### B17 — `WeatherManager.OnDayNightChanged` subscribed but body fully commented out (WeatherManager)
Remove the subscription and empty method, or restore the body. Wasted subscription.

### B18 — `autoWeather` flag and related API do nothing (WeatherManager)
`UpdateWeatherDuration()` is entirely commented out. `SetManualWeather()`, `ResumeAutoWeather()`, `IsManualMode()` are misleading dead API.

### B23 — SaveManager slot guard mismatch (SaveManager)
`CheckCurrentScene` uses `CurrentSlot >= 0`; `StartAutoSave` uses `CurrentSlot > 0`. Outer guard should match inner intent.

### B26 — SkillTreeManager subscribes `OnJarlDied` with method named `OnJarlChanged` (SkillTreeManager)
Naming inconsistency. Should be `OnJarlDied` or `ResetXPOnJarlDeath`.

### B27 — `OnXPChanged` fires during `LoadSaveData` before UI has subscribed (SkillTreeManager)
XP UI will show default value until it initializes and subscribes post-load.

### B34 — MissionManager calls `missionTrackerUI.Init()` in `Awake()` before Bootstrap (MissionManager)
Runs before any other manager's `Initialize()`. Reads from uninitialised managers will return defaults.

### B35 — MissionManager uses `FindObjectsByType<Building>()` on mission accept (MissionManager)
Expensive scene search on every mission accept. Should use `SettlementManager.GetAllBuildings()`.

### B41 — `PauseManager.EnterMenuPause` null-dereferences `settingsButton` without null check
`UIFocus.Set(settingsButton.gameObject)` will throw if `settingsButton` is not assigned in inspector.

### B51 — `VillagerSpawner.SpawnVillager` doesn't null-check `JarlManager.Instance` (VillagerSpawner)
`JarlManager.Instance.CurrentJarl == null` will throw if JarlManager hasn't Awake'd yet.

### G22 — Building-to-worker restoration uses SO name as key, not GUID (SettlementManager)
Two buildings of the same type (e.g. two LumberCamp) share a name. Workers can be cross-assigned on load.

---

## Fixed Bugs

| Date | Bug | Summary |
|------|-----|---------|
| 2026-07-02 | B8 — SettlementSimulator hard-codes 120-second day | Now reads `DayNightManager.Instance.dayLengthInSeconds`, falls back to 120f with a warning if unavailable. |
| 2026-07-02 | B9sim — Simulator used Wheat as food; live game uses Fish | Simulator now Fish-only, matching `SettlementManager`. |
| 2026-07-02 | B10sim — Simulator firewood ignored StormScheduler and RunestoneManager multipliers | Per-day storm lookup via `StormScheduler.GetStormDaysInRange` (keyed off `raidStartAbsoluteDay`, computed before the silent day-advance so the anchor is still valid) plus `RunestoneManager.GetWoodConsumptionMultiplier()` now applied alongside season/storm multipliers. |
| 2026-07-02 | B25 — CeilToInt drifted season/calendar clock from fractional raid days | `Mathf.FloorToInt` + partial-day remainder used consistently in `RaidManager.ApplyPendingResults` and `SettlementSimulator`. |
| 2026-07-02 | G16 — Storm schedule not saved; randomised on every load mid-winter | `StormScheduler` and `CalendarManager` implement `ISaveable`; `StormScheduler.LoadSaveData` reconstructs `_stormSchedule` from the loaded calendar days instead of re-rolling. |
| 2026-06-27 | B37 — Raid Jarl-casualty succession fires before UI ready | `ApplyPendingResults()` moved to after `GameSceneBootstrap.Init()` in GameManager. |
| 2026-06-27 | B4 — `eveningMultiplier` dead field | `ambientLight.intensity` now multiplied by `eveningMultiplier` in `UpdateAmbientLight`. |
| 2026-06-27 | B5 — Day rollover discards fractional time | `currentTimeOfDay -= 1f` instead of `= 0f`. |
| 2026-06-27 | B9 — `OnDawnEveningChanged` fires every frame | Added `wasDawnEvening` edge-detection guard. |
| 2026-06-27 | B28 — At-capacity SelectRunestone skips CompleteSelection | Logs error and leaves selection open so UI can recover; no silent skip. |
| 2026-06-27 | B31 — Succession timeout unreachable | Real 120 s loop with `Time.unscaledDeltaTime`; auto-selects best candidate on expiry. |
| 2026-06-27 | B32 — Only-children branch missing `OnSuccessionEnded` | Added `OnSuccessionEnded?.Invoke()` to that branch in `HandleNoHeirs`. |
| 2026-06-27 | Camera doesn't follow new Jarl after succession | `PauseManager.CleanupStrategicPause` restored a cached pre-succession camera target. Fixed by removing the cache and using `ReturnToPlayerTarget()`. |
| 2026-06-27 | SeasonManager dead visual-effects layer | Removed 11 fields and 8 methods for particle/light effects that WeatherManager had superseded. Inspector slots were permanently empty. |
| Earlier | Raid end editor freeze | `SceneManager.LoadScene` called synchronously inside `Enemy.Die()` event chain. Fixed by deferring to a results UI button press. |
| Earlier | SeasonManager not firing on load from main menu | GameManager (DDOL) on same GameObject as scene-local managers — duplicate DDOL destroyed the whole Managers GO. Fixed by isolating GameManager onto its own root GameObject. |
