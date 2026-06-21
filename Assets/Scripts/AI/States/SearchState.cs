using UnityEngine;

public class SearchState : AIStateBase
{
    private float _timer;
    private float _timeout;

    public override void OnEnter(CharacterAI ai)
    {
        ai.Controller?.Stop();
        _timer   = 0f;
        _timeout = Random.Range(ai.IdleTimeMin, ai.IdleTimeMax);
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget != null)
        {
            ai.ChangeState(new ChaseState());
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= _timeout)
            ai.ChangeState(new WanderState());
    }
}
