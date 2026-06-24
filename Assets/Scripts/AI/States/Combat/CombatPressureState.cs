using System;
using UnityEngine;

/// <summary>
/// Holds attack range while watching the target for attack windup.
/// Reacts to target windup with CombatBlockState.
/// Commits to CombatAttackState when BOTH gates clear simultaneously:
///   - AI decision gate: PressureTime elapsed + aggression roll passes
///   - Physical gate:    CharacterBase.CanAttack() (attackDelay cleared)
/// High aggression + slow weapon = waits for the weapon. Fast weapon + low aggression = weapon
/// ready but AI holds back for a better moment.
/// </summary>
public class CombatPressureState : AIStateBase
{
    private CombatAnimationListener _listener;
    private Action _onWindup;
    private Action _onTargetRecovery;
    private float _timer;

    public override void OnEnter(CharacterAI ai)
    {
        _timer    = 0f;
        _listener = ai.AnimListener;

        var target = ai.CurrentTarget?.GetComponent<CharacterBase>();
        if (_listener != null && target != null)
        {
            _listener.SetTarget(target);

            _onWindup = () =>
            {
                if (!ai.IsActionLocked) ai.ChangeState(new CombatBlockState());
            };
            _onTargetRecovery = () =>
            {
                if (!ai.IsActionLocked && ai.Controller.CanAttack())
                    ai.ChangeState(new CombatAttackState());
            };

            _listener.OnWindup        += _onWindup;
            _listener.OnAttackRecovery += _onTargetRecovery;
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

        float dist = Vector2.Distance(ai.transform.position, ai.CurrentTarget.position);
        if (dist > ai.AttackRange * 1.5f)
        {
            if (ai.CalculateSeparationForce().magnitude >= 0.1f) return;

            ai.ChangeState(new CombatApproachState());
            return;
        }

        float pressureTime = ai.CombatStats != null ? ai.CombatStats.PressureTime : 1.5f;
        float aggression   = ai.CombatStats != null ? ai.CombatStats.AggressionLevel : 0.5f;
        _timer += Time.deltaTime;

        bool aiDecisionReady = _timer >= pressureTime && UnityEngine.Random.value < aggression;
        bool weaponReady     = ai.Controller.CanAttack();

        if (aiDecisionReady && weaponReady)
            ai.ChangeState(new CombatAttackState());
    }

    public override void OnExit(CharacterAI ai)
    {
        if (_listener != null)
        {
            if (_onWindup         != null) _listener.OnWindup         -= _onWindup;
            if (_onTargetRecovery != null) _listener.OnAttackRecovery -= _onTargetRecovery;
        }
        _onWindup         = null;
        _onTargetRecovery = null;
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
