using UnityEngine;

public class VillagerWanderState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        if (!WanderToRandomPoint((VillagerAIBase)ai))
            ai.ChangeState(new VillagerIdleState());
    }

    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;
        if (!v.VillagerController.ReturnIsMoving())
            ai.ChangeState(new VillagerIdleState());
    }

    private bool WanderToRandomPoint(VillagerAIBase v)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 dir  = Random.insideUnitCircle.normalized;
            float   dist = Random.Range(2f, v.WanderRadius);
            Vector2 pt   = (Vector2)v.transform.position + dir * dist;

            if (v.VillageCentre != null
                && Vector2.Distance(pt, v.VillageCentre.position) > v.MaxDistanceFromCentre)
                continue;

            if (!v.IsPointWalkable(pt)) continue;

            if (Physics2D.Raycast(v.transform.position, dir, dist, v.VillagerController.obstacleLayer).collider != null)
                continue;

            v.VillagerController.MoveTo(pt);
            return true;
        }
        return false;
    }
}
