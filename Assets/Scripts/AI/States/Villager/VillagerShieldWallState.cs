using UnityEngine;

public class VillagerShieldWallState : AIStateBase
{
    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;
        if (v.FollowTarget == null) return;

        Vector2 slotPos  = (Vector2)v.FollowTarget.position + v.wallFormationOffset;
        float distToSlot = Vector2.Distance(v.transform.position, slotPos);

        if (distToSlot > 0.4f)
        {
            v.VillagerController.isBlocking = false;
            v.VillagerController.MoveTo(slotPos);
        }
        else
        {
            v.VillagerController.isBlocking =
                v.VillagerController.shield != null && !v.VillagerController.shield.IsBroken;

            if (distToSlot > 0.1f)
                v.VillagerController.MoveTo(slotPos);
            else
                v.VillagerController.Stop();
        }
    }
}
