using UnityEngine;

public class VillagerWorkState : AIStateBase
{
    private float _timer;
    private float _nextTime;

    public override void OnEnter(CharacterAI ai)
    {
        _timer    = 0f;
        _nextTime = Random.Range(ai.IdleTimeMin * 2f, ai.IdleTimeMax * 2f);
    }

    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;

        if (v.VillagerData == null || v.VillagerData.assignedBuilding == null)
        {
            ai.ChangeState(new VillagerIdleState());
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _nextTime) return;

        _timer    = 0f;
        _nextTime = Random.Range(ai.IdleTimeMin * 2f, ai.IdleTimeMax * 2f);
        v.VillagerController.MoveTo(v.GetRandomPointNearWork());
    }
}
