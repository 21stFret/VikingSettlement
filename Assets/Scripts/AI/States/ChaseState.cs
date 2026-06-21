using UnityEngine;

public class ChaseState : AIStateBase
{
    private float _lostTimer;

    public override void OnEnter(CharacterAI ai)
    {
        _lostTimer = 0f;
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null)
        {
            _lostTimer += Time.deltaTime;
            if (_lostTimer >= ai.LoseTargetTime)
            {
                ai.ReleaseEngagementSlot();
                ai.ChangeState(new SearchState());
            }
            return;
        }

        var villager = ai.CurrentTarget.GetComponent<Villager>();
        if (villager != null && villager.IsDead())
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            ai.ChangeState(new SearchState());
            return;
        }

        if (!ai.PursueTarget)
        {
            ai.ReleaseEngagementSlot();
            ai.ChangeState(new SearchState());
            return;
        }

        float distToTarget = Vector2.Distance(ai.transform.position, ai.CurrentTarget.position);

        if (distToTarget > ai.PursuitRange)
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            ai.ChangeState(new ReturnToSpawnState());
            return;
        }

        // Register fight zone and navigate
        var targetCB = ai.CurrentTarget.GetComponent<CharacterBase>();
        if (targetCB != null)
        {
            ai.RegisterFightZone(targetCB);

            if (ai.UseCombatSlots && FightManager.Instance != null && FightManager.Instance.IsEngaged(targetCB))
            {
                if (ai.CurrentSlotHost != targetCB)
                    TryClaimSlot(ai, targetCB);
            }
            else
            {
                ai.CurrentSlotHost?.ReleaseSlot(ai.Controller);
                ai.CurrentSlotHost = null;
            }
        }

        Vector2 destination = ai.CurrentSlotHost != null
            ? ai.CurrentSlotHost.GetSlotWorldPos(ai.Controller)
            : (Vector2)ai.CurrentTarget.position;

        ai.Controller.SetMoveSpeed(ai.ChaseSpeed);
        ai.Controller.MoveTo(destination);

        // Ally pile-on avoidance — if 2+ same-type allies are already on this target, seek another
        if (ai is EnemyAIBase enemyAI)
        {
            int allyCount = enemyAI.GetNearbyAlliesOfSameType(ai.DetectionRange)
                .FindAll(a => a.CurrentTarget == ai.CurrentTarget).Count;

            if (allyCount >= 2)
            {
                Transform alt = enemyAI.FindNearestTarget();
                if (alt != null && alt != ai.CurrentTarget)
                {
                    ai.ReleaseEngagementSlot();
                    ai.CurrentTarget = alt;
                    return;
                }
            }
        }

        if (distToTarget <= ai.AttackRange)
        {
            ai.Controller.Stop();
            ai.ChangeState(new CombatState());
        }
    }

    private static void TryClaimSlot(CharacterAI ai, CharacterBase target)
    {
        if (target.TryClaimSlot(ai.Controller, out _))
        {
            ai.CurrentSlotHost = target;
            return;
        }

        // All slots on primary target full — try a nearby free target
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            ai.transform.position, ai.DetectionRange, ai.Controller.attackTargetLayer);

        foreach (var col in nearby)
        {
            var cb = col.GetComponent<CharacterBase>();
            if (cb == null || cb == target || cb.characterFaction == Faction.Enemy) continue;
            if (cb.TryClaimSlot(ai.Controller, out _))
            {
                ai.CurrentSlotHost = cb;
                ai.CurrentTarget   = col.transform;
                return;
            }
        }
    }
}
