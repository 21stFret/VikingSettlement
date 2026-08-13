# Combat System

**Core locations:** `Assets/Scripts/AI/States/Combat/*.cs` (FSM states), `Assets/Scripts/AI/CharacterAI.cs` / `CombatAIBase.cs` (AI brain), `Assets/Scripts/CharacterBase.cs` (shared character logic, animation events, slots), `Assets/Scripts/AI/FightManager.cs` (shared separation data), `Assets/Scripts/AI/CombatFighterStats.cs` (tuning ScriptableObject)

## Overview

Combat is a pure-C# finite state machine driven per-character by `CharacterAI`. Every villager is a combatant — there is no non-combat job gate — and the player (Jarl) is just a possessed `Villager` with its own AI disabled, so AI-vs-AI and AI-vs-player combat share the exact same slot/event/damage machinery. Ranged combat is not implemented.

## State Machine

Six live states, all under `Assets/Scripts/AI/States/Combat/`:

| State | Purpose |
|---|---|
| `CombatApproachState` | Closes distance to the target, claims an engagement slot (or orbits at `ThreatCircleDistance` if the target is full), gives way to other fights while travelling. → `CombatPressureState` on arrival. |
| `CombatPressureState` | Holds at attack range, watches the target's animation phase via `CombatAnimationListener`. → `CombatAttackState` when its own attack cooldown and an aggression roll both clear; → `CombatBlockState` on opponent windup; back to `CombatApproachState` if knocked out of range. |
| `CombatAttackState` | Triggers `Controller.Attack()`, locks the FSM until its own `OnAttackRecoveryEvent` fires. → `CombatRecoveringState`. |
| `CombatRecoveringState` | Fixed cooldown (`CombatFighterStats.RecoveryTime`) before returning to `CombatPressureState`. Can still detour to `CombatBlockState` if the opponent windows up (advanced blockers). |
| `CombatBlockState` | Reacts to a detected opponent windup — blocks (consumes a charge) or dodges, based on what's actually available and `BlockVsDodgeRatio`. Resumes on the target's recovery event. |
| `CombatStunnedState` | Entered externally, overriding whatever state the fighter was in, when `CharacterBase.OnStunned` fires (parry punish). Waits out the duration, then → `CombatRecoveringState`. |

Villagers additionally route through **`VillagerPrepareCombatState`** (`AI/States/Villager/`) before `CombatApproachState`, to find and equip a nearby unequipped shield first.

There is **no retreat/flee state and no ranged positioning state** in the current codebase — an earlier design (`CombatRetreatState`, `CombatFleeMeleeState`, `CombatRangedPositioningState`) was deliberately removed (commit `9a978c8`, "Target acquisition audit: remove dead code, drop flee/retreat, fix pile-on tiebreak"). A generic, unrelated `FleeState` stub exists (`AI/States/FleeState.cs`) but is never instantiated anywhere and has no combat role.

## Event Wiring

`CharacterBase` fires combat events from Animator animation-event callbacks on the character itself:

- `OnAttackWindupEvent` — start of a swing (`NotifyAttackWindup()`)
- `OnAttackWindowEvent` — the actual hit-scan/damage frame (`PerformAttackHitbox()`, does an `OverlapBoxAll` and applies damage)
- `OnAttackRecoveryEvent` — end of the attack animation (`StopAttacking()`)
- `OnHitByAttacker` — fired on the victim the instant a hit lands (`OnHitBy(attacker)`, called from the attacker's `OnHitTarget`)
- `OnStunned` — fired when a stun window starts (e.g. a punished parry, via `CheckParryAndStun`)

`CombatAnimationListener` subscribes to a **target's** three attack events and re-broadcasts them as its own `OnWindup` / `OnAttackWindow` / `OnAttackRecovery` — this is how a fighter observes its *opponent's* swing phase without polling. States call `SetTarget()` on entry so the listener always tracks the current target.

**End-to-end duel flow:**
1. `CombatPressureState` wires the listener's `OnWindup` → detour to `CombatBlockState`, and `OnAttackRecovery` → attempt its own attack.
2. Attacker swings → `NotifyAttackWindup()` fires → listener relays `OnWindup` → defender reacts.
3. `PerformAttackHitbox()` fires the hit-scan; on a landed hit, the victim's `OnHitBy` fires `OnHitByAttacker` → `CombatAIBase.HandleHitBy` (retargeting).
4. `StopAttacking()` fires `OnAttackRecoveryEvent` on the attacker — ends its own `CombatAttackState` and, via the listener, ends the defender's block/triggers the defender's own attack attempt.
5. If the defender was parrying at impact, `CheckParryAndStun` stuns the *attacker*, forcing it into `CombatStunnedState` regardless of what it was doing.

## Engagement Slot System

Attackers claim positional slots around a target rather than stacking on top of it (`CharacterBase`, `#region Combat Slots`):

- `TryClaimSlot` — rejects if the host is already at `MaxAttackers`; computes an angle via `CalculateBisectAngle` (first occupant snaps to its live compass bearing; each subsequent occupant bisects the largest free gap, with a lone occupant treated as leaving the full 360° free so a 2nd claimant lands directly opposite).
- `UpdateSlotAngle` (every frame, via `CharacterAI.RefreshEngagementSlot()`) — the "main" occupant (the one the host is reciprocally engaged with) live-tracks its real bearing; every other ("extra") occupant is redefined each frame as a fixed offset from that live main angle, evenly spread across the remaining arc. This avoids extras independently re-bisecting against each other's shifting positions.
- `ReleaseSlot` / `ReleaseAllSlots` — a death (`TargetHealth.Die()`) immediately frees all of a host's attackers.

**Reciprocal engagement lock:** `IsEngagedWithHost` is true only when a host's own `CurrentTarget` points back at the attacker — a genuine mutual 1:1 bond, satisfiable by at most one attacker per host. `TryForceReciprocalLock` runs whenever a fighter claims a slot: if the target isn't already pursuing this fighter and isn't genuinely engaged elsewhere, it force-commits the target back — snapping first-come pairings into a clean, non-crossed bond before independent per-fighter search ticks can scramble a multi-attacker fight into criss-crossed pairings.

**Pile-on / "extra" peel-off:** a fighter holding a slot but *not* the reciprocally-engaged main (an unrequited extra piled onto a target someone else is duelling) checks every search tick for a genuinely free target elsewhere; if one exists, it releases its slot and pairs off into a fresh 1v1. This is how a 2-vs-1 naturally resolves into two separate duels as reinforcements arrive.

## Block / Dodge / Stun Economy

Tuning lives on `CombatFighterStats` (ScriptableObject): `AggressionLevel`, `BlockVsDodgeRatio`, `CanDodge`, `AdvancedBlocking`, `ThreatCircleDistance`, `PressureTime`, `RecoveryTime`, `MaxBlockCharges`, `BlockCooldown`.

- **Block charges** live on `CharacterAI`: `CanBlock` (charges remaining), `CanBlockNow` (also requires an unbroken shield), `CanDodgeNow` (requires `CombatStats.CanDodge` and not already rolling/attacking). `ConsumeBlockCharge()` decrements and starts a cooldown once depleted; villagers recover cooldown faster with higher combat skill (`GetCombatSkillMultiplier`).
- `CombatBlockState` only rolls between options that are actually available right now (never "attempts" something impossible), weighted by `BlockVsDodgeRatio`.
- **Blocking** halves move speed; hitting a blocking/parrying target knocks the attacker back.
- **Parrying** (a short window after the block input) halves shield-durability damage vs. a plain block, and fully negates HP damage if not hit from behind — the damage is dumped into shield durability instead.
- `CheckParryAndStun` stuns an attacker who hits a parrying target with an unbroken shield (default 1.5s), forcing `CombatStunnedState` and overriding whatever state the attacker was in.
- **Stun** (`CharacterBase.ApplyStun`) immobilizes, clears blocking/parrying flags, pauses the attack cooldown clock, and fires `OnStunned`.

## Movement / Separation

Simultaneous nearby fights are kept from overlapping/jittering via `FightManager` (`AI/FightManager.cs`), a self-instantiating scene-local singleton:

- Tracks `ActiveHosts` (any character currently hosting a fight), kept in sync via slot claim/release notifications — no per-frame scanning needed.
- Computes the canonical list of nearby fights **once per frame**, then serves each observer a memoized, filtered view (`GetFightsFor`) — replacing an earlier design where every fighter recomputed this independently every frame.
- `CharacterAI.CalculateSeparationForce` sums a push away from each nearby fight (with falloff), clamps the *total* magnitude regardless of how many fights contribute, and exponentially smooths the result frame-to-frame to prevent feedback-loop jitter between mutually-reacting agents.
- `MoveWithSeparation`: if genuinely crowded, moves purely along the separation push (ignoring the destination) rather than fighting a pull-vs-push tug of war; otherwise blends the destination pull with body-avoidance (arcing around the target instead of cutting through it) and tapers speed near arrival to prevent overshoot/bounce.
- `CombatApproachState` only actively avoids other fights once it has arrived and committed to its slot — while still travelling, it goes straight there, with a separate "give-way" mechanism if the target itself gets shoved by a different fight.

## Retargeting

`CombatAIBase.OnTargetSearchTick` (0.5s cadence) handles two cleanup paths:

- **Pursuit timeout**: if a target has been out of `PursuitRange` for longer than `LoseTargetTime`, the fighter releases its slot and gives up — even mid-engagement, since that's exactly the "target fled" case.
- **On-hit retargeting** (`HandleHitBy`, gated per-character by `retargetOnHit` — on by default for villagers, off by default for enemies): taking a hit from a new attacker releases the old slot and re-engages onto the hitter via the normal acquisition path (so it also gets a reciprocal-lock attempt for free). Guards against a corpse claiming a slot on its own killer, since a killing blow can resolve before the victim's own hit-reaction runs.

## Other Notes

- **Player/AI interaction**: `PlayerController` drives the possessed villager's `CharacterBase` directly and disables its AI (`SetAIEnabled(false)`). Slot bookkeeping and reciprocal-lock logic still operate on the player normally, since that data lives on `CharacterBase`/`CharacterAI`, not the FSM. `FaceTowards` explicitly skips locking the player's facing direction, since nothing would ever be able to clear that lock while their own AI update loop is disabled.
- **Villagers vs. enemies**: villagers route through `VillagerPrepareCombatState` for shield pickup, support raid behaviors (Follow/ShieldWall/Aggressive), and scale block-cooldown recovery by combat skill; enemies pull stats from an `Enemy` component and default to not retargeting on hit.
- **Damage/death**: `TargetHealth` handles HP/damage/invincibility windows and death, releasing all engagement slots (both as host and as attacker) before firing `OnDeath`. `Enemy`/`Villager` each override death/damage handling for faction-specific effects (XP + loot drops and delayed despawn for enemies; death-cause tracking, Jarl succession, and settlement unregistration for villagers).
- **Debug tooling**: `Assets/Scripts/Debug/CombatDummy.cs`, `CombatRecorder.cs`, `CombatReplayUI.cs`, `CombatTestManager.cs` form a separate combat-testing harness, not part of the live FSM.
