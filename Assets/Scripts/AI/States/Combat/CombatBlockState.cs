using System;
using UnityEngine;

/// <summary>
/// Raises a block (or dodges) in reaction to a detected attack windup.
/// Waits for the target's attack-recovery event before releasing the block and
/// returning to CombatPressureState.
/// IsActionLocked prevents the search tick from interrupting while blocking.
/// </summary>
public class CombatBlockState : AIStateBase
{
    private Action _onTargetRecovery;

    public override void OnEnter(CharacterAI ai)
    {
        ai.IsActionLocked = true;

        if (ai.CurrentTarget != null)
            ai.Controller.FaceTowards(ai.CurrentTarget.position);
        ai.RefreshEngagementSlot();

        float ratio = ai.CombatStats != null ? ai.CombatStats.BlockVsDodgeRatio : 1f;


        if (UnityEngine.Random.value <= ratio)
        {
            if (ai.Controller.shield == null || !ai.CanBlock)
            {
                // No shield, or guard broken (out of block charges) — the hit gets through.
                ai.Controller.isBlocking = false;
                ai.IsActionLocked = false;
                ai.ChangeState(new CombatPressureState());
                return;
            }
            ai.Controller.isBlocking = true;
            ai.ConsumeBlockCharge();
        }
        else
        {
            // Dodge perpendicular to the incoming attack direction
            Vector2 toTarget = ai.CurrentTarget != null
                ? ((Vector2)ai.CurrentTarget.position - (Vector2)ai.transform.position).normalized
                : Vector2.zero;
            Vector2 dodge = new Vector2(-toTarget.y, toTarget.x);
            ai.Controller.Roll(dodge);
        }

        var listener = ai.AnimListener;
        if (listener != null)
        {
            _onTargetRecovery = () =>
            {
                ai.Controller.isBlocking = false;
                ai.IsActionLocked        = false;
                ai.ChangeState(new CombatPressureState());
            };
            listener.OnAttackRecovery += _onTargetRecovery;
        }
    }

    public override void OnUpdate(CharacterAI ai)
    {
        // Target may die mid-windup, switching straight to a death animation and never firing
        // the StopAttacking event this state is waiting on — without this check the blocker
        // would stay IsActionLocked forever, holding its engagement slot on the corpse.
        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            if (ai is CombatAIBase combat)
                combat.ForceTargetSearch();
            else
                ai.ChangeState(new VillagerIdleState());
        }
    }

    public override void OnExit(CharacterAI ai)
    {
        ai.Controller.isBlocking = false;
        ai.IsActionLocked        = false;

        var listener = ai.AnimListener;
        if (listener != null && _onTargetRecovery != null)
            listener.OnAttackRecovery -= _onTargetRecovery;
        _onTargetRecovery = null;
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
