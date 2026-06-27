# Combat, Durability & Wound System — Design Spec
**Game:** Jarl-Born
**Version:** 1.0
**Date:** February 17, 2026

## Implementation Status (updated 2026-06-27)
| Component | Status |
|-----------|--------|
| Shield system (all combatants, durability tracks) | ✅ Implemented |
| Shield breakage (shatters at 0 durability) | ✅ Implemented |
| Villager AI blocking | ✅ Implemented |
| Wound system — `WoundManager.cs` | ✅ Implemented |
| Wound effects (6 wound types, penalties applied) | ✅ Implemented |
| 3-wound max → death rule | ✅ Implemented |
| Healer Hut building | ✅ Implemented |
| Shield pickup (villagers grab dropped shields) | ✅ Implemented |
| Jarl active blocking (right-click hold) | ❓ Verify in PlayerController |
| Parry system (0.3s timing window, stun) | ❓ Unknown — check PlayerController |
| Jarl weapon durability (per-swing, forge repair) | ❓ Unknown — check EquipableItem |
| Post-raid villager weapon breakage roll | ❓ Unknown — check RaidManager |
| Wound visual indicators on sprites | ❓ Unknown — verify in VillagerPersonalUI |

**Status:** Core implemented. Parry, weapon durability, and post-raid breakage need verification.

---

## Overview

Combat revolves around shields as the primary defensive layer. Every combatant (Jarl and villagers) uses shields that absorb damage and degrade through combat. The Jarl has active blocking with a parry system that rewards timing. Weapons degrade for the Jarl only, while villager weapons are lost on death or break after raids. Wounds are rare but impactful permanent penalties that can be healed at cost.

---

## 1. Shield System (Universal)

Shields are the core defensive mechanic for all combatants. Shield durability IS shield health — when it hits 0, the shield shatters and the character takes HP damage directly.

### Shield Stats

| Property | Description |
|---|---|
| **Max Durability** | Total shield health. Higher = lasts longer in combat. |
| **Current Durability** | Decreases when the shield absorbs hits. At 0 the shield breaks. |
| **Defense Value** | Damage reduction when blocking (percentage or flat — see blocking section). |

### Shield Tiers

| Shield | Max Durability | Material Cost | Notes |
|---|---|---|---|
| Wooden Shield | 40 | 5 planks | Starting tier. Breaks after a few fights. |
| Iron Shield | 80 | 3 planks + 3 iron ingots | Mid-game upgrade. Lasts significantly longer. |
| Great Shield | 120 | 5 planks + 5 iron ingots | Late-game. High durability for Final Horde prep. |

*Durability values are starting points. Playtest and adjust.*

### Shield Breakage

- When current durability reaches 0, the shield **shatters** (visual/audio feedback: crack, pieces fly off)
- Character is now **unshielded** — all incoming damage goes directly to HP
- Broken shield is gone. Must be replaced from stockpile or forge production.
- Villagers with no shield should already have AI to pick up dropped shields (existing behaviour per implementation notes)

### Production Pressure

A 10-wave Final Horde with 20+ villagers could burn through dozens of shields. Shield production must be a constant forge priority throughout the campaign. This is intentional — it makes wood and iron valuable from start to finish and creates genuine pre-horde preparation pressure.

---

## 2. Blocking & Parrying (Jarl — Player Controlled)

### Input
- **Right-click (hold):** Raise shield / enter block stance
- **Right-click (timed, within 0.3s of incoming hit):** Parry

### Block (Hold Right-Click)
- Shield absorbs **all incoming damage** to its own durability (no HP bleed-through)
- Shield takes **full durability damage** equal to the attack's damage value
- Character cannot attack while blocking
- **Frontal only** — attacks from behind bypass the shield entirely

### Parry (Timed Right-Click)
- Must be activated within **0.3 second window** before an attack lands
- Shield absorbs **all incoming damage** but takes only **50% durability damage**
- Attacker is **stunned for 1.5 seconds** (counter-attack window)
- **Visual/audio feedback:** distinct parry sound, spark effect, brief slow-motion or screen flash
- Parry window of 0.3s is a starting value — tune based on playtesting. Too generous = shields never break. Too tight = frustrating.

### No Shield (Unshielded Jarl)
- Right-click does nothing (or a dodge roll if implemented later)
- All incoming damage goes directly to HP
- This state should feel dangerous and urgent — get a new shield or avoid combat

### Design Intent
Three tiers of player skill:
1. **No block:** Full HP damage. Bad play.
2. **Block:** No HP damage but shield degrades at full rate. Adequate play.
3. **Parry:** No HP damage, shield degrades at half rate, enemy stunned. Skilled play.

Skilled players preserve their shields longer and create more attack windows. The parry system rewards mastery without punishing casual players (blocking still works fine).

---

## 3. Blocking (Villagers — AI Controlled)

### Behaviour
- Villagers with shields **block based on AI decisions** (not player timing)
- AI alternates between attacking and blocking naturally during combat
- No parry mechanic for AI — villagers always take full durability damage on block
- Villagers without shields take all damage to HP

### Shield Wall Ability Interaction
- When the Jarl activates Shield Wall, nearby villagers raise shields and hold position
- During Shield Wall, villagers **block continuously** (don't alternate to attack)
- Shield Wall grants a flat defense bonus on top of normal blocking
- Shield durability still degrades during Shield Wall — it's not free, it costs shield health

### AI Shield Priority
- Villagers already have behaviour to pick up dropped shields (per existing implementation)
- Villagers should prioritise having a shield equipped before engaging in combat
- Unarmed/unshielded villagers should retreat from combat if possible

---

## 4. Weapon Durability (Jarl Only)

### How It Works
- Every Jarl weapon has a **max durability** and **current durability**
- **Each swing in combat costs 1 durability** (whether it hits or misses)
- Weapon stats (damage, speed) remain constant regardless of durability — a damaged sword still hits the same
- At **0 durability the weapon breaks and is permanently lost**
- Rare weapons found on raids are lost forever if they break

### Weapon Durability Values

| Weapon | Max Durability | Notes |
|---|---|---|
| Wooden Club | 15 | Starting weapon. Breaks fast. Disposable. |
| Iron Sword | 30 | Lowest metal tier. Lasts a few minor raids. |
| Iron Axe | 25 | Slightly less durable but higher damage. |
| Steel Sword | 50 | Mid-game upgrade. Comfortable durability. |
| Rare/Named Weapons | 60-80 | Found on raids. Best stats AND best durability. |

*Durability values are starting points. Playtest and adjust — if iron sword at 30 feels too low for the intended 2-3 minor raids, increase to 40-50.*

### Repair

- **Location:** Jarl walks to the forge, interacts with it
- **Cost:** Flat rate per durability point restored. **1 iron + 1 wood per point.**
- **Speed:** Instant on interaction (no waiting)
- **Partial repair:** Player can repair any amount they can afford
- **Example:** Iron Sword at 12/30 durability. Full repair = 18 points = 18 iron + 18 wood. Player has 10 iron + 10 wood, so repairs to 22/30.

### Design Intent
- Creates a personal resource sink that competes with villager equipment production
- Rare weapons are precious — let them break and they're gone forever
- Repair is instant but costs resources, so the tension is economic not time-based
- Player must balance: repair my sword vs forge new shields for my warriors

---

## 5. Villager Weapon Loss

Villager weapons do **not** degrade through normal combat. Instead, they are lost through two mechanisms:

### Death
- When a villager dies, their **weapon is lost** (destroyed/dropped)
- Their **shield** is dropped on the ground and can be picked up by another villager
- This means every villager death costs you a weapon replacement

### Post-Raid Breakage
- After completing a raid, each villager who participated in combat has a **percentage chance** their weapon broke during the raid
- Breakage is rolled once per villager at raid end, not per swing

| Weapon Material | Break Chance |
|---|---|
| Wooden weapons | 25% |
| Iron weapons | 10% |
| Steel weapons | 5% |

- Broken weapon is gone. Villager returns unarmed and needs a replacement from the forge.
- This creates a post-raid logistics check: "Who needs rearming?"
- Iron/steel weapons breaking less often is an additional incentive to upgrade beyond raw damage stats

### Design Intent
- No per-swing tracking for 20+ villagers (avoids micromanagement)
- Equipment loss creates ongoing forge demand at a manageable pace
- Death = weapon lost incentivises keeping villagers alive (your equipment stockpile is your army)
- Post-raid breakage = periodic restocking, not constant attention
- Material tier affects reliability, giving another reason to upgrade

---

## 6. Wound System

### Trigger
Any single hit that deals **more than 30% of the character's max HP** has a chance to inflict a wound. This only applies to HP damage — shield damage does not trigger wounds.

This means wounds only happen when:
- A character is unshielded (shield broken or not equipped)
- A character is hit from behind (bypasses shield)
- A character fails to block

### Wound Chance

Scales with hit severity:

| HP Damage (% of Max HP) | Wound Chance |
|---|---|
| 30-40% | 15% |
| 40-50% | 30% |
| 50-70% | 50% |
| 70%+ | 80% |

*Starting values. Tune based on how frequently wounds occur in playtesting. Wounds should be uncommon but not rare — maybe 1 wound every 2-3 serious fights without a shield.*

### Wound Effects

Each wound applies **1-2 specific penalties** averaging around -10% total impact. Wounds are drawn randomly from the wound pool.

| Wound | Penalty 1 | Penalty 2 | Flavour |
|---|---|---|---|
| **Lame** | -8% movement speed | — | Leg injury. Slower but still functional. |
| **One Eye** | -5% max HP | -5% attack damage | Depth perception lost. Weaker all-round. |
| **Battle-Scarred** | -5% max HP | -5% defense | Accumulated damage. More fragile. |
| **Broken Ribs** | -8% attack damage | — | Torso injury. Swings are weaker. |
| **Torn Shoulder** | -5% attack speed | -5% attack damage | Arm injury. Slower, weaker strikes. |
| **Iron Will** | -5% max HP | +10% morale resistance | Survived trauma. Tougher mentally, weaker physically. |

### Wound Rules
- **Maximum 3 wounds per character.** A 4th wound triggers death regardless of remaining HP.
- **Wounds stack.** A character with Lame + One Eye has -8% speed, -5% HP, -5% damage.
- **Wounds are permanent** until healed at the Healer Hut.
- **Wounds do not inherit.** They are per-character only. Equipment passes to heirs, wounds do not.
- **Wounds apply to both the Jarl and villagers.** Same system, same rules.

### Wound Visibility
- Wounded characters should have a visible indicator (icon, colour tint, or wound marker on their sprite)
- Villager info panel shows active wounds and their penalties
- Number of wounds visible at a glance (1/3, 2/3, 3/3 — with 3/3 highlighted as critical/near death)

---

## 7. Healing System

### Healer Hut (Building)

| Property | Value |
|---|---|
| **Type** | Production building (assign 1 villager) |
| **Worker** | Healer (gains Healing skill over time like any other worker) |
| **Function** | Passively heals one wounded villager at a time |
| **Healing Speed** | 1 wound removed per in-game day (affected by worker skill) |
| **Cost Per Wound** | 3 honey + 5 gold |
| **Cost is flat** | Same price for every wound on every character, every time |

### How It Works
1. Assign a villager to the Healer Hut (works like assigning a woodcutter to the lumber mill)
2. If any villager in the settlement has wounds, the healer begins treating the most wounded villager automatically (highest wound count first)
3. Treatment consumes 3 honey + 5 gold from stockpile per wound
4. If resources are insufficient, healing pauses until resources are available
5. One wound is removed per in-game day
6. Once the current patient is fully healed (or resources run out), the healer moves to the next wounded villager

### Player Interaction
- Player can **prioritise** which villager gets healed next (click healer hut, assign patient)
- Default behaviour: auto-select most wounded villager
- The Jarl can be assigned as patient — they go to the healer hut and are unavailable during treatment (1 day per wound)

### Resource Tension
- **Honey** is also used for mead production (Honey → Brewery → Mead for morale)
- **Gold** is obtained from raids
- Healing competes with morale production (honey) and requires aggressive play (gold from raids)
- A heavily wounded roster after a bad raid forces a choice: heal your warriors or brew mead to keep morale up?

### Design Intent
- Healing works like any other production building — consistent with every other system
- One healer means triage decisions: who gets healed first?
- Flat cost is simple to understand and balance
- Resource cost (honey + gold) ties healing into both economy and combat loops
- The Jarl being unavailable during healing creates an interesting cost — do you take a day off to heal or fight through your wounds?

---

## 8. Combat Summary

### Complete Damage Flow

```
Attacker swings
    ↓
Is target blocking/parrying? ──── YES (has shield) ────→ PARRY?
    │                                                       │
    NO                                                  YES: Shield takes 50% durability damage
    │                                                       Attacker stunned 1.5s
    ↓                                                       │
Target takes full HP damage                             NO (regular block):
    │                                                       Shield takes 100% durability damage
    ↓                                                       Target takes 0 HP damage
Was HP damage > 30% max HP?                                 │
    │                                                       ↓
    YES → Roll wound chance                             Is shield at 0 durability?
    │         │                                             │
    │     WOUND → Apply random wound from pool          YES → Shield shatters
    │     NO WOUND → Continue                               (character now unshielded)
    │                                                   NO → Shield holds
    ↓
Is character at 0 HP? → Death
Does character have 3 wounds + new wound? → Death
```

### Equipment Degradation Summary

| Item | Who | Degrades When | Lost When | Replacement |
|---|---|---|---|---|
| Jarl Weapon | Jarl only | Per swing (1 dur per swing) | 0 durability | Repair at forge (1 iron + 1 wood per point) or craft new |
| Villager Weapon | Villagers | Doesn't degrade in combat | Villager dies OR post-raid breakage roll (10-25% chance) | Craft new at forge |
| All Shields | Everyone | Per hit absorbed in combat | 0 durability (shatters mid-combat) | Craft new at forge, pick up dropped shields |

### Jarl vs Villager Combat Comparison

| Mechanic | Jarl (Player) | Villager (AI) |
|---|---|---|
| **Blocking** | Active (right-click hold) | AI-driven (automatic) |
| **Parrying** | Timed (0.3s window, right-click) | No parry capability |
| **Shield Damage** | 50% on parry, 100% on block | Always 100% |
| **Weapon Degradation** | Per swing, must repair | No degradation, lost on death or post-raid roll |
| **Wounds** | Same system | Same system |
| **Abilities** | Shield Wall, Heavy Strike (1, 2 keys) | Respond to Shield Wall, no personal abilities |

---

## 9. Implementation Notes

### New Files Needed

| File | Purpose |
|---|---|
| `DurabilitySystem.cs` | Component for all equippable items. Tracks max/current durability, handles degradation and breakage events. |
| `WoundManager.cs` | Tracks wounds per character. Rolls wound chance on big hits. Applies/removes wound penalties. |
| `WoundData.cs` | ScriptableObject or enum defining each wound type and its penalties. |
| `HealerHut.cs` | Building script (extends Building). Manages healing queue, resource consumption, treatment progress. |
| `ParrySystem.cs` | Component on PlayerController. Detects parry timing window, triggers stun on attacker. |

### Modified Files

| File | Changes |
|---|---|
| `EquipableItem.cs` | Add `maxDurability`, `currentDurability` fields. Add `TakeDurabilityDamage(float, bool isParry)` method. Fire `OnItemBroken` event at 0. |
| `TargetHealth.cs` | In `TakeDamage()`: check if blocking → route damage to shield durability instead of HP. Check if parry → halve shield damage + stun attacker. If HP damage dealt, check wound threshold. |
| `Villager.cs` | Add `List<WoundData> activeWounds`. Apply wound penalties to stats. On death: drop shield (pickup-able), destroy weapon. Add wound count check (3 + new = death). |
| `PlayerController.cs` | Add parry detection (right-click timing vs incoming attack). Add block state (right-click hold). Weapon durability loss per swing. |
| `RaidManager.cs` | On raid end: roll weapon breakage per villager participant. Remove broken weapons. |
| `CharacterController.cs` | Add `ApplyStun(float duration)` method for parry stun and Heavy Strike stun. |
| `Building.cs` | Add forge repair interaction for Jarl (if forge is the repair location). |
| `SaveData.cs` | Add durability to equipment save data. Add wound list per villager save data. Add healer hut patient queue. |

### Integration with Existing Systems

**Shield Wall Ability:**
- When activated, all villagers in radius enter forced block state
- Their shields take durability damage as normal during Shield Wall
- Shield Wall doesn't prevent shield breakage — if a villager's shield breaks during Shield Wall, they lose the defensive benefit

**Succession / Inheritance:**
- Jarl's equipment (with current durability) passes to heir
- Wounds do NOT pass to heir
- A heavily damaged weapon inherited by the heir should be an immediate "go repair this" moment

**Runestone: Swift Recovery:**
- Wounds heal one tier faster at the Healer Hut
- With this system (single wound tier), reinterpret as: healing takes half the time (0.5 days per wound instead of 1 day)

**Runestone: Resilient Blood (+10% max HP):**
- Increases the HP threshold for wound chance (30% of a bigger number = bigger hit needed to trigger wound roll)
- Indirect wound prevention — thematic and mechanical synergy

---

## 10. Demo Scope

### Must Have
- Shield durability system (shields break in combat)
- Basic blocking for Jarl (right-click hold, shield absorbs damage)
- Villager AI blocking (existing AI with shield check)
- Wound system (big hits → wound chance → random wound from pool)
- Wound penalties applied to character stats
- 3 wound max → death rule
- Healer Hut as production building (assign worker, consumes honey + gold, heals wounds)

### Nice to Have
- Parry system (0.3s timing window, 50% shield damage, stun)
- Jarl weapon durability and forge repair
- Post-raid villager weapon breakage rolls
- Wound visual indicators on sprites

### Defer to Post-Demo
- Villager weapon degradation in combat (already agreed: cut)
- Advanced wound types beyond the initial 6
- Healer skill progression (healer gets faster with experience)
- Multiple healer huts

---

## 11. Open Questions

1. **Forge interaction for repair** — is the forge a building the Jarl walks up to and clicks, or is it accessed through a menu? Walking to it fits the "you ARE the Jarl" pillar but requires the Jarl to physically be at the forge.
2. **Shield pickup priority** — when a shield drops in combat, who picks it up first? Nearest villager? Or should the player be able to direct "you, grab that shield"?
3. **Wound notification** — when a wound is inflicted mid-combat, how prominent is the notification? A brief flash + icon? A pause with "Lame — -8% speed" text? Combat is real-time so it can't be too disruptive, but wounds are significant enough that the player should notice.
4. **Rare weapon discovery** — where do rare Jarl weapons come from? Raid loot tables? Specific quest rewards? Both? This needs defining but is separate from the core combat spec.
5. **Healer patient priority** — auto-select most wounded, or does the player always manually assign? If auto, does the Jarl get priority, or is the Jarl treated the same as any villager in the queue?
