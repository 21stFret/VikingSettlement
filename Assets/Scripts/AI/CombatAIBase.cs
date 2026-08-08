using System.Linq;
using UnityEngine;

/// <summary>
/// Shared combat layer between CharacterAI and the faction-specific bases.
/// Provides a unified OnTargetSearchTick, retarget-on-hit handler, pack assist fallback,
/// and the IsInActiveCombatState guard used by the search tick and state routing.
///
/// Hierarchy:
///   CharacterAI
///     └── CombatAIBase  (this)
///           ├── VillagerAIBase
///           └── EnemyAIBase
/// </summary>
public abstract class CombatAIBase : CharacterAI
{
    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override void Start()
    {
        if (Controller != null)
        {
            Controller.OnHitByAttacker += HandleHitBy;
            Controller.OnStunned       += HandleStunned;
        }
        base.Start();
    }

    private void OnDestroy()
    {
        if (Controller != null)
        {
            Controller.OnHitByAttacker -= HandleHitBy;
            Controller.OnStunned       -= HandleStunned;
        }
        if (_subscribedShield != null)
            _subscribedShield.OnBroken -= HandleShieldBroken;
    }

    // ── Unified target search tick ─────────────────────────────────────────────

    protected override void OnTargetSearchTick()
    {
        RefreshNearbyFighters();

        // Dead target cleanup
        if (CurrentTarget != null && IsTargetDead(CurrentTarget))
        {
            ReleaseEngagementSlot();
            CurrentTarget = null;
        }

        // Give up on a target that's fled beyond PursuitRange for too long — fires even while
        // genuinely engaged (CurrentSlotHost != null), since that's exactly the case that would
        // otherwise be stuck chasing a target that's no longer there.
        if (CurrentTarget != null)
        {
            float dist = Vector2.Distance(transform.position, CurrentTarget.position);
            if (dist <= PursuitRange)
                LastTargetInRangeTime = Time.time;
            else if (Time.time - LastTargetInRangeTime >= LoseTargetTime)
            {
                ReleaseEngagementSlot();
                CurrentTarget = null;
            }
        }

        // Cutscene / shield-wall / immature life stage — no new acquisition or peel-off below,
        // but the unconditional cleanup above still ran, so a stale dead/fled target doesn't sit
        // uncleaned for the whole duration of the abort condition.
        if (ShouldAbortSearchTick()) return;

        if (CurrentSlotHost != null)
        {
            // Genuinely engaged main (host.CurrentTarget points back at me) — never voluntarily
            // leaves a real duel.
            if (IsEngagedWithHost) return;

            // I'm an unengaged "extra" piled onto this host — keep looking for a genuinely free
            // enemy to pair off into a fresh 1v1. SelectBestTarget already sorts by OccupiedCount
            // ascending, so any 0-occupant enemy in range surfaces first. Also require the
            // candidate isn't itself already the engaged main somewhere else — "nobody is
            // attacking it yet" doesn't mean attacking it would actually produce a 1v1 (it could
            // already be mid-duel elsewhere), which would just bounce this extra from one
            // unrequited pile-on to another every tick instead of landing a real pair.
            CharacterBase bestTarget = SelectBestTarget();
            if (bestTarget != null && bestTarget.OccupiedCount == 0)
            {
                var bestAI = bestTarget.AI;
                if (bestAI == null || !bestAI.IsEngagedWithHost)
                {
                    ReleaseEngagementSlot();
                    CurrentTarget = bestTarget.transform;
                    OnTargetAcquired(bestTarget, true);
                }
            }
            return;
        }

        // Not engaged yet — always pick whichever nearby valid target currently has the fewest
        // combatants (0 if a completely free one exists). Re-evaluated every tick until a slot
        // is secured, so an attacker approaching a target that fills up before it arrives will
        // split off toward a less-contested one instead of piling on.
        CharacterBase best = SelectBestTarget();
        if (best == null) return; // no enemies found → stay idle

        bool targetChanged = best.transform != CurrentTarget;
        if (targetChanged)
        {
            ReleaseEngagementSlot();
            CurrentTarget = best.transform;
            OnTargetAcquired(best, true);
        }
        else if (!IsInActiveCombatState())
        {
            // Same target as before but not actively pursuing it (e.g. very first tick) —
            // start the approach. Doesn't re-fire every tick once approach is under way.
            OnTargetAcquired(best, false);
        }
    }

    // ── Virtual hooks — override in subclasses for faction-specific behaviour ──

    /// <summary>Return true to skip the entire tick (e.g. cutscene, shield wall, life stage).</summary>
    protected virtual bool ShouldAbortSearchTick() => false;

    /// <summary>
    /// Called when a valid target is found and engagement should (re)start.
    /// The base tick already gates this call behind (targetChanged || !IsInActiveCombatState()).
    /// </summary>
    protected virtual void OnTargetAcquired(CharacterBase target, bool targetChanged)
        => ChangeState(GetApproachState());

    /// <summary>Called when no target exists and no living current target remains.</summary>
    protected virtual void OnNoTargetFound() { }

    /// <summary>Idle/passive state to fall back to when combat ends.</summary>
    protected virtual AIStateBase GetDefaultIdleState() => new IdleState();

    /// <summary>
    /// State to enter when closing on / holding at a freshly (re)acquired target. Melee default is
    /// CombatApproachState (claims a slot, walks to it). Overridden by ArcherAI (and anything else
    /// ranged) to return a slot-less positioning state instead — see RangedCombatState.
    /// </summary>
    internal virtual AIStateBase GetApproachState() => new CombatApproachState();

    /// <summary>
    /// State to resume once a discrete action (attack, block/dodge) finishes and the fighter should
    /// go back to holding at range. Melee default is CombatPressureState. Overridden alongside
    /// GetApproachState() for ranged fighters — see RangedCombatState and ArcherAI's comment on why
    /// both hooks map to the same state there.
    /// </summary>
    internal virtual AIStateBase GetPressureState() => new CombatPressureState();

    // ── Pack assist fallback ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the target of a nearby ally if own scan found nothing.
    /// Override to add faction-specific restrictions (e.g. raid-mode villagers skip this).
    /// </summary>
    protected virtual CharacterBase InheritAllyTarget()
    {
        foreach (var ally in NearbyAllies)
        {
            if (ally == null) continue;
            var allyAI = ally.AI;
            if (allyAI?.CurrentTarget == null) continue;
            if (IsTargetDead(allyAI.CurrentTarget)) continue;
            return allyAI.CurrentTarget.GetComponent<CharacterBase>();
        }
        return null;
    }

    // ── Active-combat guard ────────────────────────────────────────────────────

    public bool IsInActiveCombatState()
    {
        return CurrentState is CombatApproachState        ||
               CurrentState is CombatPressureState        ||
               CurrentState is CombatAttackState          ||
               CurrentState is CombatBlockState           ||
               CurrentState is CombatStunnedState         ||
               CurrentState is CombatRecoveringState      ||
               CurrentState is VillagerPrepareCombatState ||
               CurrentState is RangedCombatState          ||
               CurrentState is FindEquipmentState;
    }

    // ── Commitment ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Claims an engagement slot the instant a target is committed to, rather than waiting for
    /// whichever FSM state target acquisition routes into (CombatApproachState, or for a
    /// villager needing a shield first, VillagerPrepareCombatState) to get around to it. Without
    /// this, CurrentSlotHost lagged CurrentTarget for a real window — long enough that a fighter
    /// who had already committed to a target, but hadn't yet physically claimed a slot on it,
    /// looked indistinguishable from a totally free agent to TryForceReciprocalLock's "don't
    /// hijack a committed fighter" guard (which only checks CurrentSlotHost). Only remaining
    /// case where CurrentSlotHost can still lag CurrentTarget is the target being genuinely at
    /// MaxAttackers capacity — CombatApproachState.OnUpdate's orbit-and-retry loop is still the
    /// correct fallback for that, and is left unchanged.
    /// </summary>
    protected override void OnCurrentTargetChanged(CharacterBase newTarget)
    {
        if (Controller == null || newTarget == CurrentSlotHost) return;
        ReleaseEngagementSlot();
        if (newTarget.TryClaimSlot(Controller, out _))
        {
            CurrentSlotHost = newTarget;
            TryForceReciprocalLock(Controller, newTarget);
        }
    }

    // ── Reciprocal engagement ───────────────────────────────────────────────────

    /// <summary>
    /// Forces this fighter to immediately commit to <paramref name="attacker"/>, via the same
    /// subclass-specific OnTargetAcquired hook the normal search tick uses — so villager
    /// shield-prep routing still applies. Does NOT construct CombatApproachState directly, since
    /// OnTargetAcquired may need to route through VillagerPrepareCombatState first. The explicit
    /// ReleaseEngagementSlot below is belt-and-suspenders — CurrentTarget's setter already
    /// releases any stale slot via OnCurrentTargetChanged once assigned below.
    /// </summary>
    public void ForceReciprocalEngagement(CharacterBase attacker)
    {
        if (ShouldAbortSearchTick()) return;
        ReleaseEngagementSlot();
        CurrentTarget = attacker.transform;
        OnTargetAcquired(attacker, true);
    }

    /// <summary>
    /// Attempts to make <paramref name="target"/> commit back to <paramref name="me"/>, unless
    /// target is already pursuing <paramref name="me"/>, or target is itself the genuinely
    /// engaged ("main") attacker on its current host — i.e. that host's own CurrentTarget
    /// points back at target (see IsEngaged/CharacterAI.cs). Only a non-reciprocally-engaged
    /// "extra" piled onto the same host is expendable and gets pulled off onto this fresh,
    /// otherwise-unengaged opponent instead — e.g. the "extra" 2nd attacker in a 2v1 should peel
    /// off onto a newly-arrived opponent rather than dog-piling forever, but the genuinely
    /// engaged 1st attacker must never be hijacked away from its host, no matter how many other
    /// attackers are also piled onto that same host.
    /// No-op if target has no combat AI. Called whenever a fighter newly claims an engagement
    /// slot, so a first-come pairing snaps into a mutual, non-crossed bond before other
    /// independent search ticks can produce a scrambled configuration.
    /// </summary>
    internal static void TryForceReciprocalLock(CharacterBase me, CharacterBase target)
    {
        var targetAI = target.GetComponent<CombatAIBase>();
        if (targetAI == null) return;
        if (targetAI.CurrentTarget == me.transform) return;
        if (targetAI.IsEngagedWithHost) return;
        targetAI.ForceReciprocalEngagement(me);
    }

    // ── Immediate retarget ─────────────────────────────────────────────────────

    /// <summary>
    /// Skips the 0.5s tick and scans for a new target right now.
    /// Call this when the current target has just died so the fighter re-engages instantly.
    /// </summary>
    public void ForceTargetSearch()
    {
        RefreshNearbyFighters();
        if (ShouldAbortSearchTick())
        {
            ChangeState(GetDefaultIdleState());
            return;
        }

        CharacterBase best = SelectBestTarget() ?? InheritAllyTarget();
        if (best != null)
        {
            CurrentTarget = best.transform;
            OnTargetAcquired(best, true);
        }
        else
        {
            CurrentTarget = null;
            OnNoTargetFound();
            if (IsInActiveCombatState())
                ChangeState(GetDefaultIdleState());
        }
    }

    // ── Shared helpers ─────────────────────────────────────────────────────────

    public static bool IsTargetDead(Transform t) =>
        t?.GetComponent<TargetHealth>()?.IsDead() ?? true;

    /// <summary>
    /// Closest unequipped "Shield"-tagged pickup within radius, or null. Single shared
    /// implementation for every "go find a shield" need in the codebase — pre-combat prep
    /// (VillagerPrepareCombatState / VillagerAIBase.CanFindShield) and the mid-combat
    /// FindEquipmentState below.
    /// </summary>
    public static GameObject FindNearestDroppedShield(Vector2 position, float radius = 5f)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);
        float closestDist  = Mathf.Infinity;
        GameObject closest = null;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Shield")) continue;
            var item = h.GetComponent<EquipableItem>();
            if (item == null || item.isEquipped) continue;

            float d = Vector2.Distance(position, h.transform.position);
            if (d < closestDist) { closestDist = d; closest = h.gameObject; }
        }

        return closest;
    }

    // ── Mid-combat shield recovery ────────────────────────────────────────────

    private EquipableItem _subscribedShield;

    /// <summary>
    /// Called by ItemAttachment right after Controller.shield is assigned or cleared (EquipShield /
    /// DropShield), so this AI's own OnBroken subscription always tracks whichever shield is
    /// currently equipped — regardless of Awake/Start ordering across sibling components, since
    /// this fires synchronously at the moment of equip rather than being set up once in Start().
    /// </summary>
    public void HandleShieldChanged(EquipableItem newShield)
    {
        if (_subscribedShield == newShield) return;
        if (_subscribedShield != null) _subscribedShield.OnBroken -= HandleShieldBroken;
        _subscribedShield = newShield;
        if (_subscribedShield != null) _subscribedShield.OnBroken += HandleShieldBroken;
    }

    /// <summary>
    /// Fires when the currently-equipped shield's durability hits zero. Interrupts whatever combat
    /// state is active to go find a replacement (FindEquipmentState), unless there's nothing to
    /// interrupt (no target) or the tick is otherwise suppressed (cutscene / shield wall / immature
    /// life stage) — mirrors the same guard OnTargetSearchTick uses.
    /// </summary>
    private void HandleShieldBroken()
    {
        _subscribedShield = null;
        if (CurrentTarget == null) return;
        if (ShouldAbortSearchTick()) return;
        ChangeState(new FindEquipmentState());
    }

    // ── Retarget on hit ────────────────────────────────────────────────────────

    private void HandleHitBy(CharacterBase attacker)
    {
        if (!retargetOnHit || attacker == null) return;
        if (Controller != null && attacker.characterFaction == Controller.characterFaction) return;
        if (attacker.transform == CurrentTarget) return; // already fighting them, no-op

        // A killing blow calls TakeDamage() (which can synchronously run Die() and release all
        // engagement slots) BEFORE the attacker's OnHitTarget goes on to call OnHitBy on us —
        // so a corpse can still reach this point on the very hit that killed it. Without this
        // guard it would claim a brand-new slot on its own killer that nothing ever releases
        // until this GameObject is eventually destroyed (immediate for most enemies via a
        // delayed Destroy, but indefinite for a villager corpse with no Valkyrie configured) —
        // seen as a stray slot gizmo pointing at a dead/destroyed claimer.
        if (IsTargetDead(transform)) return;

        // Always re-enter cleanly via the subclass hook, even if already mid-engagement —
        // previously this only transitioned state when NOT already in active combat, which left
        // an already-fighting character with CurrentTarget silently swapped but its FSM state
        // (and animation listener) still watching the OLD target. Routing through
        // OnTargetAcquired reuses the same CombatApproachState entry as a normal acquisition,
        // which also attempts a reciprocal lock on the attacker (see TryForceReciprocalLock) —
        // so getting hit cleanly hands aggro to whoever's actually landing hits, and the
        // abandoned former target is freed (via ReleaseEngagementSlot) to re-engage someone else
        // rather than being left as a stuck "extra".
        ReleaseEngagementSlot();
        CurrentTarget = attacker.transform;
        OnTargetAcquired(attacker, true);
    }

    // ── Stun ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called from CharacterBase.OnStunned (e.g. self-stun from swinging into a parry).
    /// Overrides whatever combat state is active — a stunned fighter can't attack, block,
    /// or dodge regardless of what the FSM was mid-way through doing.
    /// </summary>
    private void HandleStunned(float duration) => ChangeState(new CombatStunnedState(duration));
}
