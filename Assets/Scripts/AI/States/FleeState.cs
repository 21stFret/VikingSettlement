/// <summary>
/// Stub for flee behaviour. Used as a foundation for villager and future enemy flee logic.
/// Implement direction calculation and obstacle avoidance here when needed.
/// </summary>
public class FleeState : AIStateBase
{
    public override void OnEnter(CharacterAI ai)
    {
        ai.Controller?.Stop();
    }

    public override void OnUpdate(CharacterAI ai)
    {
        // Not yet implemented — fall back to searching
        ai.ChangeState(new SearchState());
    }
}
