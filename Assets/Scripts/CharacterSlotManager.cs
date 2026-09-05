using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Owns the engagement-slot state for one CharacterBase: who's attacking it, at what angle,
/// and where each attacker should stand. Extracted from CharacterBase's "Combat Slots" region -
/// logic unchanged, just relocated so it's a plain, independently testable class instead of
/// being wedged into the CharacterBase MonoBehaviour.
/// </summary>
public class CharacterSlotManager
{
    private readonly CharacterBase _owner;
    private readonly List<(CharacterBase claimer, float angle)> _occupiedSlots = new List<(CharacterBase, float)>();

    public CharacterSlotManager(CharacterBase owner)
    {
        _owner = owner;
    }

    public int OccupiedCount => _occupiedSlots.Count;

    /// Read-only view for debug/gizmo drawing (CharacterBase.OnDrawGizmos).
    public IReadOnlyList<(CharacterBase claimer, float angle)> OccupiedSlots => _occupiedSlots;

    public bool TryClaimSlot(CharacterBase claimer, out Vector2 slotWorldPos)
    {
        ReleaseSlot(claimer);

        if (_occupiedSlots.Count >= _owner.MaxAttackers)
        {
            slotWorldPos = Vector2.zero;
            return false;
        }

        float newAngle = CalculateBisectAngle(claimer);
        _occupiedSlots.Add((claimer, newAngle));
        FightManager.Instance.NotifyOccupancyChanged(_owner);

        if (claimer.AI?.showDebug == true)
        {
            Debug.Log($"[{_owner.name}] Slot claimed by {claimer.name} " +
                $"at angle:{newAngle:F1}° " +
                $"existing slots:{_occupiedSlots.Count - 1} " +
                $"existing angles:{string.Join(", ", _occupiedSlots.Where(s => s.claimer != claimer).Select(s => s.angle.ToString("F1")))}");
        }

        slotWorldPos = GetSlotWorldPos(claimer);
        return true;
    }

    private float CalculateBisectAngle(CharacterBase claimer)
    {
        if (_occupiedSlots.Count == 0)
            return ComputeSlotAngleTo(claimer);

        var angles = _occupiedSlots.Select(s => s.angle).OrderBy(a => a).ToList();
        float largestGap = 0f;
        float gapStart = 0f;

        for (int i = 0; i < angles.Count; i++)
        {
            float next = angles[(i + 1) % angles.Count];
            // With a single occupant, (i+1)%count wraps back to the same element, so the
            // generic formula collapses to a 0° gap. The whole circle is actually free
            // (the lone occupant has zero angular width), so treat it as a full 360° gap —
            // this places the second claimant directly opposite the first.
            float gap = angles.Count == 1 ? 360f : (next - angles[i] + 360f) % 360f;
            if (gap > largestGap)
            {
                largestGap = gap;
                gapStart = angles[i];
            }
        }
        var value = (gapStart + largestGap / 2f) % 360f;
        return value;
    }

    /// <summary>
    /// Compass angle (unit-circle degrees, matching GetSlotWorldPos) from the owner
    /// towards claimer's live position, snapped to one of the 4 facing directions.
    /// </summary>
    private float ComputeSlotAngleTo(CharacterBase claimer)
    {
        Vector2 dir = (Vector2)claimer.transform.position - (Vector2)_owner.transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        var facing = CharacterBase.ComputeFacingDirection(dir.normalized);
        var facingVector = CharacterBase.FacingDirectionToVector(facing);
        return Mathf.Atan2(facingVector.y, facingVector.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Re-snaps every occupied slot's angle to the current live layout. The "main" occupant —
    /// whichever claimer the owner is itself reciprocally engaged with (CurrentTarget) — always
    /// tracks its own live bearing from the owner, same as the old single-occupant case. Every
    /// other ("extra") occupant is then defined purely as a fixed angular offset from that live
    /// main angle, evenly spread around the remaining arc, so extras follow along for free as
    /// the engaged pair circles each other, instead of each one independently re-bisecting
    /// against the others' shifting positions (which is what made live-tracking multi-occupant
    /// angles unstable before — see bug-history "second attacker's bisect angle" fix).
    /// </summary>
    public void UpdateSlotAngle(CharacterBase claimer)
    {
        if (_occupiedSlots.Count == 0) return;
        if (!_occupiedSlots.Any(s => s.claimer == claimer)) return;

        int mainIndex = _occupiedSlots.FindIndex(s => s.claimer == _owner.CurrentTarget);

        if (mainIndex < 0)
        {
            // No reciprocally-engaged occupant to anchor extras to yet. A lone occupant still
            // tracks its own live bearing regardless (matches the old single-occupant case);
            // with 2+ occupants and no main yet, leave claim-time angles alone until one of them
            // becomes genuinely engaged.
            if (_occupiedSlots.Count == 1)
                _occupiedSlots[0] = (_occupiedSlots[0].claimer, ComputeSlotAngleTo(_occupiedSlots[0].claimer));
            return;
        }

        float mainAngle = ComputeSlotAngleTo(_occupiedSlots[mainIndex].claimer);
        _occupiedSlots[mainIndex] = (_occupiedSlots[mainIndex].claimer, mainAngle);

        int extraCount = _occupiedSlots.Count - 1;
        if (extraCount <= 0) return;

        float step = 360f / (extraCount + 1);
        int slot = 0;
        for (int i = 0; i < _occupiedSlots.Count; i++)
        {
            if (i == mainIndex) continue;
            slot++;
            float angle = (mainAngle + step * slot) % 360f;
            _occupiedSlots[i] = (_occupiedSlots[i].claimer, angle);
        }
    }

    public Vector2 GetSlotWorldPos(CharacterBase claimer)
    {
        foreach (var slot in _occupiedSlots)
        {
            if (slot.claimer == claimer)
            {
                float rad = slot.angle * Mathf.Deg2Rad;
                return (Vector2)_owner.transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _owner.slotDistance;
            }
        }
        return _owner.transform.position;
    }

    public void ReleaseSlot(CharacterBase claimer)
    {
        _occupiedSlots.RemoveAll(s => s.claimer == claimer);
        // Guarded (unlike TryClaimSlot's call above): reachable from CharacterAI.OnDestroy() via
        // ReleaseEngagementSlot(), which can fire during scene teardown after FightManager's own
        // OnDestroy already ran — Instance would otherwise spin up a fresh one mid-unload.
        if (FightManager.Exists) FightManager.Instance.NotifyOccupancyChanged(_owner);
    }

    public void ReleaseAllSlots()
    {
        _occupiedSlots.Clear();
        if (FightManager.Exists) FightManager.Instance.NotifyOccupancyChanged(_owner);
    }
}
