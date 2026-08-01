using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local waypoint planner: casts toward the target, and if blocked, sweeps outward in fixed-degree
/// steps (alternating left/right) until a direction is fully clear, jumps past the obstacle along
/// that direction (sized off its own world-space bounds), then re-aims at the real target and
/// repeats. Capped at maxNodes. Computes two candidate routes (prefer-left, prefer-right) once at
/// the first blocked ray and keeps whichever totals a shorter distance, so a real detour comparison
/// happens without branching at every subsequent obstacle.
/// </summary>
public static class RaySweepPathfinder
{
    private const float SweepStepDegrees     = 5f;
    // Fallback-only now that corner targeting (see TryCornerNode) handles the common case — no
    // longer needs the wider range that was tried while the fallback was doing all the work.
    private const float MaxSweepAngleDegrees = 180f;
    private const float NodeJumpMargin       = 0.25f;

    private struct BranchResult
    {
        public List<Vector2> Nodes;
        public float TotalDistance;
        public bool ReachedTarget;
    }

    /// <param name="arrivalSlop">
    /// The caller's waypoint-arrival tolerance (how far short of a waypoint it's willing to call
    /// "arrived" and advance to the next one). Every clearance margin below is padded by this —
    /// otherwise a character that advances to the next leg while still up to arrivalSlop short of
    /// a corner-clearing waypoint can be sent straight back into the same corner it just supposedly
    /// rounded, since the next leg's straight line is only validated from the exact waypoint
    /// position, not from "anywhere within arrivalSlop of it".
    /// </param>
    public static bool TryFindPath(
        Vector2 start, Vector2 target, LayerMask obstacleLayer, float bodyRadius,
        out List<Vector2> path, int maxNodes = 10, GameObject self = null, float arrivalSlop = 0f)
    {
        if (IsClearLine(start, target, obstacleLayer, bodyRadius, self, out _))
        {
            path = new List<Vector2> { target };
            return true;
        }

        BranchResult left  = BuildBranch(start, target, obstacleLayer, bodyRadius, self, maxNodes, preferSign: -1, arrivalSlop: arrivalSlop);
        BranchResult right = BuildBranch(start, target, obstacleLayer, bodyRadius, self, maxNodes, preferSign: +1, arrivalSlop: arrivalSlop);

        bool leftHas  = left.Nodes.Count  > 0;
        bool rightHas = right.Nodes.Count > 0;

        BranchResult best;
        if (!leftHas && !rightHas)
        {
            path = new List<Vector2>();
            return false;
        }
        else if (leftHas != rightHas)
        {
            best = leftHas ? left : right;
        }
        else if (left.ReachedTarget != right.ReachedTarget)
        {
            // A branch that actually reaches target always wins over one that merely dead-ended
            // shorter — comparing raw summed distance alone would otherwise let a short,
            // incomplete detour beat a longer detour that actually gets there.
            best = left.ReachedTarget ? left : right;
        }
        else
        {
            best = left.TotalDistance <= right.TotalDistance ? left : right;
        }

        path = best.Nodes;
        return best.ReachedTarget;
    }

    private static BranchResult BuildBranch(Vector2 start, Vector2 target, LayerMask obstacleLayer,
        float bodyRadius, GameObject self, int maxNodes, int preferSign, float arrivalSlop)
    {
        var nodes = new List<Vector2>();
        float totalDist = 0f;
        Vector2 current = start;

        for (int i = 0; i < maxNodes; i++)
        {
            if (IsClearLine(current, target, obstacleLayer, bodyRadius, self, out RaycastHit2D hit))
            {
                nodes.Add(target);
                totalDist += Vector2.Distance(current, target);
                return new BranchResult { Nodes = nodes, TotalDistance = totalDist, ReachedTarget = true };
            }

            if (!TryFindSweepNode(current, target, hit.collider, obstacleLayer, bodyRadius, self, preferSign, arrivalSlop, out Vector2 next))
            {
                // Genuinely stuck — no viable direction at this node. Return whatever progress
                // was made; the branch stops here rather than looping fruitlessly.
                return new BranchResult { Nodes = nodes, TotalDistance = totalDist, ReachedTarget = false };
            }

            totalDist += Vector2.Distance(current, next);
            nodes.Add(next);
            current = next;
        }

        // Hit the node cap without a final clear line to target.
        return new BranchResult { Nodes = nodes, TotalDistance = totalDist, ReachedTarget = false };
    }

    /// <summary>
    /// Finds the next node to route around `blocker`, preferring preferSign's side. Tries the
    /// obstacle's actual silhouette corner first (one hop around most convex obstacles); falls
    /// back to an angle sweep only when that isn't viable.
    /// </summary>
    private static bool TryFindSweepNode(Vector2 from, Vector2 target, Collider2D blocker,
        LayerMask obstacleLayer, float bodyRadius, GameObject self, int preferSign, float arrivalSlop, out Vector2 node)
    {
        if (TryCornerNode(from, target, blocker, bodyRadius, obstacleLayer, self, preferSign, arrivalSlop, out node))
            return true;

        // Corner targeting can miss for non-box colliders (bounds is just the AABB, so a rotated
        // or irregular collider's real corner isn't necessarily at an AABB corner) or when the
        // offset corner point is itself blocked by something else. Fall back to an angle sweep —
        // first at a jump distance sized off the obstacle's own bounds (so a big obstacle still
        // gets one big hop instead of degrading to tiny steps), then at a minimal body-width step
        // as a last resort.
        Vector2 blockedDir = (target - from).normalized;
        float jumpDistance = ComputeJumpDistance(blocker, bodyRadius, arrivalSlop);
        if (TrySweepAtDistance(from, blockedDir, jumpDistance, obstacleLayer, bodyRadius, self, preferSign, out node))
            return true;

        float minStep = bodyRadius * 2f + NodeJumpMargin + arrivalSlop;
        return TrySweepAtDistance(from, blockedDir, minStep, obstacleLayer, bodyRadius, self, preferSign, out node);
    }

    /// <summary>
    /// Aims directly at the obstacle's silhouette corner — the one furthest to preferSign's side
    /// of the straight-line direction to target, found via the collider's world-space bounds —
    /// offset outward by body radius + margin + arrivalSlop so the character actually clears it
    /// even in the worst case (the caller calling this node "arrived" while still arrivalSlop short
    /// of it). This is what gives "one node per corner" instead of many small hops crawling along
    /// the obstacle's edge.
    /// </summary>
    private static bool TryCornerNode(Vector2 from, Vector2 target, Collider2D blocker, float bodyRadius,
        LayerMask obstacleLayer, GameObject self, int preferSign, float arrivalSlop, out Vector2 node)
    {
        Bounds bounds = blocker.bounds;
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;
        Vector2 center = bounds.center;

        Vector2[] corners =
        {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
        };

        Vector2 blockedDir = (target - from).normalized;

        // The two silhouette corners as seen from `from` — furthest to either side of the
        // straight-line direction to target — are where a route actually needs to swing around,
        // not just any corner of the box. Signed angle relative to blockedDir picks them out
        // directly (negative/positive matches Rotate()'s sign convention used by the sweep
        // fallback, so preferSign selects the same side consistently either way).
        float minAngle = float.MaxValue, maxAngle = float.MinValue;
        int negIdx = -1, posIdx = -1;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 toCorner = corners[i] - from;
            if (toCorner.sqrMagnitude < 0.0001f) continue;
            float angle = Vector2.SignedAngle(blockedDir, toCorner);
            if (angle < minAngle) { minAngle = angle; negIdx = i; }
            if (angle > maxAngle) { maxAngle = angle; posIdx = i; }
        }
        if (negIdx == -1 || posIdx == -1) { node = default; return false; }

        Vector2 cornerPos = corners[preferSign < 0 ? negIdx : posIdx];

        Vector2 outward = cornerPos - center;
        outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : blockedDir;
        Vector2 candidate = cornerPos + outward * (bodyRadius + NodeJumpMargin + arrivalSlop);

        if (IsClearLine(from, candidate, obstacleLayer, bodyRadius, self, out _))
        {
            node = candidate;
            return true;
        }

        node = default;
        return false;
    }

    /// <summary>
    /// Jump distance sized off the blocking collider's world-space AABB, not a fixed step: the
    /// enclosing-circle radius of the obstacle's bounds (its half-diagonal, bounds.extents.magnitude)
    /// scales naturally with obstacle size — a rock gets a small jump, a building gets a big one.
    /// </summary>
    private static float ComputeJumpDistance(Collider2D blocker, float bodyRadius, float arrivalSlop)
    {
        float boundingRadius = blocker.bounds.extents.magnitude;
        return boundingRadius + bodyRadius + NodeJumpMargin + arrivalSlop;
    }

    private static bool TrySweepAtDistance(Vector2 from, Vector2 blockedDir, float distance,
        LayerMask obstacleLayer, float bodyRadius, GameObject self, int preferSign, out Vector2 node)
    {
        for (float angle = SweepStepDegrees; angle <= MaxSweepAngleDegrees; angle += SweepStepDegrees)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                int sign = pass == 0 ? preferSign : -preferSign;
                Vector2 dir = Rotate(blockedDir, sign * angle);
                Vector2 candidate = from + dir * distance;

                // Same CircleCast serves both purposes required: confirming the angle is "fully
                // clear of any obstacle" over the jump distance, AND verifying (at bodyRadius) that
                // the character can actually fit along it — no need for two casts.
                if (IsClearLine(from, candidate, obstacleLayer, bodyRadius, self, out _))
                {
                    node = candidate;
                    return true;
                }
            }
        }
        node = default;
        return false;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Reused across calls to avoid a per-cast array allocation. Physics2D calls only ever happen
    // on Unity's main thread, so a plain static (not per-thread) buffer is safe here.
    private const int MaxHitsPerCast = 16;
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[MaxHitsPerCast];
    private static ContactFilter2D _contactFilter = new ContactFilter2D { useTriggers = false };

    private static bool IsClearLine(Vector2 from, Vector2 to, LayerMask obstacleLayer,
        float bodyRadius, GameObject self, out RaycastHit2D hit)
    {
        Vector2 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.0001f) { hit = default; return true; }

        Vector2 dir = delta / dist;

        // A plain CircleCast only reports the single closest hit — if that happens to be a
        // trigger volume (e.g. an interaction zone) sitting in front of the real, solid collider,
        // discarding it as "ignore triggers" would wrongly read the whole line as clear and never
        // see the actual obstacle right behind it. Cast for every overlapping collider instead and
        // pick the closest one that isn't ourselves (useTriggers=false already excludes triggers).
        _contactFilter.SetLayerMask(obstacleLayer);
        int count = Physics2D.CircleCast(from, bodyRadius, dir, _contactFilter, _hitBuffer, dist);

        hit = default;
        float closestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D candidate = _hitBuffer[i];
            if (candidate.collider == null) continue;
            if (self != null && candidate.collider.gameObject == self) continue;

            if (candidate.distance < closestDist)
            {
                closestDist = candidate.distance;
                hit = candidate;
            }
        }

        return hit.collider == null;
    }
}
