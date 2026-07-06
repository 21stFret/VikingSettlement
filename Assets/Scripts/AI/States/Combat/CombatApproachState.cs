using UnityEngine;

/// <summary>
/// Closes on the target and claims a melee slot. If all slots are full (MaxAttackers cap),
/// orbits at ThreatCircleDistance instead. Transitions to CombatPressureState on arrival.
/// </summary>
public class CombatApproachState : AIStateBase
{
    private const float SeparationClearThreshold = 0.15f;
    private const float ForceCommitTimeout = 4f; // safety valve if the slot itself sits inside another fight's zone

    private float _timeInState;
    private bool  _arrivedAtSlot;

    public override void OnEnter(CharacterAI ai)
    {
        _timeInState = 0f;
        _arrivedAtSlot = false;

        var target = ai.CurrentTarget?.GetComponent<CharacterBase>();
        if (target == null) return;

        ai.AnimListener?.SetTarget(target);

        // Release any slot held on a previous target, then claim on the new one.
        // If slots are full the orbit loop in OnUpdate will retry each frame.
        if (ai.CurrentSlotHost != target)
            ai.ReleaseEngagementSlot();

        if (ai.CurrentSlotHost == null && target.TryClaimSlot(ai.Controller, out _))
        {
            ai.CurrentSlotHost = target;
            CombatAIBase.TryForceReciprocalLock(ai.Controller, target);
        }
    }

    public override void OnUpdate(CharacterAI ai)
    {
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

        var target = ai.CurrentTarget.GetComponent<CharacterBase>();
        if (target == null) return;

        ai.RefreshEngagementSlot();

        // Slot was full at OnEnter — keep retrying without releasing
        if (ai.CurrentSlotHost == null)
        {
            if (!target.TryClaimSlot(ai.Controller, out _))
            {
                OrbitTarget(ai, target);
                return;
            }
            ai.CurrentSlotHost = target;
            CombatAIBase.TryForceReciprocalLock(ai.Controller, target);
        }

        _timeInState += Time.deltaTime;

        Vector2 slotPos    = ai.CurrentSlotHost.GetSlotWorldPos(ai.Controller);

        if (!_arrivedAtSlot)
        {
            float distToSlot = Vector2.Distance((Vector2)ai.transform.position, slotPos);
            if (distToSlot > ai.commitRange)
            {
                // Still travelling to reach our own assigned slot — not genuinely "locked in" to
                // this engagement yet, so go straight there and ignore other fights entirely.
                // Blending in a push-away force while walking toward an already-correct destination
                // just fights the straight-line pull and produces jitter. Host-body arc-around
                // avoidance (not clipping through our own target) still applies regardless.
                ai.MoveWithSeparation(slotPos, avoidOtherFights: false);
                return;
            }
            // Arrived — latch this so a push-away nudge below can never flip distToSlot back
            // over commitRange and re-trigger the full-strength pull-back branch above. Without
            // this latch, "closeEnough" was re-derived from raw distance every frame: the push
            // (below) could shove the fighter just past commitRange, which flipped straight back
            // to the zero-push branch above, snapping it back in one frame, which re-triggered
            // the push again next frame — a hard, boundary-crossing oscillation every frame
            // (commitRange is only 0.4 units, well within a single frame's movement step).
            _arrivedAtSlot = true;
        }

        // Arrived at the slot — we ARE locked in now. Commit unless a DIFFERENT fight's zone
        // still overlaps this exact position; ForceCommitTimeout guarantees this doesn't stall
        // forever if the slot itself sits inside another fight's zone.
        bool clearOfFights = ai.CalculateSeparationForce().magnitude < SeparationClearThreshold;
        if (clearOfFights || _timeInState >= ForceCommitTimeout)
        {
            ai.Controller.Stop();
            ai.ChangeState(new CombatPressureState());
            return;
        }

        // Genuinely locked in but crowded by another fight — nudge away instead of committing.
        ai.MoveWithSeparation(slotPos, avoidOtherFights: true);
    }

    public override void OnExit(CharacterAI ai) => ai.Controller.Stop();

    private void OrbitTarget(CharacterAI ai, CharacterBase target)
    {
        float radius = ai.CombatStats != null ? ai.CombatStats.ThreatCircleDistance : 2.5f;
        Vector2 dir  = ((Vector2)ai.transform.position - (Vector2)target.transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        // Not locked into any engagement yet (slot was full) — same rule as the travelling
        // branch above: go straight to the orbit point, ignore other fights.
        ai.MoveWithSeparation((Vector2)target.transform.position + dir * radius, avoidOtherFights: false);
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
