using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global registry of active melee fights. A "fight" is any CharacterBase host currently holding
/// at least one occupied engagement slot (CharacterBase.OccupiedCount > 0). CharacterBase notifies
/// this registry from TryClaimSlot/ReleaseSlot/ReleaseAllSlots whenever occupancy changes, so
/// ActiveHosts always stays in sync without any per-frame scanning of its own.
///
/// This exists so CharacterAI.GetNearbyFightCentres() can source candidates from "characters
/// actually hosting a fight" (small, typically far fewer than the whole scene) instead of
/// "every nearby character" (NearbyFighters, physics-query populated) — previously every observer
/// re-walked and re-filtered the same crowd down to the handful of real fights, every call.
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
}
