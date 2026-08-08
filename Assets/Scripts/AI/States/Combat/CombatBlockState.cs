using System;
using UnityEngine;

/// <summary>
/// Raises a block (or dodges) in reaction to a detected attack windup. Reachable from
/// CombatPressureState always, and from CombatApproachState/CombatRecoveringState when
/// CombatFighterStats.AdvancedBlocking is set (more alert/skilled warriors).
/// Waits for the target's attack-recovery event before releasing the block and resuming
/// whichever state is appropriate (CombatPressureState if a slot is already held, otherwise
/// CombatApproachState).
/// IsActionLocked prevents the search tick from interrupting while blocking.
/// </summary>
public class CombatBlockState : AIStateBase
{
    private Action _onTargetRecovery;

    /// <summary>Where to resume after the block/dodge resolves — Pressure if already slotted in,
    /// otherwise Approach (mid-approach reactions can't have secured a slot yet). Routed through
    /// CombatAIBase's GetApproachState()/GetPressureState() hooks so ranged fighters (which never
    /// hold a slot — see ArcherAI) resume ranged positioning instead of melee approach.</summary>
    private static AIStateBase ResumeState(CharacterAI ai)
    {
        if (ai is CombatAIBase combat)
            return combat.CurrentSlotHost != null ? combat.GetPressureState() : combat.GetApproachState();
        return ai.CurrentSlotHost != null ? new CombatPressureState() : new CombatApproachState();
    }

    public override void OnEnter(CharacterAI ai)
    {
        ai.IsActionLocked = true;

        if (ai.CurrentTarget != null)
            ai.Controller.FaceTowards(ai.CurrentTarget.position);
        ai.RefreshEngagementSlot();

        // Decide which reactions are actually viable right now (shield+charges for block,
        // dodge-ability+off-cooldown for dodge), then only roll BlockVsDodgeRatio between
        // options that are genuinely available — never "attempt" one that can't work.
        bool canBlockNow = ai.CanBlockNow;
        bool canDodgeNow = ai.CanDodgeNow;

        if (!canBlockNow && !canDodgeNow)
        {
            // The calling state already gates on this, so this shouldn't normally be reachable —
            // kept as a safety net in case state changes between the windup event and OnEnter.
            ai.Controller.isBlocking = false;
            ai.IsActionLocked = false;
            ai.ChangeState(ResumeState(ai));
            return;
        }

        float ratio = ai.CombatStats != null ? ai.CombatStats.BlockVsDodgeRatio : 1f;
        bool attemptBlock = canBlockNow && (!canDodgeNow || UnityEngine.Random.value <= ratio);

        if (attemptBlock)
        {
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
                ai.ChangeState(ResumeState(ai));
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
