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

        // Already holding a slot on someone — genuinely engaged, stay in that fight.
        // (A target's CurrentTarget field can only name one attacker back, so it can't be used
        // to detect "is this fighter already engaged" once 2+ attackers legitimately share a
        // target — CurrentSlotHost is the real signal. See bug history 2026-07-04.)
        if (CurrentSlotHost != null) return;

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
        => ChangeState(new CombatApproachState());

    /// <summary>Called when no target exists and no living current target remains.</summary>
    protected virtual void OnNoTargetFound() { }

    /// <summary>Idle/passive state to fall back to when combat ends.</summary>
    protected virtual AIStateBase GetDefaultIdleState() => new IdleState();

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
            var allyAI = ally.GetComponent<CharacterAI>();
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
               CurrentState is CombatRetreatState         ||
               CurrentState is VillagerPrepareCombatState;
    }

    // ── Reciprocal engagement ───────────────────────────────────────────────────

    /// <summary>
    /// Forces this fighter to immediately commit to <paramref name="attacker"/>, via the same
    /// subclass-specific OnTargetAcquired hook the normal search tick uses — so villager
    /// flee-checks, ranged positioning, and shield-prep still apply. Does NOT construct
    /// CombatApproachState directly, since a low-HP villager should flee instead of engaging.
    /// Releases any slot held on a previous target up front — OnTargetAcquired can route
    /// somewhere other than CombatApproachState (flee, ranged positioning, shield-prep),
    /// which wouldn't otherwise clear the stale claim on the old host.
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
    /// target is already pursuing <paramref name="me"/>, or is the SOLE attacker engaged in its
    /// current fight (peeling it off would leave that host completely unattended — the genuine
    /// 1v1 case must never be hijacked). If target is one of several attackers piled onto the
    /// same host (its current host's OccupiedCount > 1), it's expendable there and gets pulled
    /// off onto this fresh, otherwise-unengaged opponent instead — e.g. the "extra" 2nd attacker
    /// in a 2v1 should peel off onto a newly-arrived opponent rather than dog-piling forever.
    /// No-op if target has no combat AI. Called whenever a fighter newly claims an engagement
    /// slot, so a first-come pairing snaps into a mutual, non-crossed bond before other
    /// independent search ticks can produce a scrambled configuration.
    /// </summary>
    internal static void TryForceReciprocalLock(CharacterBase me, CharacterBase target)
    {
        var targetAI = target.GetComponent<CombatAIBase>();
        if (targetAI == null) return;
        if (targetAI.CurrentTarget == me.transform) return;
        if (targetAI.CurrentSlotHost != null && targetAI.CurrentSlotHost.OccupiedCount <= 1) return;
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

    // ── Retarget on hit ────────────────────────────────────────────────────────

    private void HandleHitBy(CharacterBase attacker)
    {
        if (!retargetOnHit || attacker == null) return;
        if (Controller != null && attacker.characterFaction == Controller.characterFaction) return;
        if (attacker.transform == CurrentTarget) return; // already fighting them, no-op

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
