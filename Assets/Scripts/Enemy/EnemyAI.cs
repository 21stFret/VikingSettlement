/// <summary>
/// Standard enemy AI. Extends EnemyAIBase with no additional states.
/// Class name is preserved so all existing prefab and scene component references remain valid.
///
/// To create a new enemy type with unique states, extend EnemyAIBase directly:
///   public class BerserkerAI : EnemyAIBase { ... }
/// </summary>
public class EnemyAI : EnemyAIBase
{
    protected override AIStateBase GetInitialState() => new IdleState();

    public string GetCurrentState() => CurrentState?.GetType().Name ?? "null";
}
