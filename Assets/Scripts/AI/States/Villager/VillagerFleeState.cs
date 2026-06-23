using UnityEngine;

public class VillagerFleeState : AIStateBase
{
    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;

        if (ai.CurrentTarget == null)
        {
            ai.ChangeState(v.IsInRaidMode ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
            return;
        }

        var enemy = ai.CurrentTarget.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead())
        {
            ai.CurrentTarget = null;
            ai.ChangeState(v.IsInRaidMode ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
            return;
        }

        Vector2 away      = ((Vector2)v.transform.position - (Vector2)ai.CurrentTarget.position).normalized;
        Vector2 fleePoint = (Vector2)v.transform.position + away * v.WanderRadius;

        RaycastHit2D hit = Physics2D.Raycast(v.transform.position, away, v.WanderRadius, v.movementLayerMask);
        if (hit.collider != null)
        {
            Vector2 left  = new Vector2(-away.y, away.x);
            Vector2 right = new Vector2(away.y, -away.x);

            if (!Physics2D.Raycast(v.transform.position, left, v.WanderRadius, v.movementLayerMask))
                fleePoint = (Vector2)v.transform.position + left * v.WanderRadius;
            else if (!Physics2D.Raycast(v.transform.position, right, v.WanderRadius, v.movementLayerMask))
                fleePoint = (Vector2)v.transform.position + right * v.WanderRadius;
            else
                fleePoint = hit.point - away * 0.5f;
        }

        v.VillagerController.MoveTo(fleePoint);

        if (Vector2.Distance(v.transform.position, ai.CurrentTarget.position) > ai.DetectionRange * 2f)
        {
            ai.CurrentTarget = null;
            ai.ChangeState(v.IsInRaidMode ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
        }
    }
}
