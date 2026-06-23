using UnityEngine;

/// <summary>
/// Brief cooldown after an attack. Returns to CombatPressureState or CombatRetreatState
/// based on aggression level.
/// </summary>
public class CombatRecoveringState : AIStateBase
{
    private float _timer;
    private float _duration;

    public override void OnEnter(CharacterAI ai)
    {
        _timer    = 0f;
        _duration = ai.CombatStats != null ? ai.CombatStats.RecoveryTime : 0.8f;
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            ai.ChangeState(new VillagerIdleState());
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _duration) return;

        float aggression = ai.CombatStats != null ? ai.CombatStats.AggressionLevel : 0.5f;
        ai.ChangeState(UnityEngine.Random.value > aggression
            ? (AIStateBase)new CombatRetreatState()
            : new CombatPressureState());
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
