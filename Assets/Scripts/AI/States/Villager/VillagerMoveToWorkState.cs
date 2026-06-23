public class VillagerMoveToWorkState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;
        if (v.WorkLocation != null)
            v.VillagerController.MoveTo(v.GetRandomPointNearWork());
        else
            ai.ChangeState(new VillagerIdleState());
    }

    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;
        if (!v.VillagerController.ReturnIsMoving())
            ai.ChangeState(new VillagerWorkState());
    }
}
