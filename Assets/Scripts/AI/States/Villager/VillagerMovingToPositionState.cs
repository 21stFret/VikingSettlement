public class VillagerMovingToPositionState : AIStateBase
{
    public override void OnUpdate(CharacterAI ai)
    {
        // Movement was started by SetCutsceneTarget(). This state waits until
        // ClearCutsceneTarget() is called externally to return to normal AI.
    }
}
