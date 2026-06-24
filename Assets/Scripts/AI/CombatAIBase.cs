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
        if (ShouldAbortSearchTick()) return;

        // Living primary target and not an extra attacker — stay locked
        if (CurrentTarget != null && !IsTargetDead(CurrentTarget) && !IsExtra) return;

        // Current target just died — find an immediate replacement
        if (CurrentTarget != null && IsTargetDead(CurrentTarget))
        {
            ReleaseEngagementSlot();
            CharacterBase replacement = SelectBestTarget() ?? InheritAllyTarget();
            CurrentTarget = replacement?.transform;
            if (CurrentTarget == null)
                ChangeState(GetDefaultIdleState());
            return;
        }

        // No current target, or we are an extra — search for the best available
        CharacterBase best = SelectBestTarget() ?? InheritAllyTarget();

        if (best != null)
        {
            bool targetChanged = best.transform != CurrentTarget;
            CurrentTarget = best.transform;
            if (targetChanged || !IsInActiveCombatState())
                OnTargetAcquired(best, targetChanged);
        }
        else if (CurrentTarget == null || IsTargetDead(CurrentTarget))
        {
            CurrentTarget = null;
            OnNoTargetFound();
        }
        // else: SelectBestTarget returned null but CurrentTarget is still alive — keep fighting
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
