using System;
using UnityEngine;

/// <summary>
/// Ranged-combat analogue of CombatApproachState + CombatPressureState combined into one state,
/// since ranged fighters have no melee slot to travel to and commit to — there's no equivalent of
/// "arrived, now hold" as a separate physical event, just a distance band to manage every frame:
///   - Beyond AttackRange: close the distance, don't fire yet.
///   - Inside CombatFighterStats.MinEngageRange: retreat straight away from the target (the
///     "flee"/kite behaviour) — but keep firing while doing so (strafe & shoot), not just once
///     safe again.
///   - Otherwise: hold position.
/// Reused for both CombatAIBase.GetApproachState() and GetPressureState() on ranged fighters (see
/// ArcherAI) — there's nothing to differentiate between "just acquired" and "settled" here the way
/// melee's slot-arrival latch does.
///
/// Reacts to the target's attack windup with CombatBlockState unconditionally (not gated behind
/// CombatFighterStats.AdvancedBlocking) — this is the ranged fighter's "holding" state, analogous
/// to CombatPressureState which reacts unconditionally too, not CombatApproachState/Recovering's
/// AdvancedBlocking-gated reaction.
/// </summary>
public class RangedCombatState : AIStateBase
{
    private CombatAnimationListener _listener;
    private Action _onWindup;

    public override void OnEnter(CharacterAI ai)
    {
        var target = ai.CurrentTarget?.GetComponent<CharacterBase>();
        if (target == null) return;

        ai.AnimListener?.SetTarget(target);
        ai.Controller.FaceTowards(ai.CurrentTarget.position);

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

        var target = ai.CurrentTarget.GetComponent<CharacterBase>();
        if (target == null) return;

        ai.Controller.FaceTowards(ai.CurrentTarget.position);

        float dist     = Vector2.Distance(ai.transform.position, target.transform.position);
        float minRange = ai.CombatStats != null ? ai.CombatStats.MinEngageRange : 0f;

        if (dist > ai.AttackRange)
        {
            // Still closing distance — not in range to fire yet.
            ai.MoveWithSeparation(target.transform.position, avoidOtherFights: false);
            return;
        }

        if (dist < minRange)
        {
            // Too close — back straight away from the target (the flee/kite behaviour).
            Vector2 away = (Vector2)ai.transform.position - (Vector2)target.transform.position;
            ai.MoveAlongPush(away);
        }
        else
        {
            ai.Controller.Stop();
        }

        // Strafe & shoot: fire from wherever we're currently standing, whether holding or
        // retreating — don't wait until back outside minRange to resume shooting.
        if (!ai.IsActionLocked && ai.Controller.CanAttack())
            ai.ChangeState(new CombatAttackState());
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
