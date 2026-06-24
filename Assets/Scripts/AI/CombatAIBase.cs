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
            Controller.OnHitByAttacker += HandleHitBy;
        base.Start();
    }

    private void OnDestroy()
    {
        if (Controller != null)
            Controller.OnHitByAttacker -= HandleHitBy;
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

        // No target at all — find one fresh (must come before IsExtra check)
        if (CurrentTarget == null)
        {
            var best = NearbyEnemies
                .Where(e => e.GetComponent<TargetHealth>()?.IsDead() != true)
                .OrderBy(e => e.OccupiedCount)
                .ThenBy(e => Vector2.Distance(transform.position, e.transform.position))
                .FirstOrDefault();

            if (best != null)
            {
                CurrentTarget = best.transform;
                OnTargetAcquired(best, true);
            }
            // No enemies found → stay idle
            return;
        }

        // Have a target — stay locked if we are the primary
        if (!IsExtra) return;

        // IsExtra — only switch for a completely free target
        var freeTarget = NearbyEnemies
            .Where(e => e.GetComponent<TargetHealth>()?.IsDead() != true)
            .Where(e => e.OccupiedCount == 0)
            .OrderBy(e => Vector2.Distance(transform.position, e.transform.position))
            .FirstOrDefault();

        if (freeTarget != null && freeTarget.transform != CurrentTarget)
        {
            ReleaseEngagementSlot();
            CurrentTarget = freeTarget.transform;
            ChangeState(new CombatApproachState());
        }

        // No free target exists → stay on current target even if IsExtra
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
               CurrentState is CombatRecoveringState      ||
               CurrentState is CombatRetreatState         ||
               CurrentState is VillagerPrepareCombatState;
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

        CharacterBase best = SelectBestTarget();
        if (best == null || best.transform == CurrentTarget) return;

        ReleaseEngagementSlot();
        CurrentTarget = best.transform;
        if (!IsInActiveCombatState())
            ChangeState(new CombatApproachState());
    }
}
