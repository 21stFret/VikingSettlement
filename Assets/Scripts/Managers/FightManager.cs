using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps fights spatially separated by assigning each pair a fixed zone center.
/// When two characters commit to each other, RegisterPair() returns a midpoint
/// that is guaranteed to be at least separationRadius away from every other
/// active fight zone. Both fighters then navigate toward that point, so fights
/// drift apart naturally rather than piling on the same spot.
/// Add one instance to each combat scene.
/// </summary>
public class FightManager : MonoBehaviour
{
    public static FightManager Instance { get; private set; }

    [Tooltip("Minimum world-unit distance between any two fight zone centers")]
    [SerializeField] private float separationRadius = 4f;

    private class FightZone
    {
        public CharacterBase fighter1;
        public CharacterBase fighter2;
        public Vector2 center;

        public bool IsValid =>
            fighter1 != null && fighter2 != null &&
            !IsDeadOrNull(fighter1) && !IsDeadOrNull(fighter2);

        public bool Contains(CharacterBase c) => fighter1 == c || fighter2 == c;
    }

    private readonly List<FightZone> _zones = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        _zones.RemoveAll(z => !z.IsValid);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Register or retrieve the zone center for this pair.
    /// If the pair is already registered (in either order) the existing center is
    /// returned unchanged — so both fighters independently calling this see the
    /// same point. If the computed midpoint clashes with an existing zone it is
    /// pushed away until clear.
    /// </summary>
    public Vector2 RegisterPair(CharacterBase a, CharacterBase b)
    {
        if (a == null || b == null) return Vector2.zero;

        // Return existing zone if this pair is already registered (either order)
        foreach (var z in _zones)
        {
            if ((z.fighter1 == a && z.fighter2 == b) ||
                (z.fighter1 == b && z.fighter2 == a))
                return z.center;
        }

        // Release any zone that contains either fighter (they switched opponents)
        _zones.RemoveAll(z => z.Contains(a) || z.Contains(b));

        // Zone center starts at the midpoint between the two fighters
        Vector2 center = ((Vector2)a.transform.position + (Vector2)b.transform.position) * 0.5f;

        // Push center away from every existing zone until it is clear
        center = FindClearCenter(center);

        _zones.Add(new FightZone { fighter1 = a, fighter2 = b, center = center });
        return center;
    }

    /// <summary>Remove this character from any zone it belongs to.</summary>
    public void Unregister(CharacterBase fighter)
    {
        if (fighter == null) return;
        _zones.RemoveAll(z => z.Contains(fighter));
    }

    /// <summary>True if this character is already committed to a fight zone.</summary>
    public bool IsEngaged(CharacterBase character)
    {
        if (character == null) return false;
        foreach (var z in _zones)
            if (z.IsValid && z.Contains(character)) return true;
        return false;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private Vector2 FindClearCenter(Vector2 proposed)
    {
        // Iterate until the proposed center no longer conflicts with any zone,
        // or until we run out of attempts (handles degenerate cases)
        for (int attempt = 0; attempt < 16; attempt++)
        {
            bool clear = true;
            foreach (var zone in _zones)
            {
                if (!zone.IsValid) continue;
                float dist = Vector2.Distance(proposed, zone.center);
                if (dist < separationRadius)
                {
                    Vector2 away = (proposed - zone.center);
                    if (away.sqrMagnitude < 0.001f)
                        away = Random.insideUnitCircle.normalized;
                    else
                        away = away.normalized;
                    proposed = zone.center + away * separationRadius;
                    clear = false;
                    break; // restart the outer loop with the adjusted position
                }
            }
            if (clear) break;
        }
        return proposed;
    }

    private static bool IsDeadOrNull(CharacterBase cb)
    {
        if (cb == null) return true;
        var h = cb.GetComponent<TargetHealth>();
        return h != null && h.IsDead();
    }
}
