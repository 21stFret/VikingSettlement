# Viking Settlement — Manager Architecture
_Last verified: 2026-06-27. Re-verify file:line citations before acting on them._

---

## Initialization Sequence (every game-scene load)

`GameSceneBootstrap` (`[DefaultExecutionOrder(-1000)]`) is found by `GameManager.OnSceneLoaded` via `FindAnyObjectByType` and runs `Init()` after a 2-frame delay.

```
GameTickManager → DayNightManager → SeasonManager → CalendarManager → StormScheduler
→ WeatherManager → SettlementManager → MissionManager → BeehiveManager
→ JarlManager.Init() → [spawn/restore villagers] → JarlManager.Init() (duplicate — see BUGS.md #B3)
→ EnsureInitialJarl → SkillTreeManager → AttackCooldownUI
→ MouseInputController → CameraController → PauseManager → UIManager → HeatUI
→ [new game: SaveToCurrentSlot]
```

**Critical ordering constraint:** `DayNightManager.OnNewDay` subscribers are called in C# multicast order (= subscription order). SeasonManager must subscribe before CalendarManager — guaranteed by current Bootstrap order — because CalendarManager.OnNewDay reads SeasonManager state that SeasonManager.OnNewDay has already updated.

---

## Manager Lifecycle

| Manager | DDOL | Init by Bootstrap | ISaveable |
|---------|------|-------------------|-----------|
| GameManager | ✓ | no (self-init) | no |
| SaveManager | ✓ | no (self-init) | no |
| WeatherManager | ✓ | yes (re-wires per scene) | no |
| RaidManager | ✓ | no (self-init) | no |
| GameTickManager | scene-local | yes | no |
| DayNightManager | scene-local | yes | yes |
| SeasonManager | scene-local | yes | yes |
| CalendarManager | scene-local | yes | no (derived, rebuilt each load) |
| StormScheduler | scene-local (`Calendar/`) | yes | no (regenerated — see BUGS.md #G16) |
| SettlementManager | scene-local | yes | yes |
| ResourceManager | scene-local | no (Awake) | yes |
| JarlManager | scene-local | yes | yes |
| MissionManager | scene-local | yes | yes |
| PauseManager | scene-local | yes | no |
| SkillTreeManager | scene-local | yes | yes |
| RunestoneManager | scene-local | no (Awake) | yes |
| DeathTypeBuff | scene-local | no (Update-driven) | yes |
| BeehiveManager | scene-local | yes (no singleton) | no |
| WoundManager | scene-local | no (Awake) | no |
| LootDropManager | scene-local | no (Awake/Start) | no |
| ValkyrieManager | scene-local | no (Awake) | no |
| VillagerSpawner | scene-local | no (Awake) | no |
| UIManager | scene-local | yes (last) | no |

---

## Event Contract Map

```
GameTickManager
  .OnGameTick      → DayNightManager, SettlementManager, MissionManager
  .OnFastUpdate    → DayNightManager, SeasonManager, SettlementManager

DayNightManager
  .OnNewDay              → SeasonManager, CalendarManager, MissionManager, WeatherManager
  .OnMealTime            → SettlementManager
  .OnDayNightChanged     → WeatherManager (body is fully commented out — NOOP)
  .OnDawnEveningChanged  → WeatherManager, BeehiveManager
                           ⚠ fires every frame during ±5% sunrise/sunset window, not once (see BUGS.md #B9)

SeasonManager
  .OnSeasonChanged  → StormScheduler
  .OnWarmthChanged  → FireController, HeatUI
                      [no manager subscribers — UI side only]

CalendarManager
  .OnCalendarUpdated → StormScheduler

JarlManager
  .OnJarlDied        → SettlementManager, SkillTreeManager
  .OnJarlChanged     → SettlementManager
  .OnSuccessionStarted → SuccessionUI
  .OnSuccessionEnded   → SuccessionUI
                         ⚠ NOT fired in only-children branch (see BUGS.md #B32)

RaidManager
  .OnRaidEnded → MissionManager

RunestoneManager
  .OnSelectionComplete → JarlManager (wired dynamically in SelectHeir)
  .OnSelectionStarted  → RunestoneUI

Building (static events)
  .OnAnyBuildingRepaired → MissionManager
  .OnAnyWorkerAssigned   → MissionManager

SettlementManager
  .OnFoodConsumed → [no manager subscriber — UI side only]

DeathTypeBuff
  .OnBuffChanged → [no manager subscriber — UI side only]

SkillTreeManager
  .OnXPChanged    → XP UI
  .OnSkillUnlocked → SkillTree UI
  .OnSkillsReset  → XP UI
```

---

## Per-Manager Notes

### GameManager (DDOL)
Orchestrates scene loads and the save/load/new-game flow. Finds Bootstrap via `FindAnyObjectByType` each load. Does not implement ISaveable — owns SaveManager calls instead.

### GameTickManager
Drives the simulation clock. `OnGameTick` at `tickRate` Hz (default 1). `OnFastUpdate` every frame. Scene-local but its `Awake` destroys the *old* Instance rather than the incoming duplicate (opposite of all other managers — harmless in practice). Not saved; tick rate resets to 1 on reload.

### DayNightManager
Owns the day/night cycle and lighting. Saves `currentTimeOfDay` and `currentDay`. `hasConsumedMealToday` reconstructed from `currentTimeOfDay > mealTime` on load — can skip one meal if saved between midnight and mealTime.

`eveningMultiplier` (line 88) is written by SeasonManager's `ApplySummerLighting`/`ApplyWinterLighting` but **never read anywhere in DayNightManager** — seasonal ambient multipliers have zero effect on lighting. See BUGS.md #B4.

### SeasonManager
Owns seasonal economy: production multipliers, wood/warmth consumption. Visual effects removed 2026-06-27 — WeatherManager owns all particles/lights now. `GetTodayWoodCost()` and the consumption logic in `ConsumeFirewood()` duplicate the same formula and must be kept in sync. `ConsumeFirewood` is only called when `currentSeason == Winter`, making the 0.5f summer branch inside it dead code.

### CalendarManager
Derived state — not saved. Rebuilt from SeasonManager state on every `Initialize()`. Uses same `daysPerSeason` value for both Summer and Winter (TODO: separate lengths for Age progression).

### StormScheduler
Located in `Calendar/` not `Managers/`. Storm schedule is **not saved** — a fresh random schedule is generated on every `Initialize()` during winter, desync between saves (see BUGS.md #G16).

### WeatherManager (DDOL)
Owns all visual weather effects. Spawns prefab instances (rain, snow, sun beams, sun dust, fireflies, lightning) parented to the camera. Re-subscribes to the incoming scene's DayNightManager on every `Initialize()` — safe because the old DayNightManager is destroyed with the scene. `autoWeather` and `UpdateWeatherDuration()` are entirely commented out — the manual/auto weather API does nothing.

### SettlementManager
Owns villager and building lists. Most complex ISaveable: saves villager stats/skills/lineage/equipment/wounds, building state, worker assignments. Building-to-worker restoration uses SO asset name as key — two buildings of the same type can have workers cross-assigned on load (Gap #G22). Debug `OnGUI` panel is on by default and runs in all builds (see BUGS.md #B23).

### SettlementSimulator
Static class — no MonoBehaviour. Computes "what happened while you were on a raid." Three divergences from live gameplay that cause wrong results: day length hard-coded to 120 s, uses Wheat as food (live uses Fish), and firewood ignores StormScheduler+RunestoneManager multipliers. See BUGS.md #B8, #B9sim, #B10sim.

### ResourceManager
`OnResourceAdded` is a **static event** — never cleared across scene loads, accumulates ghost subscribers (see BUGS.md #B12).

### JarlManager
Manages current Jarl, succession, and runestone selection trigger. VillagerAI/JarlAI component toggle on `SetJarl()`. Succession coroutine has unreachable timeout (BUGS.md #B31). Only-children path exits without firing `OnSuccessionEnded` (BUGS.md #B32).

### RaidManager (DDOL)
`pendingRaidResults` lives in memory between scenes; lost if app crashes mid-raid. If Jarl is a raid casualty, `ApplyPendingResults` fires `Villager.Die()` before Bootstrap has initialised the UI — succession is silently skipped (BUGS.md #B37).

### PauseManager
States: `Playing`, `MenuPause`, `StrategicPause`, `DialoguePause`. `ExitDialoguePause` always restores to `Playing`, ignoring `stateBeforeDialogue` — strategic pause is lost if dialogue fires during it (BUGS.md #B21). `settingsButton` accessed without null check (BUGS.md #B41).

Camera fix applied 2026-06-27: `CleanupStrategicPause` now calls `cameraController.ReturnToPlayerTarget()` instead of restoring a cached (stale) transform. See bug_history for detail.

### MissionManager
`missionTrackerUI.Init()` called in `Awake()` before Bootstrap — any reads of other managers inside that Init see uninitialised state (BUGS.md #B34). Gather objectives track current held amount, not total gathered — player can "fail" by spending resources after meeting the goal.

### SkillTreeManager
Subscribes to `JarlManager.OnJarlDied` with a method named `OnJarlChanged` — naming inconsistency (BUGS.md #B26). `OnXPChanged` fires during `LoadSaveData` before UI has subscribed (BUGS.md #B27).

### RunestoneManager
`SelectRunestone` when at capacity silently skips `CompleteSelection()` — JarlManager blocks indefinitely waiting for selection (BUGS.md #B28).

---

## Jarl / VillagerAI Component Toggle

Villager prefab has BOTH `VillagerAI` (enabled by default) and `JarlAI` (disabled by default).

`JarlManager.SetJarl()`:
- Becoming Jarl: `VillagerAI.enabled = false`, `JarlAI.enabled = true`
- Losing Jarl: `JarlAI.enabled = false`, `VillagerAI.enabled = true`

`PlayerController` finds the active AI via `GetComponents<VillagerAIBase>()` + `Array.Find(ai => ai.enabled)`, then calls `SetAIEnabled(false)` when taking control.

`JarlAI` overrides: `DetectionRange = 12f`, `FleeHealthThreshold = 15%`, `IsCombatJob() => true`.

---

## AI Hierarchy

```
CharacterAI (abstract MonoBehaviour — FSM engine, AI/CharacterAI.cs)
├── EnemyAIBase (Enemy/EnemyAIBase.cs)
│   └── EnemyAI (Enemy/EnemyAI.cs)
└── VillagerAIBase (Villager/VillagerAIBase.cs)
    ├── VillagerAI (Villager/VillagerAI.cs — standard, job-based)
    └── JarlAI    (Villager/JarlAI.cs — always combat, higher detection)
```

State classes in `Assets/Scripts/AI/States/`:
- Enemy: `IdleState`, `WanderState`, `ChaseState`, `CombatState`, `FleeState`, `SearchState`, `ReturnToSpawnState`
- Villager: `VillagerIdleState`, `VillagerWanderState`, `VillagerWorkState`, `VillagerMoveToWorkState`, `VillagerCombatState`, `VillagerPrepareCombatState`, `VillagerFleeState`, `VillagerFollowState`, `VillagerShieldWallState`, `VillagerMovingToPositionState`

---

See `BUGS.md` for the full prioritised bug and gap list.
