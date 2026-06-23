public class VillagerAI : VillagerAIBase
{
    protected override AIStateBase GetInitialState() => new VillagerIdleState();
}
