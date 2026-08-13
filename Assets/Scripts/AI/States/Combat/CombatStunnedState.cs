using UnityEngine;

/// <summary>
/// Entered when CharacterBase.ApplyStun fires (e.g. swinging into a parry). Does nothing
/// but wait out the stun window — no attacking, blocking, or dodging — then hands off to
/// CombatRecoveringState. CharacterBase already zeroed isBlocking/isParrying and immobilized
/// movement when the stun started; this just keeps the FSM from reacting while that holds.
/// </summary>
public class CombatStunnedState : AIStateBase
{
    private readonly float _duration;
    private float _timer;

    public CombatStunnedState(float duration) => _duration = duration;

    public override void OnEnter(CharacterAI ai)
    {
        _timer = 0f;
        ai.IsActionLocked = true;
        ai.Controller.Stop();
    }

    public override void OnUpdate(CharacterAI ai)
    {
        _timer += Time.deltaTime;
        if (_timer < _duration) return;

        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            if (ai is CombatAIBase combat)
                combat.ForceTargetSearch();
            else
                ai.ChangeState(new VillagerIdleState());
            return;
        }

        ai.ChangeState(new CombatRecoveringState());
    }

    public override void OnExit(CharacterAI ai) => ai.IsActionLocked = false;

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
