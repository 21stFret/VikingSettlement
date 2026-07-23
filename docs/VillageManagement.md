# Village Management

**Core files:** `Assets/Scripts/Building.cs`, `Assets/Scripts/BuildingData.cs`, `Assets/Scripts/Managers/SettlementManager.cs`, `Assets/Scripts/Managers/ResourceManager.cs`, `Assets/Scripts/Managers/SettlementFormulas.cs`, `Assets/Scripts/Villager/Villager.cs`, `Assets/Scripts/Villager/VillagerData.cs`

## Overview

Village management spans three linked systems: **buildings** (production sites configured by `BuildingData` ScriptableObjects), **worker assignment** (linking a `Villager` to a `Building`'s job slot), and the **resource economy** (a single unbounded `ResourceManager` ledger that every building reads/writes and that `SettlementManager` drains daily for food and firewood upkeep). `SettlementFormulas.cs` is the shared math layer — both live gameplay and `SettlementSimulator` (the off-screen raid-away simulation) call the same pure functions so the two can't drift apart.

---

## Buildings

### `BuildingType` enum

`Assets/Scripts/BuildingData.cs:90-110` — 18 values: `Longhouse, Farm, FishermansHut, LumberCamp, Sawmill, Quarry, Mine, Blacksmith, CarpenterWorkshop, WeaversHut, Tannery, Barracks, ArcheryRange, Shipyard, TradingPost, HealersHut, ShamansHut, MeadHall`.

### `BuildingData` (ScriptableObject config)

`[CreateAssetMenu(menuName = "Viking Settlement/Building")]`. One `.asset` instance per building exists in `Assets/Scripts/BuildingDatas/`. Fields:

| Field | Purpose |
|---|---|
| `buildingName`, `buildingType`, `buildingSprite` | Identity/display |
| `woodCost`, `stoneCost`, `ironCost`, `constructionTime` | Placement cost (see construction gap below) |
| `assignedJobType`, `maxWorkers` (default 1) | Worker-slot config |
| `productionType` (`ResourceGathering` / `Crafting`) | Which production path `Building.UpdateProduction` runs |
| `producedResource`, `productionRate`, `productionAmount` | Gathering-only output config |
| `craftingRecipe` (nested `CraftingRecipe` class) | Crafting-only: `inputResources` (list), `outputResource`, `outputAmount`, `craftingRate` |

Example (`Farm.asset`): `woodCost=8, stoneCost=3, maxWorkers=3, producedResource=Wheat, productionRate=0.8, productionAmount=10`.

There is no tier/upgrade system — every building is single-configuration.

### `Building` (MonoBehaviour instance)

Key fields: `uniqueId` (GUID), `data` (BuildingData ref), `gridPosition`, `isConstructed`, `constructionProgress`, `productionProgress` (0–100), `adjustedProductionAmount` (seasonally-scaled, for UI), `needsRepair` + `repairCosts[]`, `assignedWorkers` (`List<Villager>`).

**Production tick** — `UpdateProduction(deltaTime)` (`Building.cs:141-154`), called every fast-update tick from `SettlementManager.UpdateBuildingProduction`, guarded by `isConstructed && !needsRepair && assignedWorkers.Count > 0`:

- **ResourceGathering**: accumulates `productionProgress` at `GetProductionSpeed(productionRate)`; at ≥100 calls `CompleteResourceGathering()`, which applies the seasonal multiplier and the Gefjon's Blessing food bonus, adds the result via `ResourceManager.AddResource`, keeps overflow progress, and improves each worker's skill.
- **Crafting**: requires `craftingRecipe.CanCraft()` (enough input stock); if starved, sets `waitingForResources = true` and halts progress — otherwise accrues progress at `GetProductionSpeed(craftingRate)` and on completion consumes inputs and adds `outputAmount` of `outputResource`.

`GetProductionSpeed(baseRate)` sums `baseRate * worker.GetSkillMultiplier(assignedJobType)` across all assigned workers, then applies the Runestone "Tireless Workers" multiplier if active.

`GetSeasonalMultiplier()` delegates to `SeasonManager.GetProductionMultiplier(buildingType)` — only `Farm`, `FishermansHut`, and `LumberCamp` are seasonally gated; every other type is a flat 1.0 (see `docs/SeasonSystem.md`).

**Repair**: `SetNeedsRepair(bool)` swaps sprite/VFX. `CanRepair()` checks `ResourceManager` for enough of each `repairCosts[]` entry. `Repair()` spends the cost, clears `needsRepair`, and fires both `OnRepaired` (instance) and the static `OnAnyBuildingRepaired`.

**Events**: `OnRepaired` (instance), `static OnAnyBuildingRepaired(Building)`, `static OnAnyWorkerAssigned(Building)` — fired from `Repair()` and `AssignWorker()` respectively. There is **no** `OnAnyWorkerRemoved` counterpart; `RemoveWorker()` is a silent mirror operation.

### Placement, interaction, and construction

Real interaction-range detection lives in `WorldInteractionZone` (attached to the Jarl/player) — `BuildingInteractionZone.cs` is an obsolete 3-line shim kept only for migration. Flow: player walks into range → `WorldInteractionZone` detects the nearby `BuildingSelector` → Interact input → `BuildingSelector.SelectBuilding()` → deselects any other selected building, disables player input, retargets the camera, and opens the shared `BuildingInfoPanel`.

`BuildingSelector.IsInteractable`/`IsClickable` both require `building.isConstructed`.

**Construction is not yet a live system.** `constructionTime`, `constructionProgress`, and `isConstructed` are fully wired for save/load and gate interactivity, but nothing in gameplay code currently advances `constructionProgress` or flips a building to constructed — there is no placement/ghost-preview/build-timer script anywhere in `Assets/Scripts`. Buildings today exist pre-constructed in scene content. Treat the cost/time fields on `BuildingData` as scaffolding for a not-yet-implemented "place → pay → build over time" loop, not as an active mechanic.

### `BuildingInfoPanel` (player-facing UI)

Singleton, pauses the sim (`GameTickManager.PushUIPause()`) while open. Branches on `needsRepair`:
- **Damaged**: shows repair cost list and a repair button gated on `CanRepair()`; hides production/worker sections entirely.
- **Normal**: shows production progress (`GetProductionProgressPercent()`, estimated time via `GetEstimatedTimeToCompletion()`), worker count (`current/max`), and an assign-worker sub-panel populated from `SettlementManager.GetUnemployedVillagers()`.

Notable UI limitation: the crafting display only renders 2 consumed-resource slots (`consumeAmountText1/2`), even though `CraftingRecipe.inputResources` is an unbounded list.

---

## Worker Assignment

### Villager job data

`JobType` enum (`VillagerData.cs:149-169`): `None, Jarl, Steward, Farmer, Fisherman, Woodcutter, Miner, Smith, Carpenter, Weaver, Tanner, Warrior, Archer, Shipwright, Merchant, Healer, Shaman, Brewer`.

`VillagerSkills.GetSkillForJob(JobType)` maps most jobs to one raw stat (farming, fishing, mining, woodcutting, crafting, combat, sailing) — but `Jarl, Steward, Merchant, Healer, Shaman` have no mapping and fall through to a flat multiplier of `1f` with no `ImproveSkill` case, so working those jobs never raises any stat.

On `Villager`: `currentJob`, `assignedBuilding`, `skills`. `GetSkillMultiplier(JobType)` → `SettlementFormulas.GetSkillMultiplier(skill, morale)` — production speed is a function of both raw skill and current morale.

### Assignment flow (fully manual, player-driven)

1. Player opens a building's assign-worker panel → `BuildingInfoPanel.RefreshAvailableVillagers()` lists only mature, jobless villagers from `SettlementManager.GetUnemployedVillagers()`.
2. Clicking a villager row → `BuildingInfoPanel.AssignVillager()` → `Building.AssignWorker(villager)`.
3. `Building.AssignWorker`: gated by `CanAssignWorker()` (`!needsRepair && assignedWorkers.Count < maxWorkers` — **does not check `isConstructed`**), adds to `assignedWorkers`, calls `villager.AssignJob(jobType, building)`, fires `OnAnyWorkerAssigned`.
4. `Villager.AssignJob`: rejects if not `LifeStage.Mature`; otherwise sets `currentJob`/`assignedBuilding` and calls `SetWorkLocation(building.transform)` on every `VillagerAIBase` component, which immediately force-transitions the AI to `VillagerMoveToWorkState` regardless of its current state.

There is no auto-assignment or "best worker for job" logic anywhere — every assignment is one villager, one click.

### Unassignment, death, and destruction

- **Manual unassign**: `Building.RemoveWorker()` removes from the list and calls `villager.UnassignJob()` (clears `currentJob`/`assignedBuilding`, clears work location). No event fires (asymmetric with assignment).
- **Villager death**: `Villager.Die()` explicitly calls `assignedBuilding.RemoveWorker(this)` before unregistering — the slot is freed correctly.
- **Building destruction**: `Building.OnDestroy()` only unregisters from `SettlementManager` — it does **not** free `assignedWorkers`. Villagers assigned to a destroyed building keep a dangling `assignedBuilding`/stale work-location transform and are never returned to the unemployed pool. This is a real gap worth fixing before building destruction/demolition ships as a feature.

### AI state machine for working villagers

```
VillagerIdleState → (assignedBuilding != null) → VillagerMoveToWorkState → (movement complete) → VillagerWorkState
```

`VillagerWorkState` is cosmetic only — it does not perform or trigger production itself; it just re-randomizes a wander-near-work-point movement. Actual production math runs entirely inside `Building.UpdateProduction`, decoupled from the AI state machine and driven directly off `assignedWorkers` regardless of what the villager's AI state happens to be doing at that instant.

Skill improvement currently happens twice, through two unrelated code paths — once in `Building.CompleteResourceGathering`/`CompleteCrafting` on production completion, and again in `Villager.Work()` on each life-tick — worth flagging as a likely unintended double skill-gain rather than an intentional double-rate design.

### Capacity

Per-building cap is `BuildingData.maxWorkers`, enforced by `Building.CanAssignWorker()` and re-checked at the top of `AssignWorker()` (guards against stale-UI double assignment). No global population-vs-jobs balancing exists beyond this per-building count.

---

## Resource Economy

### `ResourceType` enum

`Assets/Scripts/BuildingData.cs:112-130`: `None, Wheat, Fish, Wood, Stone, Iron, Honey, Weapons, Tools, Armor, Shields, Sails, Leather, Mead, Planks, Gold`.

- `Wood`/`Stone`/`Iron` — construction costs (checked via `ResourceManager.HasEnoughResources`, hard-coded to only these three types).
- `Fish` — the only food resource actually consumed by the population; `Wheat` is produced (Farm) but currently has **no consumption sink** anywhere in the reviewed code — it can only accumulate. Worth confirming with design whether Wheat is meant to feed a Mead crafting recipe that hasn't been wired up yet.
- `Honey`, `Gold` — spent by `HealerHut` for wound-healing (3 Honey + 5 Gold/wound).
- `Weapons/Tools/Armor/Shields/Sails/Leather/Mead/Planks` — crafted outputs from Blacksmith/Carpenter/Weaver/Tannery/Shipyard-type buildings.
- `Gold` is the only currency-like resource; `TradingPost` exists as an enum value but has no distinct implementation beyond the generic `Building` class — there is no marketplace/exchange system.

### `ResourceManager` (the ledger)

Single `Dictionary<ResourceType, float>`, initialized to 0 for every enum value. `AddResource` adds unconditionally (no cap); `SpendResource` only subtracts and succeeds if the balance covers the cost. **Resources are unbounded floats — there is no storage cap, warehouse, or overflow mechanic anywhere in the codebase.**

`static event Action<ResourceType, float> OnResourceAdded` fires only on add, never on spend, and is never cleared across scene loads — a latent ghost-subscriber risk for any consumer that doesn't carefully pair subscribe/unsubscribe (one current subscriber, `RewardNotificationManager`, does this correctly). The main resource HUD (`ResourceDisplayUI`) does **not** use this event at all — it polls `GetResource()` on a 10 Hz timer instead, so the event and the UI are two disconnected notification paths.

### Production paths

1. **Building production** (the primary path) — `Building.UpdateProduction` → `ResourceManager.AddResource`, as described in the Buildings section above. Speed factors in worker skill + morale (`SettlementFormulas.GetSkillMultiplier`) and the seasonal multiplier for Farm/FishermansHut/LumberCamp.
2. **Manual harvesting** — `HarvestableResource`/`TreeResource` (`Assets/Scripts/Interactive Elements/`), entirely separate from buildings. Yield comes from a villager striking the object with a weapon (`TakeDamage`), scaled by tool-match multiplier and `GetSkillMultiplier`, and calls `ResourceManager.AddResource` directly — bypassing `Building` entirely. `TreeResource` additionally grants a flat bonus (`bonusWoodOnFall`) on top of the final chop yield, an asymmetry unique to trees among harvestables.

### Consumption

- **Firewood**: `SettlementManager.ConsumeFirewood()` runs once per day (unconditionally, in both summer and winter — the 0.5× summer multiplier is live, not dead code). Cost = `SettlementFormulas.GetWoodCost(population, season, coldDayType, stormMultiplier, runestoneMultiplier)`. Shortfall spends whatever wood exists and applies cold morale/health penalties to a formula-determined subset of villagers; sufficiency applies a warmth morale bonus to everyone.
- **Food**: `SettlementManager.HandleMealTime()` computes total Fish needed (`SettlementFormulas.GetTotalFishNeeded`, adjusted by the Rationing runestone). If short, hunger is distributed either **Prioritized** (some villagers fully fed, the rest take true damage + morale loss) or **Shared** (everyone takes proportional damage) depending on `HungerDistributionMode`. `OnFoodConsumed` fires with a fully-fed bool.
- Population **decay** is purely the starvation/cold damage → death pipeline above; there is no direct "resource deficit shrinks population" rule beyond that. Population **growth** comes from villager reproduction, not gated by resource surplus in the reviewed code.

### `SettlementFormulas.cs` — shared math layer

Static, side-effect-free, explicitly designed so live gameplay and `SettlementSimulator` (the raid-away offline simulation) can't diverge. Holds skill multiplier, production-adjustment, wood-cost, cold-effect, and hunger/food-heal formulas. Every live call site (`Building`, `SettlementManager`, `Villager`) and every simulator call site route through the same functions — confirmed by direct comparison during this audit.

---

## Known Gaps / Design Notes

These are descriptive flags for design follow-up, not bugs in the sense of "broken today" — most are scaffolding for features not yet wired up:

1. **No live construction loop** — `BuildingData.constructionTime`/`Building.constructionProgress`/`isConstructed` exist but nothing advances them at runtime; buildings are placed pre-constructed.
2. **Dangling worker refs on building destruction** — `Building.OnDestroy()` doesn't free `assignedWorkers`; affected villagers keep a stale job/work-location.
3. **Double skill-gain** — both `Building` production completion and `Villager.Work()`'s life-tick improve the same skill independently.
4. **Asymmetric worker events** — `OnAnyWorkerAssigned` exists; `OnAnyWorkerRemoved` doesn't.
5. **No resource storage cap** — all resources are unbounded floats; no warehouse/overflow mechanic.
6. **`Wheat` has no consumption sink** — produced by Farm, boosted by Gefjon's Blessing, but nothing spends it.
7. **`ResourceManager.OnResourceAdded`** is a static event with no scene-teardown and isn't even used by the main resource HUD, which polls instead.
8. **`TradingPost`** is a defined `BuildingType` with no distinct implementation — no marketplace/exchange system exists yet despite `Gold` being present as a currency resource.
9. **Unmapped `JobType`s** (`Jarl, Steward, Merchant, Healer, Shaman`) have no skill stat and never improve from work.
