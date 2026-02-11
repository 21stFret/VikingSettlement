# Viking Settlement - Implementation Status Report
**Date:** January 29, 2026
**Status:** Code Complete - Needs Unity Setup & Testing

---

## Summary

Implemented a complete Jarl succession system where:
- One villager is THE Jarl (player-controlled)
- When the Jarl dies, an heir selection UI appears
- Heirs are prioritized by bloodline (children > siblings > grandchildren > distant relatives > unrelated)
- Player selects new Jarl, control transfers automatically
- Settlement morale affected by Jarl death (-20) and new Jarl (+10)

---

## Files Created

| File | Location | Purpose |
|------|----------|---------|
| `JarlManager.cs` | `Assets/Scripts/Managers/` | Central singleton - manages Jarl state, succession logic, heir ranking |
| `SuccessionCandidate.cs` | `Assets/Scripts/Villager/` | Data class for heir candidates with priority enum and scoring |
| `SuccessionUI.cs` | `Assets/Scripts/` | UI panel for death notification and heir selection (includes OnGUI fallback) |
| `SuccessionCandidateItem.cs` | `Assets/Scripts/` | Individual candidate row component for Canvas UI |

## Files Modified

| File | Changes |
|------|---------|
| `Villager.cs` | Added `isJarl`, `isOfJarlLineage`, `generationsFromJarl` fields; Modified `Die()` to trigger succession; Added lineage tracking in `CreateChild()`; Added `GetChildren()`, `GetSiblings()`, `GetTotalSkillScore()` |
| `PlayerController.cs` | Converted to target-based control with `SetControlTarget(Villager)` and `GetControlTarget()` |
| `SettlementManager.cs` | Added `HandleJarlDeath()` and `HandleJarlChanged()` event handlers for morale effects |
| `VillagerAI.cs` | No changes needed - already had `SetAIEnabled()` at line 433 |

---

## Architecture Decisions Made

1. **Jarl as State, not Subclass** - Added `isJarl` flag to Villager rather than creating a Jarl subclass. Works better with existing inheritance/reproduction system.

2. **Target-Based PlayerController** - PlayerController now references a `controlTarget` Villager rather than being attached to a specific GameObject. Enables clean control transfer during succession.

3. **Event-Driven UI** - JarlManager fires events (`OnJarlDied`, `OnSuccessionStarted`, `OnJarlChanged`, `OnSuccessionEnded`) that UI and SettlementManager subscribe to.

---

## Unity Setup Required

### 1. Create JarlManager GameObject
```
1. Create empty GameObject named "JarlManager"
2. Add JarlManager component
3. Optionally assign PlayerController reference (auto-finds if not set)
```

### 2. Create SuccessionUI GameObject
```
1. Create empty GameObject named "SuccessionUI"
2. Add SuccessionUI component
3. For quick testing: Leave all fields empty - OnGUI fallback will render
4. For proper UI: Create Canvas with panel, wire up references
```

### 3. Mark Initial Jarl
```
1. Select your starting villager in the scene
2. In Inspector, check "Is Jarl" = true
3. JarlManager will auto-detect on Start() OR assign directly to JarlManager's "Current Jarl" field
```

### 4. Connect PlayerController
```
1. On PlayerController, assign "Control Target" to your initial Jarl villager
2. OR leave empty - will use fallback GetComponent<CharacterController>()
```

---

## Key Code Locations

### JarlManager.cs
- `SetJarl(Villager, bool isInitial)` - Line ~95 - Sets a villager as Jarl
- `OnCurrentJarlDied()` - Line ~138 - Called when Jarl dies, starts succession
- `GetSuccessionCandidates()` - Line ~175 - Calculates and ranks heirs
- `SelectHeir(Villager)` - Line ~243 - Finalizes heir selection

### Villager.cs
- Jarl fields - Line ~32-34
- `Die()` succession trigger - Line ~420-430
- Lineage tracking in `CreateChild()` - Line ~319-330
- Helper methods - Line ~525-575 (`GetChildren`, `GetSiblings`, `GetTotalSkillScore`)

### PlayerController.cs
- `SetControlTarget(Villager)` - Line ~120-160 - Transfers control to new villager

### SettlementManager.cs
- Jarl event subscriptions - Line ~63-67
- `HandleJarlDeath()` - Line ~85 - Applies -20 morale
- `HandleJarlChanged()` - Line ~100 - Applies +10 morale

---

## Succession Priority Order

```
1. DirectChild (0)     - Children of the dead Jarl
2. Sibling (1)         - Share a parent with the Jarl
3. Grandchild (2)      - Children of Jarl's children
4. DistantRelative (3) - In Jarl lineage but not direct
5. UnrelatedHighSkill (4) - Fallback to highest-skill villager
```

### Scoring Formula
```
Score = (Combat × 10) + AgeBonus + LineageBonus

AgeBonus:
- Under 18: 0
- 18-25: Ramps up to 20
- 25-40: 20 (peak)
- 40-60: Declines from 20
- 60+: 10

LineageBonus: +50 if isOfJarlLineage
```

---

## Testing Checklist

- [ ] JarlManager and SuccessionUI GameObjects created in scene
- [ ] Initial Jarl marked with `isJarl = true`
- [ ] PlayerController's `controlTarget` assigned
- [ ] Test: Kill Jarl via combat → Succession UI appears
- [ ] Test: Kill Jarl via old age → Succession UI appears
- [ ] Test: Select heir → Control transfers, camera follows
- [ ] Test: Verify morale drops on death, rises on new Jarl
- [ ] Test: Have Jarl reproduce → Children marked `isOfJarlLineage`
- [ ] Test: Kill Jarl with children → Children ranked first
- [ ] Test: No relatives → Highest-skill villager selected

---

## Known Limitations / Future Work

1. **No Visual Jarl Indicator** - Plan mentioned crown/aura but not implemented yet
2. **No Regent System** - If only children exist, no adult regent appointed
3. **No Game Over Screen** - `TriggerGameOver()` just logs an error
4. **Canvas UI Not Wired** - Using OnGUI fallback; need to create proper Canvas prefabs
5. ~~**No Save/Load** - Jarl state not persisted~~ ✅ DONE (Jan 29)

---

## Quick Resume Commands

```csharp
// Manually set a Jarl
JarlManager.Instance.SetJarl(someVillager);

// Get current Jarl
Villager jarl = JarlManager.Instance.CurrentJarl;

// Check if in succession
bool selecting = JarlManager.Instance.IsInSuccession;

// Transfer player control manually
playerController.SetControlTarget(someVillager);
```

---

## Recent Updates (January 25, 2026)

### Raid System - Villager Persistence Fix

**Problem:** Villagers staying behind at the settlement were destroyed when loading the raid scene, corrupting save data.

**Solution:** All villagers now use `DontDestroyOnLoad` during raids:
- Raid party: Preserved and visible in raid scene
- Home villagers: Preserved but hidden (`SetActive(false)`) until return

**Files Modified:**

| File | Changes |
|------|---------|
| `RaidManager.cs` | Added `homeVillagers` list; Added `raidSurvivorsForReintegration` HashSet; `StartRaid()` now preserves and hides home villagers; `ReturnToSettlement()` reactivates them; Added `WasRaidSurvivor()` and `ClearReintegrationTracking()` helpers |
| `VillagerSpawner.cs` | `ReintegrateSurvivors()` now distinguishes raid survivors (repositioned) from home villagers (keep position); Clears tracking after reintegration |

**Flow:**
1. `StartRaid()`: Raid party gets `DontDestroyOnLoad` + visible; Home villagers get `DontDestroyOnLoad` + hidden
2. During raid: Both groups persist in DontDestroyOnLoad scene
3. `EndRaid()`: Home villagers reactivated, scene loads
4. `ReintegrateSurvivors()`: Moves everyone back to settlement scene

---

### Damage System Refactor

**Problem:** `TakeDamage` logic was scattered across multiple files with inconsistent signatures. Defense/shield logic was duplicated. The `trueDamage` parameter was ignored.

**Solution:** Consolidated into clean inheritance pattern in `TargetHealth`:

**Files Modified:**

| File | Changes |
|------|---------|
| `TargetHealth.cs` | Single `TakeDamage(float, EquipableItem, bool trueDamage)` entry point; Added virtual `CalculateFinalDamage()` for defense; Added virtual `OnDamageTaken()` for visual feedback; `trueDamage` now properly bypasses reductions |
| `Villager.cs` | Removed `TakeDamage` override; Added `CalculateFinalDamage()` (applies `combatStats.defense` + shield); Added `OnDamageTaken()` (UI, blood, flash) |
| `Enemy.cs` | Removed `TakeDamage` override; Added `CalculateFinalDamage()` (applies shield); Added `OnDamageTaken()` (blood, UI, flash) |
| `SeasonManager.cs` | Cold damage now uses `trueDamage` flag (bypasses armor) |
| `RaidManager.cs` | Settlement event damage now uses `trueDamage` flag |
| `HarvestableResource.cs` | Fixed health calculation bug (`currentHealth -= yield` instead of `= maxHealth - yield`) |

**New Damage Flow:**
```
target.TakeDamage(damage, weapon, trueDamage)
    → Check isDead, invincibility
    → If !trueDamage: damage = CalculateFinalDamage(damage, weapon)  // Subclass override
    → currentHealth -= damage
    → OnDamageTaken(damage, weapon)  // Subclass override for visuals
    → Check death
```

---

## Related Plan File
`C:\Users\Asus TUF Gaming PC\.claude\plans\stateful-foraging-star.md`

---

## Recent Updates (January 29, 2026)

### ScriptableObject Refactor - Dialogue & Mission System

**Problem:** Dialogue and Mission data was embedded in scene objects, making it hard to reuse and modify.

**Solution:** Refactored to ScriptableObject-based assets with runtime progress wrappers.

**New Files:**

| File | Location | Purpose |
|------|----------|---------|
| `DialogueSO.cs` | `Assets/Scripts/Dialogue/` | ScriptableObject for dialogue assets with `dialogueId`, `lines`, `offersQuest` |
| `MissionDefinitionSO.cs` | `Assets/Scripts/Mission/` | ScriptableObject for mission templates with objectives, rewards, dialogue refs |
| `ActiveMission.cs` | `Assets/Scripts/Mission/` | Runtime wrapper pairing SO definition with live `objectiveProgress[]` |

**Modified Files:**

| File | Changes |
|------|---------|
| `DialogueData.cs` | Removed `Dialogue` class, kept `DialogueLine` |
| `MissionData.cs` | Removed `MissionDefinition`/`MissionObjective`, added `MissionObjectiveTemplate` |
| `DialogueManager.cs` | Changed `Dialogue` → `DialogueSO` |
| `MissionManager.cs` | Changed `List<MissionDefinition>` → `List<ActiveMission>`, events use `ActiveMission` |
| `QuestGiver.cs` | Changed `MissionDefinition[]` → `MissionDefinitionSO[]`, added `questGiverId` |
| `MissionTrackerUI.cs` | Updated to read from `ActiveMission` |

**Create Assets:**
```
Assets > Create > Viking Settlement > Dialogue
Assets > Create > Viking Settlement > Mission
```

---

### Save System Implementation

**Problem:** No persistence - game state lost on exit.

**Solution:** JSON-based save system with auto-save and manual slots.

**New Files:**

| File | Location | Purpose |
|------|----------|---------|
| `SaveData.cs` | `Assets/Scripts/Save/` | All serializable save structs + `ISaveable` interface |
| `SaveManager.cs` | `Assets/Scripts/Save/` | Singleton with auto-save, `SaveGame()`, `LoadGame()`, slot management |

**Modified Files (added ISaveable):**

| File | Save Data |
|------|-----------|
| `Villager.cs` | Added `uniqueId` (GUID), all stats/skills/references saved |
| `Building.cs` | Added `uniqueId` (GUID), construction/production state saved |
| `ResourceManager.cs` | All resource amounts |
| `DayNightManager.cs` | `currentTimeOfDay`, `currentDay` |
| `SeasonManager.cs` | `currentSeason`, `daysUntilSeasonChange`, `currentSolarYear` |
| `SettlementManager.cs` | All villagers (with ID-based cross-refs), buildings, stats |
| `MissionManager.cs` | Active missions (by SO missionId + progress), completed IDs |
| `JarlManager.cs` | Current Jarl by `uniqueId` |

**Save Location:** `Application.persistentDataPath/saves/`

**Slots:**
- `autosave` - Auto-saves every 5 minutes (configurable)
- `slot1`, `slot2`, `slot3` - Manual save slots

**Usage:**
```csharp
SaveManager.Instance.SaveGame("slot1");
SaveManager.Instance.LoadGame("slot1");
SaveManager.Instance.HasSave("slot1");
SaveManager.Instance.DeleteSave("slot1");
SaveSlotInfo[] slots = SaveManager.Instance.GetAllSaveSlots();
```

---

### Held Attack Input

**Problem:** Attack only worked on single click, not when holding the button.

**Solution:** Track held state and continuously attack at weapon's attack speed.

**Modified Files:**

| File | Changes |
|------|---------|
| `EquipableItem.cs` | Added `attackSpeed` field (default 0.5s) |
| `CharacterController.cs` | Added `GetAttackDelay()` (uses weapon speed if equipped), `CanAttack()` public check |
| `PlayerController.cs` | Added `isAttackHeld` tracking, continuous attack in `Update()` while held |

**Weapon Attack Speed:**
- Each weapon can define its own `attackSpeed` (lower = faster)
- Falls back to `CharacterController.attackDelay` if no weapon equipped

---

### Skill Tree System

**Concept:** Legacy skill tree where unlocked skills persist through succession but XP resets with each new Jarl.

**New Files:**

| File | Location | Purpose |
|------|----------|---------|
| `SkillDefinitionSO.cs` | `Assets/Scripts/Skills/` | ScriptableObject defining a skill with effects, cost, prerequisites |
| `SkillTreeManager.cs` | `Assets/Scripts/Skills/` | Singleton managing unlocked skills and XP |
| `SkillTreeUI.cs` | `Assets/Scripts/Skills/` | Main skill tree panel UI (toggle with K key) |
| `SkillNodeUI.cs` | `Assets/Scripts/Skills/` | Individual skill node component |

**Modified Files:**

| File | Changes |
|------|---------|
| `SaveData.cs` | Added `SkillTreeSaveData` struct |
| `SaveManager.cs` | Added SkillTreeManager to save/load |
| `CharacterController.cs` | Added skill bonuses for attack speed, damage, crit, life steal, move speed |
| `Villager.cs` | Added skill bonuses for defense, max health; `OnJarlStatusChanged()` for event subscription |
| `JarlManager.cs` | Calls `OnJarlStatusChanged()` when Jarl changes |
| `TargetHealth.cs` | Added `Heal()` method for life steal |
| `Enemy.cs` | Added `xpReward` field, grants XP on death |
| `MissionManager.cs` | Experience rewards now go through SkillTreeManager |

**Skill Types:**
- `Combat` - Damage, attack speed, crit, life steal, defense
- `Passive` - Move speed, XP gain, morale decay reduction
- `ResourceGathering` - Gathering speed, yield bonuses

**Effect Types:**
```
DamagePercent, AttackSpeedPercent, DefenseFlat, DefensePercent,
MaxHealthFlat, MaxHealthPercent, LifeSteal, CriticalChance,
MoveSpeedPercent, XPGainPercent, MoraleDecayReduction,
GatheringSpeedPercent, GatheringYieldPercent, ResourceCapacityPercent
```

**XP Sources:**
- Killing enemies (`xpReward` field on Enemy, default 25)
- Completing missions (Experience reward type)

**Persistence:**
- Unlocked skills: Saved and persist through succession
- XP: Resets when Jarl changes (new Jarl starts at 0 XP)

**Create Skills:**
```
Assets > Create > Viking Settlement > Skill
```

**Unity Setup:**
1. Create SkillTreeManager GameObject with SkillTreeManager component
2. Assign all SkillDefinitionSO assets to the `allSkills` array
3. Create SkillTreeUI with Canvas panel (or use OnGUI fallback)
4. Set `treePosition` on each skill for layout
5. Press K to open skill tree

---

## Recent Updates (January 30, 2026)

### Save System Fixes

**Problem:** Save/load was partially working but had several issues:
- Workers weren't being restored to buildings on load
- Seasonal effects weren't applied on second load
- Time speed buttons stopped working after loading a save

**Solutions:**

#### 1. Worker Assignment Restoration
Buildings are matched by `BuildingData.name` (SO name) instead of grid position or GUID.

**Files Modified:**

| File | Changes |
|------|---------|
| `SettlementManager.cs` | `PopulateSaveData()` now saves building data name in `assignedBuildingId`; `LoadSaveData()` builds lookup by building name and restores workers |
| `SaveData.cs` | Added comment clarifying `assignedBuildingId` stores BuildingData SO name |

#### 2. Season Effects Fix
Season effects now always apply on load, not just when season differs.

**Files Modified:**

| File | Changes |
|------|---------|
| `SeasonManager.cs` | `LoadSaveData()` always calls `ApplySeasonEffects()` regardless of whether season matches |

#### 3. Production Speed Scaling
Production now scales with `GameTickManager.TimeScale` like days/seasons do.

**Files Modified:**

| File | Changes |
|------|---------|
| `SettlementManager.cs` | `FastUpdate()` now uses `Time.deltaTime * GameTickManager.Instance.TimeScale` |

---

### Manager Singleton Architecture Fix

**Problem:** UI references (time speed buttons) broke on second load because `GameTickManager` used `DontDestroyOnLoad`, causing the old instance to persist while scene UI referenced the destroyed new instance.

**Solution:** Reorganized which managers persist vs. are scene-local.

**Managers that PERSIST (DontDestroyOnLoad):**
- `GameManager` - Scene transitions, slot tracking
- `SaveManager` - Save/load across scenes
- `RaidManager` - Raid state across scene transitions

**Managers that are SCENE-LOCAL:**
- `GameTickManager` - UI buttons wire to scene instance
- `SettlementManager` - Scene villagers/buildings
- `DayNightManager` - Scene lighting
- `SeasonManager` - Scene visual effects
- `ResourceManager` - Scene-local
- `JarlManager` - Scene villager references
- `PauseManager` - UI references
- `MissionManager` - Scene-local
- `VillagerSpawner` - Scene-local

**Files Modified:**

| File | Changes |
|------|---------|
| `GameTickManager.cs` | Removed `DontDestroyOnLoad`; Changed to scene-local singleton pattern; Added `OnDestroy()` to clear Instance and events |
| `DayNightManager.cs` | Added `OnDestroy()` to unsubscribe from GameTickManager events |
| `SettlementManager.cs` | Added GameTickManager unsubscription to `OnDestroy()` |

**Important:** Persisting managers (`GameManager`, `SaveManager`, `RaidManager`) must be **root-level objects** in the hierarchy, not children of other GameObjects. `DontDestroyOnLoad` only works on root objects.

---

### Raid System Fixes

**Problem:** After loading a save, going on a raid broke:
- RaidManager was destroyed despite `DontDestroyOnLoad`
- Villagers spawned at wrong positions
- No player controller in raid scene
- Workers not restored when returning from raid

**Solutions:**

#### 1. DontDestroyOnLoad Root Object Requirement
RaidManager was a child of another GameObject. When the parent was destroyed on scene change, RaidManager went with it.

**Fix:** Move `RaidManager`, `SaveManager`, and `GameManager` to root level in hierarchy (not children of other objects).

#### 2. Villager Reintegration After Raid
Villagers returning from a raid weren't registering with the new SettlementManager instance.

**Files Modified:**

| File | Changes |
|------|---------|
| `VillagerSpawner.cs` | `ReintegrateSurvivors()` now registers villagers with SettlementManager and calls `ReinitializeAfterSceneChange()` |
| `Villager.cs` | Added `ReinitializeAfterSceneChange()` method to re-cache manager references and clear raid flag |

#### 3. Worker Assignment Preservation Across Raids
Worker assignments were lost because building references became stale during scene transition.

**Files Modified:**

| File | Changes |
|------|---------|
| `RaidManager.cs` | Added `savedWorkerAssignments` dictionary; `StartRaid()` saves all worker assignments before raid; Added `GetSavedWorkerAssignment()` and `ClearSavedWorkerAssignments()` |
| `VillagerSpawner.cs` | Added `RestoreWorkerAssignments()` method; Called after reintegration to restore workers to buildings by name matching |

**Raid Worker Flow:**
1. `StartRaid()`: Saves villager ID → building name mappings
2. Raid scene loads, villagers persist via DontDestroyOnLoad
3. `ReturnToSettlement()`: Settlement scene loads
4. `ReintegrateSurvivors()`: Moves villagers back to scene, registers with SettlementManager
5. `RestoreWorkerAssignments()`: Finds buildings by name and reassigns workers

---

### Shield Save/Load Fix

**Problem:** Shields weren't being saved correctly. Debug logging showed `shield=''` when saving, even though shields were equipped. Fresh spawns worked fine but loaded villagers had no shields.

**Root Cause:** The `EquipableItem.itemName` field was empty on shield prefabs. Weapons had their `itemName` set, but shields did not.

**Solution:** Added `GetEquipableItemName()` helper method that falls back to the GameObject name (with "(Clone)" suffix stripped) when `itemName` is empty.

**Files Modified:**

| File | Changes |
|------|---------|
| `SettlementManager.cs` | Added `GetEquipableItemName(EquipableItem)` helper; Now used for both weapon and shield saving |

**Helper Method:**
```csharp
private string GetEquipableItemName(EquipableItem item)
{
    if (item == null) return "";
    if (!string.IsNullOrEmpty(item.itemName)) return item.itemName;

    // Fall back to GameObject name, stripping "(Clone)" suffix
    string goName = item.gameObject.name;
    if (goName.EndsWith("(Clone)"))
        goName = goName.Substring(0, goName.Length - 7).Trim();
    return goName;
}
```

**Note:** For best results, set `itemName` on all EquipableItem prefabs to match the name used in WeaponDatabase. The fallback handles cases where it's not set.

---

### Known Issues

1. ~~**Shield not loading from save** - Possible WeaponDatabase issue~~ ✅ FIXED
2. **No in-game save UI** - Main menu handles save/load, but no pause menu save option yet
3. **No "Return to Menu" button wired** - `GameManager.ReturnToMainMenu()` exists but not exposed in UI

---

## Recent Updates (February 11, 2026)

### 2D Dynamic Shadow System - Multi-Light Support

**Problem:** Shadow system only supported sun shadows. Needed fires/torches to cast shadows away from the light source, with proper day/night blending.

**Solution:** Created trigger-based light detection system with automatic shadow registration.

**New Files:**

| File | Location | Purpose |
|------|----------|---------|
| `ShadowCastingLight.cs` | `Assets/Scripts/2D Dynamic Shadows/` | Attach to Light2D (fires/torches). Uses CircleCollider2D trigger to detect nearby shadow casters. Auto-syncs radius to Light2D.pointLightOuterRadius |

**Modified Files:**

| File | Changes |
|------|---------|
| `DynamicShadow2D.cs` | Added `RegisterAutoLight()` / `UnregisterAutoLight()` for trigger-based registration; Added day/night blend factor (`nightThreshold`, `dayThreshold`); Fire shadows only visible at night; Creates separate shadow object per light source |
| `ShadowMaster.cs` | Fixed timing issue - shadows disappearing in play mode; `OnEnable` no longer calls `RefreshShadows`; `CleanupOrphanedShadows` only runs in editor mode |
| `TorchFlicker.cs` | Added position flickering (`flickerPosition`, `positionAmount`, `positionSpeed`) so shadows react to torch movement |

**How It Works:**
1. `ShadowCastingLight` uses `OnTriggerEnter2D`/`OnTriggerExit2D` to detect objects with `DynamicShadow2D`
2. When object enters range, `RegisterAutoLight()` creates a new shadow object for that light
3. Shadow direction calculated from object position to light position
4. Shadow intensity fades based on distance to light
5. Fire shadows blend out during day (`dayThreshold`), blend in at night (`nightThreshold`)

**Unity Setup:**
1. Add `ShadowCastingLight` component to any Light2D (fire, torch, etc.)
2. Set `lightHeight` (0-1, lower = longer shadows)
3. Set `shadowIntensity` (0-1)
4. Collider radius auto-syncs to Light2D outer radius

---

### Weather Manager System

**Problem:** Needed persistent weather effects across scenes with random weather changes.

**New Files:**

| File | Location | Purpose |
|------|----------|---------|
| `WeatherManager.cs` | `Assets/Scripts/Managers/` | Singleton with DontDestroyOnLoad; Spawns weather prefabs; Random weather on scene load; Sun dimming during storms |

**Features:**
- **Weather Types:** Clear, Sunny (sun beams + dust), Rain, Snow, Storm (rain + lightning)
- **Scene Persistence:** Spawns fresh effects each scene, picks random weather on load
- **Duration:** Weather lasts 0.5-1 in-game days, then changes randomly
- **Sun Dimming:** Sun intensity reduced during Rain/Storm via `stormSunIntensityMultiplier`
- **Gradual Fade:** Particle effects use `StopEmitting` behavior for smooth transitions
- **Inspector Testing:** `[InspectorButton]` for Apply/Stop weather in editor

**Modified Files:**

| File | Changes |
|------|---------|
| `LightningController.cs` | Removed `active = false` from Start() - WeatherManager controls it; Fixed thunder sound check from `< 0` to `> 0` |

**Weather Manager API:**
```csharp
WeatherManager.Instance.SetWeather(WeatherType.Storm);
WeatherManager.Instance.EnableRain(true);
WeatherManager.Instance.SetRainIntensity(100f);
WeatherManager.Instance.GetCurrentWeather();
```

**Unity Setup:**
1. Create `WeatherManager` GameObject (root level for DontDestroyOnLoad)
2. Assign prefabs: rainPrefab, snowPrefab, sunBeamsPrefab, sunDustPrefab, firefliesPrefab, lightningPrefab
3. Add scene names to `excludedScenes` list (e.g., "MainMenu")
4. Weather auto-starts on scene load

---

## Demo Progress Checklist

### Visual Systems
- [x] Day/Night cycle with sun movement
- [x] Dynamic 2D shadows from sun (ShadowMaster)
- [x] Dynamic 2D shadows from fires/torches (ShadowCastingLight)
- [x] Day/night shadow blending (fire shadows at night only)
- [x] Weather system (rain, snow, storm, sunny)
- [x] Lightning with thunder sounds
- [x] Sun beams with drift animation
- [x] Sun dust particles
- [x] Fireflies (night effect)
- [x] Torch flicker with position movement
- [x] Seasonal visual effects

### Core Systems
- [x] Villager AI and pathfinding
- [x] Building construction and production
- [x] Resource gathering and management
- [x] Combat system with damage/defense
- [x] Jarl succession system
- [x] Save/Load system
- [x] Skill tree (persists through succession)
- [x] Mission/Quest system
- [x] Raid system

### UI
- [x] Resource display
- [x] Villager info panel
- [x] Building placement
- [x] Mission tracker
- [x] Succession UI (Canvas-based) - verify working
- [x] Skill tree UI
- [x] Main menu save/load UI (slot selection, autosave, delete confirmation)
- [x] Return to main menu button

### Polish Needed
- [ ] Visual Jarl indicator (crown/aura)
- [ ] Game over screen
- [ ] Tutorial/intro sequence
- [x] Audio manager (importing from other project)
- [ ] Sound effects for actions
- [ ] Background music system
