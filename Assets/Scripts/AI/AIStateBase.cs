/// <summary>
/// Base class for all character AI states. Pure C# — no MonoBehaviour overhead.
/// States are stateless where possible; all config and references come from the CharacterAI owner.
/// </summary>
public abstract class AIStateBase
{
    public virtual  void OnEnter(CharacterAI ai) { }
    public abstract void OnUpdate(CharacterAI ai);
    public virtual  void OnExit(CharacterAI ai)  { }
}
