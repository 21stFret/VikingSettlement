using UnityEngine;

public class VillagerFollowState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        ai.Controller.FacingOverride = Vector2.zero;
    }

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

        // Obstacle avoidance for the path to targetPos is handled by CharacterBase.MoveToTarget —
        // don't duplicate it here with a second raycast/side-step pass, which used a different
        // layer mask and could pick a different dodge side than the base-class pass on the same
        // frame.
        v.VillagerController.MoveTo(targetPos);
    }
}
