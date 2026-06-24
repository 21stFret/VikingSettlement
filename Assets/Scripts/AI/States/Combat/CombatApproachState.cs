using UnityEngine;

/// <summary>
/// Closes on the target and claims a melee slot. If all slots are full (MaxAttackers cap),
/// orbits at ThreatCircleDistance instead. Transitions to CombatPressureState on arrival.
/// </summary>
public class CombatApproachState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        var target = ai.CurrentTarget?.GetComponent<CharacterBase>();
        if (target == null) return;

        ai.AnimListener?.SetTarget(target);

        // Release any slot held on a previous target, then claim on the new one.
        // If slots are full the orbit loop in OnUpdate will retry each frame.
        if (ai.CurrentSlotHost != target)
            ai.ReleaseEngagementSlot();

        if (ai.CurrentSlotHost == null && target.TryClaimSlot(ai.Controller, out _))
            ai.CurrentSlotHost = target;
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

        // Slot was full at OnEnter — keep retrying without releasing
        if (ai.CurrentSlotHost == null)
        {
            if (!target.TryClaimSlot(ai.Controller, out _))
            {
                OrbitTarget(ai, target);
                return;
            }
            ai.CurrentSlotHost = target;
        }

        float dist = Vector2.Distance(ai.transform.position, ai.CurrentTarget.position);

        Vector2 slotPos = ai.CurrentSlotHost.GetSlotWorldPos(ai.Controller);

        // Within weapon range — separation hysteresis is handled by _fightPushState
        // inside CalculateSeparationForce; no second hysteresis layer needed here.
        if (dist <= ai.AttackRange)
        {
            var fights = ai.GetNearbyFightCentres();
            Vector2 sep = ai.CalculateSeparationForce(fights);

            bool beingPushed = sep.magnitude > 0.01f;

            if (beingPushed)
            {
                ai.Controller.MoveTo(
                    (Vector2)ai.transform.position +
                    sep.normalized * ai.MoveSpeed * Time.deltaTime * 10f);
                return;
            }

            ai.Controller.Stop();
            ai.ChangeState(new CombatPressureState());
            return;
        }

        ai.MoveWithSeparation(slotPos);
    }

    public override void OnExit(CharacterAI ai) => ai.Controller.Stop();

    private void OrbitTarget(CharacterAI ai, CharacterBase target)
    {
        float radius = ai.CombatStats != null ? ai.CombatStats.ThreatCircleDistance : 2.5f;
        Vector2 dir  = ((Vector2)ai.transform.position - (Vector2)target.transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        ai.Controller.MoveTo((Vector2)target.transform.position + dir * radius);
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
