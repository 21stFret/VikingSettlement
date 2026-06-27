# Viking Settlement — Bug & Gap Registry
_Deep audit: 2026-06-27. Mark bugs FIXED with date when resolved._
_P0 = breaks correctness. P1 = wrong values. P2 = state desync. P3 = code health/performance._

---

## P0 — Breaks Correctness

_All P0 bugs resolved as of 2026-06-27._

---

## P1 — Wrong Simulation Values

### B8 — SettlementSimulator hard-codes 120-second day
`float totalSeconds = days * 120f;` — should read `DayNightManager.Instance?.dayLengthInSeconds ?? 120f`. If the inspector value changes, all simulated raid production is wrong.

### B9sim — Simulator uses Wheat as food; live game uses Fish
`SimulateProductionAndConsumption` drains Fish then Wheat. `SettlementManager.HandleMealTime` only drains Fish. Wheat consumed during raids is never consumed in live play.
**Fix:** Align Simulator food logic with SettlementManager (Fish only, or whatever food type SettlementManager uses).

### B10sim — Simulator firewood ignores StormScheduler and RunestoneManager multipliers
Hard-codes `0.5f` per villager instead of reading `SeasonManager.woodPerVillagerPerDay`. Does not apply `StormScheduler.GetCurrentDayWoodMultiplier()` or `RunestoneManager.GetFirewoodConsumptionMultiplier()`. Players with Winter's Friend runestone see inflated simulated wood drain during raids.

### B25 — CeilToInt drifts season/calendar clock from fractional raid days (RaidManager)
`gameDaysPassed` is a float; `Mathf.CeilToInt` rounds up. Simulator uses the exact float; calendar and SeasonManager advance by a rounded-up integer. Season clock drifts over multiple raids.
**Fix:** Use `Mathf.RoundToInt` or derive integer days from the same source as the simulator.

---

## P2 — State Desync

### B5 — Day rollover discards fractional time (DayNightManager)
`currentTimeOfDay = 0f` on day rollover should be `currentTimeOfDay -= 1f`. Any fraction past 1.0 is lost. Error ≤ 0.8% per day at default 120 s day length.

### B9 — `OnDawnEveningChanged` fires every frame during ±5% sunrise/sunset window (DayNightManager)
Event should fire once on the transition edge, not every frame during the window. WeatherManager calls `EnableFireflies(!isDawn)` each frame during this period, toggling the particle system many times per second.
**Fix:** Add a `wasDawnEvening` bool guard (same pattern as `wasDaytime`) and fire the event only on the state change.

### G16 — Storm schedule not saved; randomised on every load mid-winter (StormScheduler)
`StormScheduler.Initialize()` calls `GenerateStormSchedule()` when `_isWinter` is true. Each load during winter shows different storms than before the save. `_stormSchedule` needs to be serialised or the schedule regenerated from a saved RNG seed.

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
