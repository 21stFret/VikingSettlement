using UnityEngine;

public class CombatState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        ai.Controller?.Stop();
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null)
        {
            ai.ReleaseEngagementSlot();
            ai.ChangeState(new SearchState());
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

        float distance = Vector2.Distance(ai.transform.position, ai.CurrentTarget.position);
        if (distance > ai.AttackRange)
        {
            ai.ChangeState(new ChaseState());
            return;
        }

        // Re-register fight zone if the target slot changed (spawned-inside-range case)
        var targetCB = ai.CurrentTarget.GetComponent<CharacterBase>();
        if (targetCB != null && targetCB != ai.RegisteredTarget)
            ai.RegisterFightZone(targetCB);

        ai.Controller.FaceTowards(ai.CurrentTarget.position);

        if (!ai.Controller.IsAttacking())
            ai.Controller.Attack();
    }
}
