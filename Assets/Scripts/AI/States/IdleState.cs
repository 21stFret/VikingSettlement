using UnityEngine;

public class IdleState : AIStateBase
{
    private float _timer;
    private float _duration;

    public override void OnEnter(CharacterAI ai)
    {
        ai.Controller?.Stop();
        _timer    = 0f;
        _duration = Random.Range(ai.IdleTimeMin, ai.IdleTimeMax);
        ai.Controller.FacingOverride = Vector2.zero;
    }

    public override void OnUpdate(CharacterAI ai)
    {
        _timer += Time.deltaTime;
        if (_timer < _duration) return;

        if (ai.CurrentTarget != null)
            ai.ChangeState(ai is CombatAIBase combat ? combat.GetApproachState() : new CombatApproachState());
        else
            ai.ChangeState(new WanderState());
    }
}
