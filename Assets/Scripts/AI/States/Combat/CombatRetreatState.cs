using UnityEngine;

/// <summary>
/// Steps backward away from the target, releases the engagement slot, then re-approaches.
/// Gives the AI a breathing-room moment between attack sequences.
/// </summary>
public class CombatRetreatState : AIStateBase
{
    private Vector2 _retreatPos;
    private float   _timer;
    private const float MaxRetreatDuration = 2f;

    public override void OnEnter(CharacterAI ai)
    {
        _timer = 0f;
        ai.ReleaseEngagementSlot();

        float retreatDist = ai.CombatStats != null ? ai.CombatStats.RetreatDistance : 3f;
        Vector2 away = ai.CurrentTarget != null
            ? ((Vector2)ai.transform.position - (Vector2)ai.CurrentTarget.position).normalized
            : Vector2.down;
        if (away == Vector2.zero) away = Vector2.down;

        _retreatPos = (Vector2)ai.transform.position + away * retreatDist;
        ai.Controller.MoveTo(_retreatPos);
    }

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.CurrentTarget = null;
            if (ai is CombatAIBase combat)
                combat.ForceTargetSearch();
            else
                ai.ChangeState(new VillagerIdleState());
            return;
        }

        if (ai.CurrentTarget != null)
        {
            Vector2 toTarget = ((Vector2)ai.CurrentTarget.position - (Vector2)ai.transform.position).normalized;
            ai.Controller.FacingOverride = toTarget;
        }

        _timer += Time.deltaTime;
        float distToRetreat = Vector2.Distance(ai.transform.position, _retreatPos);
        if (distToRetreat < 0.3f || _timer >= MaxRetreatDuration)
            ai.ChangeState(new CombatApproachState());
    }

    public override void OnExit(CharacterAI ai)
    {
        ai.Controller.FacingOverride = null;
        ai.Controller.Stop();
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
