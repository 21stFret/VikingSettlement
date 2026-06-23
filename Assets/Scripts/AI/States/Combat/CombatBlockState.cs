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

        float ratio = ai.CombatStats != null ? ai.CombatStats.BlockVsDodgeRatio : 1f;
        if (UnityEngine.Random.value <= ratio)
        {
            ai.Controller.isBlocking = true;
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

    public override void OnUpdate(CharacterAI ai) { }

    public override void OnExit(CharacterAI ai)
    {
        ai.Controller.isBlocking = false;
        ai.IsActionLocked        = false;

        var listener = ai.AnimListener;
        if (listener != null && _onTargetRecovery != null)
            listener.OnAttackRecovery -= _onTargetRecovery;
        _onTargetRecovery = null;
    }
}
