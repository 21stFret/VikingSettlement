using UnityEngine;

public class WanderState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        if (!TryWander(ai))
            ai.ChangeState(new IdleState());
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget != null)
        {
            ai.ChangeState(ai is CombatAIBase combat ? combat.GetApproachState() : new CombatApproachState());
            return;
        }

        if (!ai.Controller.ReturnIsMoving())
            ai.ChangeState(new IdleState());
    }

    private bool TryWander(CharacterAI ai)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 dir      = Random.insideUnitCircle.normalized;
            float   distance = Random.Range(1f, ai.WanderRadius);
            Vector2 target   = ai.SpawnPoint + dir * distance;

            if (!ai.IsPointWalkable(target)) continue;

            if (ai.ObstacleLayer != 0)
            {
                var hit = Physics2D.Raycast(ai.transform.position, dir, distance, ai.ObstacleLayer);
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != ai.gameObject)
                    continue;
            }

            ai.Controller.SetMoveSpeed(ai.MoveSpeed);
            ai.Controller.MoveTo(target);
            return true;
        }

        return false;
    }
}
