using System;
using UnityEngine;

/// <summary>
/// Brief cooldown after an attack, then back to CombatPressureState.
/// </summary>
public class CombatRecoveringState : AIStateBase
{
    private float _timer;
    private float _duration;

    private CombatAnimationListener _listener;
    private Action _onWindup;

    public override void OnEnter(CharacterAI ai)
    {
        _timer    = 0f;
        _duration = ai.CombatStats != null ? ai.CombatStats.RecoveryTime : 0.8f;

        // Advanced blockers can react to a windup even while catching their breath post-swing,
        // not just once settled back into CombatPressureState. Gated per-class, mirrors
        // CombatApproachState's identical wiring.
        if (ai.CombatStats != null && ai.CombatStats.AdvancedBlocking)
        {
            _listener = ai.AnimListener;
            if (_listener != null)
            {
                _onWindup = () =>
                {
                    if (!ai.IsActionLocked && (ai.CanBlockNow || ai.CanDodgeNow))
                        ai.ChangeState(new CombatBlockState());
                };
                _listener.OnWindup += _onWindup;
            }
        }
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            if (ai is CombatAIBase combat)
                combat.ForceTargetSearch();
            else
                ai.ChangeState(new VillagerIdleState());
            return;
        }

        ai.Controller.FaceTowards(ai.CurrentTarget.position);
        ai.RefreshEngagementSlot();

        _timer += Time.deltaTime;
        if (_timer < _duration) return;

        ai.ChangeState(new CombatPressureState());
    }

    public override void OnExit(CharacterAI ai)
    {
        if (_listener != null && _onWindup != null)
            _listener.OnWindup -= _onWindup;
        _listener = null;
        _onWindup = null;
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
