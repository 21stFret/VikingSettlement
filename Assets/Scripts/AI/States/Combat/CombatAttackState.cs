using System;
using UnityEngine;

/// <summary>
/// Triggers an attack and waits for the character's own StopAttacking animation event
/// before transitioning to CombatRecoveringState.
/// IsActionLocked prevents the search tick from interrupting mid-swing.
/// </summary>
public class CombatAttackState : AIStateBase
{
    private Action _onSelfRecovery;

    public override void OnEnter(CharacterAI ai)
    {
        ai.IsActionLocked = true;

        if (ai.CurrentTarget != null)
            ai.Controller.FaceTowards(ai.CurrentTarget.position);
        ai.RefreshEngagementSlot();

        _onSelfRecovery = () => ai.ChangeState(new CombatRecoveringState());
        ai.Controller.OnAttackRecoveryEvent += _onSelfRecovery;

        ai.Controller.Attack();
    }

    public override void OnUpdate(CharacterAI ai) 
    {
        Vector2 slotPos = ai.CurrentSlotHost.GetSlotWorldPos(ai.Controller);
        ai.MoveWithSeparation(slotPos, avoidOtherFights: true); 
    }

    public override void OnExit(CharacterAI ai)
    {
        if (_onSelfRecovery != null)
            ai.Controller.OnAttackRecoveryEvent -= _onSelfRecovery;
        _onSelfRecovery   = null;
        ai.IsActionLocked = false;
    }
}
