# Villagers / Vikings

**Core files:** `Assets/Scripts/Villager/Villager.cs`, `Assets/Scripts/Villager/VillagerData.cs`, `Assets/Scripts/Villager/VillagerAIBase.cs`, `Assets/Scripts/Managers/WoundManager.cs`, `Assets/Scripts/Managers/DeathTypeBuff.cs`, `Assets/Scripts/Skills/SkillTreeManager.cs`, `Assets/Scripts/Managers/RunestoneManager.cs`, `Assets/Scripts/Managers/JarlManager.cs`

## Overview

A villager carries three largely independent kinds of state: **life stats** (life stage, health, morale, wounds — this doc's first section), **skill growth** (a per-villager raw-stat creep, entirely separate from the Jarl-scoped meta skill tree and from Runestones — three unrelated progression tracks that are easy to conflate), and **lineage** (two-parent reproduction with real parent tracking, feeding directly into Jarl succession eligibility). See `docs/VillageManagement.md` for how a villager's job/building assignment works.

---

## Life Stages & Stats

### `LifeStage`

`enum LifeStage { Young, Mature, Dead }` (`Villager.cs:907-912`) — only three stages; there is no separate "Elder" stage. Transitions are driven by `age` (years), which increments on a timer (`ageingInterval`/`ageingAmount` from `SettlementManager`):

| Stage | Condition | Capabilities |
|---|---|---|
| `Young` | `age < 16` | Cannot be assigned a job (`AssignJob` rejects), doesn't work/reproduce, AI won't initiate combat (`VillagerAIBase.ShouldAbortSearchTick`) — but can still be attacked/fight back defensively (every villager is a combatant, there's no separate non-combat role). |
| `Mature` | `16 <= age < lifeExpectancy` | Full capability: jobs, work/skill-gain, reproduction, combat initiation. |
| `Dead` | `age >= lifeExpectancy` | Terminal. |

`lifeExpectancy` defaults to 60, but children roll `Random.Range(50, 70)` individually. Death fires both from `Age()` directly on threshold and again from the `Dead` branch of `OnLifeStageChanged` — redundant but harmless (`Die()` is idempotent).

### Raw stats

`VillagerSkills` (`VillagerData.cs:4-140`): `intelligence`, `learningRate`, `farming`, `fishing`, `mining`, `woodcutting`, `crafting`, `combat`, `sailing` — all floats, default 1, randomized `1–4` at spawn (`Randomize`), no upper cap (see Skill Progression below).

`CombatStats`: `strength = 5`, `defense = 5`.

On `Villager`: `morale`/`maxMorale = 100/100` (clamped to range), `age`, `lifeExpectancy`, `healthRegenFromFood = 1`, `moraleRegenFromFood = 5`, `activeWounds` (max 3). Health comes from the base `TargetHealth` class: `maxHealth = 100`, no explicit floor clamp on `currentHealth` (can go briefly negative before `Die()` fires — a UI reading raw health could flash a negative fill for one frame).

### Health & morale — who touches them

Damage flows through `TargetHealth.TakeDamage`, overridden by `Villager.CalculateFinalDamage`, which layers in `combatStats.defense`, Jarl skill-tree defense bonuses, Runestone/DeathTypeBuff defense%, and wound defense penalties before the hit lands. Cold (`SettlementManager.ApplyColdEffects`) and starvation (`Villager.HandleHunger`/`HandleSharedHunger`) both deal **true damage** — they bypass defense entirely.

Morale changes from: hunger (`SettlementFormulas` hunger-effect formulas, roughly −10/tick when short), food (+`moraleRegenFromFood`), cold/warmth (±`coldMoralePenalty`/`warmthMoraleBonus`), and Jarl death/succession (−20 to everyone on death, +10 on a new Jarl taking over). There is **no passive morale decay** outside these triggers.

Morale isn't cosmetic — `SettlementFormulas.GetSkillMultiplier` = `(1 + skill*0.5) * (morale/100)`, so a demoralized settlement produces less regardless of skill level.

### Wounds (`WoundManager`)

Wounds only roll on **weapon damage**, never on true damage (cold/hunger) — `Villager.OnSignificantHPDamage` fires from `TargetHealth.TakeDamage` only when a weapon dealt the hit. Below 30% of max HP in one hit, no roll happens; above that, chance scales in tiers (15%/30%/50%/80% at 30/40/50/70%-of-maxHP damage thresholds).

A wound is a **semi-permanent debuff**, not HP loss — one of 6 `WoundType`s (`Lame` −8% move speed, `OneEye` −5% maxHP/−5% attack, `BattleScarred` −5% maxHP/−5% defense, `BrokenRibs` −8% attack, `TornShoulder` −5% attack speed/−5% attack, `IronWill` −5% maxHP), stacking additively across up to 3 active wounds. **A 4th wound kills the villager outright**, regardless of current HP. Wounds only clear via `HealerHut` (3 Honey + 5 Gold, requires an assigned healer worker) — there is no passive healing.

### `DeathTypeBuff` — settlement-wide buff on succession

A single-slot, 10-real-minute buff granted **only when a Jarl dies and a new one is chosen** (not on ordinary villager deaths), keyed by the dead Jarl's cause of death:

- **Combat death** → *Valkyrie's Favour*: +5% warrior damage, +5% warrior defense, −10% Jarl ability cooldowns.
- **Any other cause** (old age / starvation / cold) → *Gefjon's Blessing*: +10% Fish/Wheat production, +10% birth rate (reduces reproduction cooldown), +15% food-heal speed.

Only one buff is active at a time; a new Jarl death overwrites it. Effects are read piecemeal wherever relevant (combat damage calc, `TryReproduce`, food-heal, building production) rather than applied centrally.

### Identity

Names: `VillagerNameGenerator.GenerateNorseName(gender)` picks randomly from small fixed pools (~10-14 names per gender per slot) with no uniqueness check — duplicate names are possible. Children get an independently-rolled name, **not** inherited from parents, and there is no surname/clan-name system anywhere in the codebase. Appearance: a `spriteVariant` is assigned randomly at spawn, or for children, 80% chance to copy one parent's variant / 20% chance of a fresh random mutation.

---

## Skill / XP Progression

Three separate systems share the word "skill" but don't share state — worth being explicit about which one any given mechanic touches:

| System | Scope | Currency | Persists across succession? |
|---|---|---|---|
| **Raw villager stats** (`VillagerSkills`) | Per-villager | None — grows automatically from work | Yes (it's just villager data) |
| **`SkillTreeManager`** | Jarl-only meta unlock tree | Global XP pool | Unlocked nodes persist; XP pool resets to 0 |
| **`RunestoneManager`** | Settlement-wide passive bonuses | None — awarded on Jarl death, not earned | Active runestones persist (max 3) |

### Raw villager stat growth

`ImproveSkill(job)` (`VillagerData.cs:36-55`): `amount = 0.05 * max(0.1, learningRate * intelligence/10)`, added directly to the stat matching the job. **No cap** — stats grow unbounded. Jobs `Jarl, Steward, Merchant, Healer, Shaman` have no mapping in `GetSkillForJob`/`ImproveSkill`, so working those "jobs" gains nothing.

This is called from **at least five independent sites**: `Building.CompleteResourceGathering`, `Building.CompleteCrafting`, `Villager.Work()` (per life-tick), `HarvestableResource` (on manual harvest hit), and `CharacterBase`/`EquipableItem` (combat swings — doubled further while on a raid or parrying). This compounds well beyond the previously-known "double-counting" between building completion and the life-tick — worth a design pass if growth rate needs to be predictable/tunable.

### `SkillTreeManager` — Jarl meta progression

XP is a single global int, fed from three sources: combat kills (`Enemy.xpReward`, default 25), resource harvesting (`HarvestableResource.xpPerHarvest`, default 5), and mission rewards. Nodes (`SkillDefinitionSO` assets) are tiered 1-4 across three categories (Combat, Passive, ResourceGathering), each with an `xpCost` and prerequisites; unlocking spends from the shared pool.

**Important limitation**: only Combat and Passive effect types are actually read anywhere in gameplay, and only for the character currently flagged `isJarl` — every read site (`CharacterBase`, `Villager`) gates on `isJarl == true` first. The `GetEffectForJob`/`GetEffectForResource` API that would let `ResourceGathering`-category nodes (and `MoraleDecayReduction`) affect ordinary villagers is **never called anywhere** — those nodes are unlockable, cost real XP, and display tooltip text, but have zero mechanical effect today. Worth flagging before a player spends XP expecting a settlement-wide gathering boost.

XP resets to 0 on Jarl succession (`ResetXP`, fired from `JarlManager.OnJarlDied`), but already-unlocked nodes are **not** reset — only the spendable pool. `SkillTreeUI` shows XP as a flat number with no bar/level concept. Persisted via `ISaveable` (`currentXP`, `unlockedSkillIds`); loaded before villager state so `ApplySkillBonuses()` sees correct values on load.

### `RunestoneManager` — succession-reward bonuses

Fully independent of XP. On Jarl death, offers 3 `RunestoneType` picks (one weighted toward the dead Jarl's dominant skill category, two random); player selects, up to 3 active at once. These are the source of the flat multipliers referenced elsewhere in the codebase: *Tireless Workers* (production speed), *Winter's Friend* (wood consumption), *Rationing* (fish consumption), *Fertile Lands* (+20% birth rate), *Education* (+2 to two random skills on newborns). Persisted as an int array of active types.

---

## Reproduction & Inheritance

Reproduction is **two-parent with real parent tracking** — not an abstract "settlement produces N babies per season" model.

### Mechanics

Each `Mature` villager accumulates a `reproductionTimer`; past `SettlementManager.reproductionInterval` it calls `TryReproduce()`. Effective cooldown = `SettlementManager.reproductionCooldown` (default 5), divided by the Fertile Lands runestone multiplier (1.2×) and further divided if Gefjon's Blessing is active. A partner is either the villager's existing `partner` field or found via a scan for any other `Mature`, opposite-gender villager whose own cooldown has also cleared. **No morale/health/housing-capacity gate exists** — only life-stage and cooldown timing matter. Only the female of a pair triggers `CreateChild` (avoids duplicate births); the child spawns at age 0 via `VillagerSpawner`.

### Inheritance

- **Skills**: mean of both parents per stat, ±10% independent random variation, floored at 0.5. `learningRate` itself is not inherited.
- **Combat stats** (`strength`/`defense`): plain parent average, no randomization.
- **Skill gain rate**: parent average ± up to 0.1, floored at 0.5.
- **Appearance**: 80% chance to copy one parent's sprite variant, 20% chance of a fresh mutation.
- **Name**: not inherited — independently rolled.
- **Education runestone**, if active, adds +2 to two random stats on top of inheritance.
- **Jarl lineage flag**: if either parent `isJarl`, the child becomes `isOfJarlLineage = true` at `generationsFromJarl = 1`; otherwise it inherits the flag from a lineage-flagged parent at `generationsFromJarl = min(parents) + 1`. This is what succession candidate ranking reads (below).

---

## Jarl Succession

On Jarl death (`OnJarlDied`), `JarlManager.SuccessionProcess()` waits a short delay, then gathers candidates: alive, not the dead Jarl, and `Mature` (children are excluded unless `allowChildrenInSuccession` is explicitly enabled — default off). Candidates are ranked first by lineage-derived priority tier (Direct Child > Sibling > Grandchild > Distant Relative > Unrelated High-Skill), then by a score (`combat*10 + age-curve bonus (peaks at 25-40) + 50 if of Jarl lineage`), capped to the top 5 shown to the player.

**Selection is player-driven**, not random: the UI presents ranked candidates and the player picks. If nobody responds within 120 real-time seconds, it auto-picks the top candidate. Note candidate eligibility doesn't separately check physical presence — a villager away on a raid is technically eligible.

Choosing an heir (`SelectHeir`) transfers Jarl control/camera/AI components, applies the `DeathTypeBuff` for the death cause, then **chains into a mandatory Runestone pick** for the deceased Jarl's memorial — the succession UI stays open through both steps until `RunestoneManager.OnSelectionComplete` fires and `JarlManager` re-applies skill bonuses to everyone and closes out with `OnSuccessionEnded`.

**No eligible heirs**: falls back to the highest-scored `Mature` villager regardless of lineage; if only `Young` villagers remain, logs a `// TODO: Implement regent system` note and ends succession with no Jarl assigned; if no villagers remain at all, triggers game over.

**Persistence**: `JarlManager` saves only `currentJarlId`, re-resolving on load. Lineage data lives per-villager in the save (`parent1Id`, `parent2Id`, `partnerId`, `childrenCount`, `timeSinceLastChild`, `isJarl`, `isOfJarlLineage`, `generationsFromJarl`), resolved back into references post-load.

A previously-tracked bug (`RunestoneManager.SelectRunestone` silently stalling at capacity) is partially resolved: it now logs a clear error instead of failing silently, and the normal UI flow (`RunestoneUI`) checks capacity first and routes to a replacement flow — but if anything ever calls `SelectRunestone` directly while at capacity outside that UI path, `JarlManager` would still wait indefinitely with no timeout on that second wait (unlike the 120s heir-selection timeout). Latent risk, not currently reachable through normal play.

---

## Known Gaps / Design Notes

1. **Raw skill growth has no cap and fires from 5+ independent call sites** (building completion ×2, per-tick work, manual harvesting, combat swings) — growth rate is effectively unpredictable/untunable from a single formula.
2. **`ResourceGathering`/`MoraleDecayReduction` skill-tree nodes are dead weight** — unlockable, cost XP, but no code path ever applies their effect.
3. **Skill-tree Combat/Passive bonuses only ever apply to the Jarl**, never to other villagers — easy to assume settlement-wide, isn't.
4. **`OnSkillsReset` event has zero subscribers** — dead code.
5. **No morale/health/housing gate on reproduction** — pairing is purely life-stage + cooldown timing.
6. **No surname/clan-name system** — children get a fully independent random name, so lineage isn't visible by name.
7. **Succession candidate eligibility doesn't check physical presence** — a villager on a raid is technically selectable.
8. **`RunestoneManager` capacity-stall risk** if `SelectRunestone` is ever called outside the guarded `RunestoneUI` flow (see above).
9. **Jobs `Jarl, Steward, Merchant, Healer, Shaman` have no skill mapping** — working them never improves any stat or benefits from a skill multiplier beyond a flat 1.0.
