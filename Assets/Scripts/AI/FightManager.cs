using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global registry of active melee fights. A "fight" is any CharacterBase host currently holding
/// at least one occupied engagement slot (CharacterBase.OccupiedCount > 0). CharacterBase notifies
/// this registry from TryClaimSlot/ReleaseSlot/ReleaseAllSlots whenever occupancy changes, so
/// ActiveHosts always stays in sync without any per-frame scanning of its own.
///
/// Beyond just holding ActiveHosts, this also owns the shared work every observer used to redo
/// independently in CharacterAI.GetNearbyFightCentres(): mutual-duel dedup (observer-independent —
/// the same answer for everyone, see GetCanonicalFights) is computed exactly once per frame here,
/// and each observer's own filtered view (GetFightsFor) is memoized per-observer-per-frame instead
/// of recomputed from scratch on every call. CharacterAI.GetNearbyFightCentres() is now a pure
/// lookup into GetFightsFor — it does no filtering itself.
///
/// Self-instantiating (like a classic lazy singleton) rather than requiring a scene-placed
/// GameObject — it holds no configuration, just runtime state, so there's nothing to wire up in
/// the Inspector. Scene-local: not DontDestroyOnLoad, same as GameTickManager.
/// </summary>
public class FightManager : MonoBehaviour
{
    private static FightManager _instance;
    public static FightManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(FightManager));
                _instance = go.AddComponent<FightManager>();
            }
            return _instance;
        }
    }

    /// <summary>True if an instance already exists, without triggering lazy creation — use this
    /// (not Instance) from teardown paths like OnDestroy, so a character dying/unloading after
    /// FightManager's own OnDestroy has already run doesn't spin up a fresh one mid-teardown.</summary>
    public static bool Exists => _instance != null;

    private readonly List<CharacterBase> _activeHosts = new List<CharacterBase>();
    public IReadOnlyList<CharacterBase> ActiveHosts => _activeHosts;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// Syncs host's membership in the registry with its live OccupiedCount. Idempotent — safe to
    /// call any time occupancy might have changed, even if it turns out it didn't.
    /// </summary>
    public void NotifyOccupancyChanged(CharacterBase host)
    {
        bool shouldBeActive = host.OccupiedCount > 0;
        bool isActive = _activeHosts.Contains(host);

        if (shouldBeActive && !isActive)
            _activeHosts.Add(host);
        else if (!shouldBeActive && isActive)
            _activeHosts.Remove(host);
    }

    // ── Shared per-frame fight computation ──────────────────────────────────────────

    private readonly List<CharacterBase> _canonicalFights = new List<CharacterBase>();
    private int _canonicalFrame = -1;

    /// <summary>
    /// The deduped set of active fight hosts, rebuilt at most once per frame regardless of how
    /// many observers ask this frame. Contains the observer-INdependent filtering only — anything
    /// that would give the same answer no matter who's asking (dead/stale-entry defensiveness,
    /// mutual-duel collapse). Observer-dependent filtering (own fight, faction, distance, pile-on)
    /// happens per-observer in GetFightsFor below.
    /// </summary>
    private List<CharacterBase> GetCanonicalFights()
    {
        if (Time.frameCount == _canonicalFrame) return _canonicalFights;
        _canonicalFrame = Time.frameCount;
        _canonicalFights.Clear();

        foreach (var fighter in _activeHosts)
        {
            if (fighter == null) continue;
            if (fighter.OccupiedCount <= 0) continue; // stale entry mid-transition; skip defensively

            // Collapse mutual 1-1 duels (both sides host each other's slot) into one entry —
            // otherwise a symmetric duel counts twice (once per host) and pushes observers
            // twice as hard as a one-sided N-attacker fight. Observer-independent: the same pair
            // resolves to the same single survivor no matter who's asking, since the comparison
            // only references the two duelists themselves.
            var mutualPartner = fighter.CurrentTarget;
            if (mutualPartner != null && mutualPartner.CurrentTarget == fighter
                && mutualPartner.OccupiedCount > 0
                && fighter.GetHashCode() > mutualPartner.GetHashCode())
                continue;

            _canonicalFights.Add(fighter);
        }

        return _canonicalFights;
    }

    private class FighterView
    {
        public readonly List<CharacterAI.NearbyFight> Fights = new List<CharacterAI.NearbyFight>();
        public int Frame = -1;
    }

    private readonly Dictionary<CharacterAI, FighterView> _views = new Dictionary<CharacterAI, FighterView>();

    /// <summary>
    /// observer's nearby active fights (excluding its own), memoized per-observer-per-frame — the
    /// backing implementation for CharacterAI.GetNearbyFightCentres(). Filters the shared
    /// canonical fight list (deduped once above) by the checks that DO depend on who's asking.
    /// </summary>
    public List<CharacterAI.NearbyFight> GetFightsFor(CharacterAI observer)
    {
        var canonical = GetCanonicalFights();

        if (!_views.TryGetValue(observer, out var view))
        {
            view = new FighterView();
            _views[observer] = view;
        }
        if (view.Frame == Time.frameCount) return view.Fights;
        view.Frame = Time.frameCount;
        view.Fights.Clear();

        var controller = observer.Controller;
        if (controller == null) return view.Fights;

        foreach (var fighter in canonical)
        {
            if (fighter == controller) continue;              // my own fight (I'm the host)
            if (fighter == observer.CurrentSlotHost) continue; // my own fight (I'm an attacker in it)
            if (fighter.characterFaction == controller.characterFaction) continue;

            float dist = Vector2.Distance(observer.transform.position, fighter.transform.position);
            if (dist > observer.awarenessRadius) continue;

            // fighter is also attacking MY host (e.g. it's the reciprocally-engaged main
            // attacker while I'm the "extra" piling onto the same target) — that's the same
            // fight I'm joining, not a rival one. Without this, a fresh attacker approaching a
            // pile-on target perceives its own host's other attacker as a separate fight to
            // avoid — and since that attacker stands right next to the shared host, the
            // avoidance zone covers the very slot position it's trying to reach, producing a
            // tug-of-war that only resolves once CombatApproachState's ForceCommitTimeout forces
            // it to commit anyway.
            if (observer.CurrentSlotHost != null
                && fighter.AI?.CurrentSlotHost == observer.CurrentSlotHost)
                continue;

            view.Fights.Add(new CharacterAI.NearbyFight { Host = fighter, Centre = fighter.transform.position });
        }

        return view.Fights;
    }

    /// <summary>Drops observer's memoized view — call from CharacterAI.OnDestroy() to avoid the
    /// dictionary quietly accumulating entries for destroyed characters.</summary>
    public void RemoveFighterView(CharacterAI observer) => _views.Remove(observer);
}
