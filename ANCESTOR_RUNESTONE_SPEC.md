# Ancestor Runestone System — Design Spec
**Game:** Jarl-Born
**Version:** 1.0
**Date:** February 16, 2026

## Implementation Status (updated 2026-06-27)
| Component | Status |
|-----------|--------|
| `RunestoneManager.cs` | ✅ Implemented — manages active runestones, pool, save/load |
| `DeathTypeBuff.cs` | ✅ Implemented — 10-min real-time buff, saves remaining time |
| `RunestoneUI.cs` | ✅ Implemented |
| 12 runestone pool | ✅ Implemented as `RunestoneType` enum |
| Selection logic (3 options, dead Jarl's skill category) | ✅ Implemented |
| Replacement UI (4th death forces swap) | ✅ Implemented |
| Runestone bonuses applied via `RunestoneManager` query methods | ✅ Implemented |
| Death cause detection (combat vs peaceful) | ✅ Implemented |
| Save/load integration | ✅ Implemented |
| Runestone inspection UI (click stone to view) | ❓ Unknown — verify in scene |
| Visual stone placement animation | ❓ Unknown — verify in scene |
| Ancestors' Fury cooldown reduction (requires Jarl abilities) | ❓ Jarl abilities status unknown |
| Swift Recovery (halves heal time) | ✅ Implemented via `WoundManager` |

**B28 fixed:** `SelectRunestone` at capacity now logs an error and leaves selection open for UI recovery instead of silently skipping `CompleteSelection()`.

---

**Status:** Implemented

---

## Overview

When a Jarl dies, their legacy is carved into a runestone in the village. The player selects a permanent village-wide bonus from 3 options. Additionally, the manner of death (battle vs peaceful) grants a temporary buff to ease the transition to the new Jarl.

This system is the primary generational reward. It should feel like a consolation prize that makes death meaningful rather than punishing.

---

## System Flow

### Trigger
Jarl dies (any cause: combat, old age, disease).

### Sequence
1. **Screen darkens** — gameplay pauses
2. **Succession UI** — player selects heir from ranked candidates (existing system)
3. **Heir confirmed** — new Jarl is set, control transfers
4. **Runestone UI appears** — "Honour [Dead Jarl Name]'s Legacy"
5. **Death Type Buff applied** — Valkyrie's Favour or Gefjon's Blessing (see §Death Type Buffs)
6. **3 runestone options displayed** — player picks 1
7. **Runestone placed** — stone appears at memorial site, bonus applied permanently
8. **Gameplay resumes**

Game remains paused throughout steps 1-7. No time passes.

---

## Death Type Buffs

The manner of the Jarl's death grants a **temporary 10-minute (real-time) buff** to the village. This eases the succession transition and rewards thematic play.

### Valkyrie's Favour (Battle Death)
**Trigger:** Jarl's HP reaches 0 from combat damage (enemy attack, raid combat).
**Duration:** 10 minutes real-time.
**Effects:**
- All warriors +5% damage
- All warriors +5% defense
- Jarl ability cooldowns -10%

**Thematic Intent:** The Jarl died a warrior's death. Valhalla honours them. The village fights harder in their memory.

### Gefjon's Blessing (Peaceful Death)
**Trigger:** Jarl dies from old age, disease, or any non-combat cause.
**Duration:** 10 minutes real-time.
**Effects:**
- Food production +10%
- Villager birth rate +10%
- Wounds heal 15% faster

**Thematic Intent:** The Jarl passed peacefully. Gefjon blesses the land. The village grows and heals.

### Design Notes
- **No stacking.** Only the most recent death type buff is active. If a new Jarl dies while the previous buff is still running, the old buff is replaced.
- **Player agency.** Players can deliberately send an aging Jarl into combat for Valkyrie's Favour, or keep them safe for Gefjon's Blessing. Both are valid strategies. A Viking choosing to die in battle is thematically correct, not an exploit.
- **Visual indicator.** A small icon in the HUD showing the active buff and remaining time (Valkyrie = red/gold icon, Gefjon = green icon).
- **Timer pauses** when game is paused (menu access, etc.).

---

## Runestone Selection

### Options Presented
Player is shown **3 runestone bonuses** and picks **1**. The other 2 are discarded.

### Selection Logic
The dead Jarl's highest skill category determines 1 of the 3 options. The other 2 are drawn randomly from the remaining pool.

**Skill Categorisation:**
- **Combat Jarl:** Highest skill is Combat, Archery, or Defense → 1 option from Combat pool
- **Economy Jarl:** Highest skill is Woodcutting, Mining, Farming, Fishing, or Crafting → 1 option from Economy pool
- **Tie-breaker:** If tied across categories, use whichever skill was levelled most recently

**Selection Algorithm:**
1. Determine dead Jarl's category (Combat or Economy)
2. Pick 1 random option from that category's pool (excluding already-active runestones)
3. Pick 2 random options from ALL remaining pools (excluding already-active and the option from step 2)
4. Present all 3 to the player

This ensures options feel connected to the dead Jarl's life while still offering variety.

### No Duplicates
Once a runestone bonus is active, it cannot appear as an option again. If a runestone is later replaced (see §Runestone Limit), its bonus returns to the available pool.

---

## Runestone Limit

### Maximum: 3 Active Runestones

The village has **3 fixed runestone slots** at a memorial site near the Longhouse.

**When a 4th+ Jarl dies:**
1. Runestone UI shows the 3 options as normal
2. Player picks their new bonus
3. Player is then shown the 3 existing runestones and must choose **which one to replace**
4. The replaced runestone's bonus is removed and returns to the pool
5. The new runestone takes its slot

### Design Intent
- Forces hard choices in late game — which ancestor's legacy do you preserve?
- Prevents runestone power from scaling infinitely
- Creates a visible memorial that changes over the campaign
- With 3-4 Jarl deaths in a typical campaign, most players will face this choice once (if at all in the demo)

### Visual
When inspected, each runestone shows:
- The dead Jarl's name
- How they died (battle/peaceful — could show Valkyrie or Gefjon symbol)
- The active bonus name and effect
- Carved rune art (you mentioned having art for this)

---

## Runestone Pool (12 Options)

### Economy (4 options)

| # | Name | Effect | Notes |
|---|------|--------|-------|
| 1 | **Rationing** | Villagers consume 1 less food per day | Strong economy shift. Changes food math significantly. |
| 2 | **Tireless Workers** | All production buildings +15% output speed | Universal, safe pick. Affects every building equally. |
| 3 | **Education** | New villagers spawn with +2 to 2 random skills | Compounds over generations. Stronger the later it's picked. |
| 4 | **Winter's Friend** | Firewood consumption -30% | Seasonal relief. Huge in winter, irrelevant in summer. |

### Combat (4 options)

| # | Name | Effect | Notes |
|---|------|--------|-------|
| 5 | **Ancestors' Fury** | Jarl ability cooldowns -30% | Selfish but powerful. Shield Wall/Heavy Strike more often. |
| 6 | **Iron Discipline** | All warriors +15% defense | Defensive. Keeps warriors alive longer in raids and horde. |
| 7 | **Weapon Maintenance** | All warriors +10% damage | Offensive. Faster kills, better raid performance. |
| 8 | **Resilient Blood** | All villagers +10% max HP | Broad survivability. Affects everyone, not just warriors. |

### Survival (2 options)

| # | Name | Effect | Notes |
|---|------|--------|-------|
| 9 | **Swift Recovery** | Wounds heal one tier faster | Light = instant. Serious = heals to full. Critical = heals to 70%. |
| 10 | **Fertile Lands** | Villager birth rate +20% | Population growth. More villagers = more workers/warriors. |

### Utility (2 options)

| # | Name | Effect | Notes |
|---|------|--------|-------|
| 11 | **Runekeeper's Gift** | Protection Runes cost 1 less stone brick | Niche but valuable for draugr preparation. |
| 12 | **Raiding Ships** | Raids yield +10% loot | Rewards aggressive play. Compounds with frequent raiding. |

### Balance Notes
- **Rationing** is the strongest single pick for economy-focused play. 1 less food per villager per day at 20+ villagers is 20+ food saved daily.
- **Education** is weak early, strongest late. Picking it on the first Jarl death maximises generational compounding.
- **Ancestors' Fury** is the only Jarl-specific option. It's selfish but transforms combat feel.
- **Swift Recovery** depends on the wound system being implemented. If wounds aren't in the demo, swap for a placeholder.
- Pool can be expanded for full game (DLC could add themed runestones).

---

## Runestone Cost

**None.** Runestones are free and automatic on every Jarl death.

Previous GDD referenced 5 stone bricks — this is removed. The runestone is a core generational reward, not an optional purchase. Gating it behind resources would punish struggling players.

---

## Implementation Notes

### New Files Needed

| File | Purpose |
|------|---------|
| `RunestoneManager.cs` | Singleton. Tracks active runestones (max 3), available pool, applies/removes bonuses. Handles selection logic. |
| `RunestoneData.cs` | Enum or ScriptableObject for each of the 12 runestone types. Stores name, description, effect type, effect value. |
| `RunestoneUI.cs` | UI panel for selection (3 cards), replacement (pick which to remove), and inspection (view active stones). |
| `DeathTypeBuff.cs` | Manages the 10-minute temporary buff. Tracks active buff type, remaining time, applies/removes modifiers. |

### Modified Files

| File | Changes |
|------|---------|
| `JarlManager.cs` | After succession completes, trigger RunestoneManager selection flow. Pass dead Jarl's skill data for category determination. Pass death cause for death type buff. |
| `SettlementManager.cs` | Apply runestone modifiers to production speed, food consumption, etc. Subscribe to RunestoneManager events. |
| `Villager.cs` | Apply runestone modifiers to max HP, defense, damage, birth rate, healing speed. |
| `CharacterController.cs` | Apply Ancestors' Fury cooldown reduction to Jarl abilities. Apply death type buff cooldown reduction. |
| `SaveData.cs` | Add RunestoneSaveData: list of active runestone IDs (max 3), current death type buff (type + remaining time). |

### Integration with Existing Succession Flow

Current flow (from JarlManager.cs):
```
Jarl dies → OnCurrentJarlDied() → GetSuccessionCandidates() → UI shows candidates → SelectHeir() → OnJarlChanged event
```

New flow:
```
Jarl dies → OnCurrentJarlDied() → GetSuccessionCandidates() → UI shows candidates → SelectHeir()
    → DeathTypeBuff.Apply(deathCause)
    → RunestoneManager.StartSelection(deadJarlSkills)
    → RunestoneUI shows 3 options → Player picks 1
    → If 3 already active: RunestoneUI shows replacement choice → Player picks which to remove
    → RunestoneManager.ApplyRunestone(chosen)
    → OnJarlChanged event → Gameplay resumes
```

### Applying Bonuses

Runestone bonuses should be applied as **multipliers or flat modifiers** checked at the point of calculation, not baked into base stats. This makes them easy to add and remove (when replaced).

```
Example: Tireless Workers (+15% production speed)
In Building.GetProductionSpeed():
    float speed = baseSpeed * seasonMultiplier;
    speed *= RunestoneManager.Instance.GetProductionMultiplier();  // Returns 1.15 if active, 1.0 if not
    return speed;
```

Each runestone type maps to a specific modifier method on RunestoneManager. Buildings, villagers, and combat systems query RunestoneManager for their relevant bonuses.

### Death Cause Detection

`Villager.Die()` or `TargetHealth` needs to pass the cause of death to `JarlManager.OnCurrentJarlDied()`.

Simplest approach: add a `DeathCause` enum:
```
public enum DeathCause { Combat, OldAge, Disease, Cold, Starvation, Other }
```

`TakeDamage()` sets a `lastDamageSource` field. `Die()` checks:
- If `lastDamageSource` was an Enemy → `DeathCause.Combat`
- If age >= max age → `DeathCause.OldAge`
- Everything else → map appropriately

For death type buff purposes, only two outcomes matter:
- `Combat` → Valkyrie's Favour
- `Anything else` → Gefjon's Blessing

---

## Demo Scope

For the demo (Beats 1-2, 1-2 Jarl deaths):

**Must have:**
- RunestoneManager with pool of 12 options
- Selection UI (3 cards, pick 1)
- Bonuses apply correctly (at minimum: production speed, food consumption, max HP, damage, defense)
- Death type buff (Valkyrie/Gefjon) with timer
- Save/load integration

**Nice to have:**
- Replacement UI (pick which stone to remove) — may not trigger in demo if only 1-2 deaths
- Inspection UI (click runestone to see details)
- Runestone placement animation
- Visual Valkyrie/Gefjon indicator on the runestone itself

**Can defer:**
- Ancestors' Fury cooldown reduction (requires Jarl abilities to be implemented first)
- Swift Recovery (requires wound system)
- Runekeeper's Gift (requires protection runes)
- Fertile Lands (requires birth rate system to be tuneable)

If those systems aren't in the demo, replace those 4 options with simpler alternatives or reduce the pool to 8.

---

## Open Questions

1. **Runestone inspection UI** — is this a tooltip on hover, a click-to-open panel, or part of a larger village overview screen? Depends on how you're handling building inspection generally.
2. **Replacement confirmation** — when removing a runestone to make room, should there be a "are you sure?" confirmation? Losing a permanent buff is significant.
3. **Death type buff visual** — beyond the HUD icon, should the village itself look different? (Red tint for Valkyrie, green glow for Gefjon?) Or is the icon enough?
4. **Sound design** — runestone selection should feel weighty. Stone carving sounds, possibly a brief musical motif. Flag for audio pass later.
