using UnityEngine;

/// <summary>
/// Closes on the target and claims a melee slot. If all slots are full (MaxAttackers cap),
/// orbits at ThreatCircleDistance instead. Transitions to CombatPressureState on arrival.
/// </summary>
public class CombatApproachState : AIStateBase
{
    private bool    _arrivedAtSlot;
    private float   _holdTimer;
    private Vector2 _giveWayDir;

    public override void OnEnter(CharacterAI ai)
    {
        _arrivedAtSlot = false;
        _holdTimer     = 0f;
        _giveWayDir    = Vector2.zero;

        var target = ai.CurrentTarget?.GetComponent<CharacterBase>();
        if (target == null) return;

        ai.AnimListener?.SetTarget(target);

        // CombatAIBase.OnCurrentTargetChanged already claimed (or attempted to claim) the slot
        // the instant this target was committed to — CurrentSlotHost is already resolved here,
        // or correctly still null because the target was at capacity (OnUpdate's orbit-and-retry
        // loop below is the fallback for that case).
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

        Vector2 slotPos    = ai.CurrentSlotHost.GetSlotWorldPos(ai.Controller);

        if (!_arrivedAtSlot)
        {
            float distToSlot = Vector2.Distance((Vector2)ai.transform.position, slotPos);
            if (distToSlot > ai.commitRange)
            {
                // The target itself may still be getting shoved around by a DIFFERENT nearby
                // fight's separation push. Closing distance (or even just holding still) onto a
                // target that isn't holding still risks a physical collider overlap — standing
                // still can itself sit in the target's only way out. Give way instead: back off
                // using the same push-away math the target itself is using against that fight,
                // computed from OUR position — if we're close enough to actually be part of the
                // problem this produces a real push and we retreat; if we're genuinely uninvolved
                // it comes out ~zero and we just wait. This check must run unconditionally every
                // frame during travel (not gated behind our own separation status) — our own
                // force naturally rises as we approach a target near the crowding fight, so
                // gating on it here would bypass this exactly when it matters most.
                var targetAI = target.GetComponent<CharacterAI>();
                if (targetAI != null && !targetAI.NoNearbyFights)
                {
                    if (_holdTimer <= 0f)
                        _giveWayDir = ai.CalculateSeparationForce(targetAI.GetNearbyFightCentres());

                    _holdTimer += Time.deltaTime;
                    if (_holdTimer < ai.holdTimeout)
                    {
                        if (_giveWayDir.sqrMagnitude > 0.0001f)
                            ai.MoveWithSeparation((Vector2)ai.transform.position + _giveWayDir, avoidOtherFights: false);
                        else
                            ai.Controller.Stop();
                        return;
                    }
                    // Timed out — target has been stuck too long; proceed anyway rather than
                    // deadlocking forever.
                }
                else
                {
                    _holdTimer = 0f;
                }

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
        // still overlaps this exact position, or the target itself is still being shoved around
        // by one; ForceCommitTimeout guarantees this doesn't stall forever if the slot itself
        // sits inside another fight's zone. Must mirror CombatPressureState's stay condition
        // (self AND target clear) — otherwise Approach hands off to Pressure the instant it's
        // self-clear, Pressure immediately bounces back over the target-crowded half of its own
        // check, and the two states ping-pong every frame for as long as the target stays near
        // any other fight's zone.
        if (ai.NoNearbyFights && ai.CurrentTarget.GetComponent<CharacterAI>().NoNearbyFights)
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
        Vector2 toMe = (Vector2)ai.transform.position - (Vector2)target.transform.position;
        float currentDist = toMe.magnitude;

        // Deadzone, same purpose as _arrivedAtSlot above: this method reruns every single frame
        // for as long as the target stays at capacity (permanently, if MaxAttackers is already
        // full with more attackers than that — e.g. a 3rd attacker on a 2-slot target orbits
        // forever). The destination is derived from the fighter's OWN current bearing from the
        // target, recomputed fresh every frame with no stabilization — any small movement shifts
        // the bearing, which shifts the destination, which re-aims the movement command, with
        // nothing damping that loop. Once already close to the ideal radius, stop re-deriving a
        // moving destination from a moving position entirely, rather than trying to smooth a
        // point that's still chasing itself.
        if (Mathf.Abs(currentDist - radius) < ai.commitRange) return;

        Vector2 dir = currentDist > 0.0001f ? toMe / currentDist : Vector2.right;
        // Not locked into any engagement yet (slot was full) — same rule as the travelling
        // branch above: go straight to the orbit point, ignore other fights.
        ai.MoveWithSeparation((Vector2)target.transform.position + dir * radius, avoidOtherFights: false);
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
