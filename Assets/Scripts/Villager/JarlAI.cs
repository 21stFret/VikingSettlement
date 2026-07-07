using UnityEngine;

public class JarlAI : VillagerAIBase
{
    protected override AIStateBase GetInitialState() => new VillagerIdleState();
}
