using UnityEngine;

public class VillagerFollowState : AIStateBase
{
    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;

        if (v.FollowTarget == null)
        {
            ai.ChangeState(new VillagerIdleState());
            return;
        }

        float dist = Vector2.Distance(v.transform.position, v.FollowTarget.position);
        if (dist <= v.FollowDistance)
        {
            if (v.VillagerController.ReturnIsMoving())
                v.VillagerController.Stop();
            return;
        }

        Vector2 dir       = ((Vector2)v.FollowTarget.position - (Vector2)v.transform.position).normalized;
        Vector2 targetPos = (Vector2)v.FollowTarget.position - dir * (v.FollowDistance * 0.5f);
        targetPos += v.GetSeparationForce() * v.SeparationStrength;

        float distToTarget = Vector2.Distance(v.transform.position, targetPos);
        if (Physics2D.Raycast(v.transform.position, dir, distToTarget, v.movementLayerMask).collider == null)
        {
            v.VillagerController.MoveTo(targetPos);
        }
        else
        {
            Vector2 left = new Vector2(-dir.y, dir.x);
            if (!Physics2D.Raycast(v.transform.position, left, 2f, v.movementLayerMask))
                v.VillagerController.MoveTo((Vector2)v.transform.position + left * 2f);
            else
                v.VillagerController.MoveTo((Vector2)v.transform.position + new Vector2(dir.y, -dir.x) * 2f);
        }
    }
}
