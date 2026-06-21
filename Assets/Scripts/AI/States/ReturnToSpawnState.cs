using UnityEngine;

public class ReturnToSpawnState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        ai.Controller.SetMoveSpeed(ai.MoveSpeed);
        ai.Controller.MoveTo(ai.SpawnPoint);
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget != null)
        {
            ai.ChangeState(new ChaseState());
            return;
        }

        if (Vector2.Distance(ai.transform.position, ai.SpawnPoint) <= 1f)
        {
            ai.Controller.Stop();
            ai.ChangeState(new IdleState());
        }
    }
}
