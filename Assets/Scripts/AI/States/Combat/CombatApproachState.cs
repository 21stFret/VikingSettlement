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
        if (target != null)
            ai.AnimListener?.SetTarget(target);
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            ai.ChangeState(new VillagerIdleState());
            return;
        }

        var target = ai.CurrentTarget.GetComponent<CharacterBase>();
        if (target == null) return;

        float dist = Vector2.Distance(ai.transform.position, ai.CurrentTarget.position);

        // Register fight zone once
        if (ai.RegisteredTarget != target)
            ai.RegisterFightZone(target);

        // Claim a slot if we don't have one yet
        if (ai.CurrentSlotHost == null)
        {
            if (target.TryClaimSlot(ai.Controller, out _))
                ai.CurrentSlotHost = target;
            else
            {
                // Slots full — orbit at threat circle distance
                OrbitTarget(ai, target);
                return;
            }
        }

        // Move toward the claimed slot position
        Vector2 slotPos = ai.CurrentSlotHost.GetSlotWorldPos(ai.Controller);
        float slotDist  = Vector2.Distance(ai.transform.position, slotPos);

        if (slotDist > 0.3f)
            ai.Controller.MoveTo(slotPos);
        else if (dist <= ai.AttackRange)
        {
            ai.Controller.Stop();
            ai.ChangeState(new CombatPressureState());
        }
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
