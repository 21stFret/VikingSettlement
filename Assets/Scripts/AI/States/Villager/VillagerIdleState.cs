using UnityEngine;

public class VillagerIdleState : AIStateBase
{
    private float _timer;
    private float _nextTime;

    public override void OnEnter(CharacterAI ai)
    {
        _timer = 0f;
        _nextTime = Random.Range(ai.IdleTimeMin, ai.IdleTimeMax);
        ai.Controller?.Stop();
    }

    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;

        if (v.IsInRaidMode && v.FollowTarget != null)
        {
            ai.ChangeState(new VillagerFollowState());
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _nextTime) return;

        _timer    = 0f;
        _nextTime = Random.Range(ai.IdleTimeMin, ai.IdleTimeMax);

        if (v.VillagerData != null && v.VillagerData.assignedBuilding != null)
            ai.ChangeState(new VillagerMoveToWorkState());
        else if (v.ShouldWander)
            ai.ChangeState(new VillagerWanderState());
    }
}
