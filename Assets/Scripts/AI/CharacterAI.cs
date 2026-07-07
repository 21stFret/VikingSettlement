using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Abstract base for all character AI. Drives the FSM via a periodic search tick and
/// per-frame state update. Subclasses expose config via virtual properties and override
/// OnTargetSearchTick() to supply targets.
///
/// Concrete hierarchy:
///   CharacterAI  (this)
///   ├── EnemyAIBase   (enemy target finding, ally detection)
///   │   └── EnemyAI   (standard enemy — keeps class name for Unity prefab compat)
///   └── VillagerAIBase (villager work/combat/raid logic — every villager is a combatant)
///       ├── VillagerAI  (standard villager)
///       └── JarlAI      (same combat behaviour, distinct only where JarlAI overrides apply)
/// </summary>
public abstract class CharacterAI : MonoBehaviour
{
    // ── Config — virtual so concrete subclasses can back them with [SerializeField] ──

    public virtual float      AttackRange    => 1.5f;
    public virtual float      MoveSpeed      => 2f;
    public virtual float      ChaseSpeed     => 3f;
    public virtual float      WanderRadius   => 5f;
    public virtual float      IdleTimeMin    => 1f;
    public virtual float      IdleTimeMax    => 3f;
    public virtual float      PursuitRange   => 15f;
    public virtual float      LoseTargetTime => 3f;
    public virtual LayerMask  ObstacleLayer  => default;

    [Header("AI Core")]
    [SerializeField] private float searchInterval = 0.5f;

    [Header("Reactive Combat")]
    [SerializeField] public CombatType CombatStyle = CombatType.Melee;
    [SerializeField] public CombatFighterStats CombatStats;

    // ── References ─────────────────────────────────────────────────────────────────

    public CharacterBase Controller { get; protected set; }
    public Vector2       SpawnPoint { get; protected set; }

    private Transform _currentTarget;
    public Transform CurrentTarget
    {
        get => _currentTarget;
        set
        {
            _currentTarget = value;
            if (Controller != null)
                Controller.CurrentTarget = value?.GetComponent<CharacterBase>();
            if (value != null) LastTargetInRangeTime = Time.time;
        }
    }

    /// <summary>Timestamp of the last time CurrentTarget was within PursuitRange (or newly
    /// assigned) — CombatAIBase.OnTargetSearchTick uses this to give up a target that's fled
    /// beyond PursuitRange for longer than LoseTargetTime.</summary>
    public float LastTargetInRangeTime { get; protected set; }

    // ── FSM ────────────────────────────────────────────────────────────────────────

    public AIStateBase CurrentState { get; private set; }

    private bool  _aiEnabled = true;
    private float _searchTimer;

    // ── Reactive combat ────────────────────────────────────────────────────────────

    public bool IsActionLocked { get; set; }
    public List<CharacterBase> NearbyAllies   { get; } = new List<CharacterBase>();
    public List<CharacterBase> NearbyEnemies  { get; } = new List<CharacterBase>();
    public List<CharacterBase> NearbyFighters { get; } = new List<CharacterBase>();

    private CombatAnimationListener _animListener;
    public CombatAnimationListener AnimListener
    {
        get { if (_animListener == null) _animListener = GetComponent<CombatAnimationListener>(); return _animListener; }
    }

    // ── Engagement (slot system) ────────────────────────────────────────────────────

    public CharacterBase CurrentSlotHost { get; set; }

    // ── Reactive block economy ──────────────────────────────────────────────────
    // Charges limit how many times CombatBlockState can raise a reactive block before
    // the guard breaks and needs to recover — the mechanism that lets a player eventually
    // break through a heavily-blocking enemy.

    private int _blockCharges = -1; // -1 = not yet initialised from CombatStats
    private float _blockCooldownTimer;
    private bool _blockOnCooldown;

    private int MaxBlockCharges => CombatStats != null ? CombatStats.MaxBlockCharges : 1;

    /// <summary>Fired with the current charge count whenever a charge is spent or restored — drives personal-UI block pips.</summary>
    public event Action<int> OnBlockChargesChanged;

    /// <summary>Override to scale block-charge recovery time (e.g. by combat skill).</summary>
    protected virtual float GetEffectiveBlockCooldown() => CombatStats != null ? CombatStats.BlockCooldown : 5f;

    /// <summary>True if a reactive block charge is available right now.</summary>
    public bool CanBlock => (_blockCharges < 0 ? MaxBlockCharges : _blockCharges) > 0;

    /// <summary>Spends one reactive-block charge; starts the recovery cooldown once depleted.</summary>
    public void ConsumeBlockCharge()
    {
        if (_blockCharges < 0) _blockCharges = MaxBlockCharges;
        _blockCharges = Mathf.Max(0, _blockCharges - 1);
        OnBlockChargesChanged?.Invoke(_blockCharges);

        if (_blockCharges == 0 && !_blockOnCooldown)
        {
            _blockOnCooldown    = true;
            _blockCooldownTimer = GetEffectiveBlockCooldown();
        }
    }

    private void TickBlockCooldown()
    {
        if (!_blockOnCooldown) return;

        _blockCooldownTimer -= Time.deltaTime;
        if (_blockCooldownTimer <= 0f)
        {
            _blockOnCooldown = false;
            _blockCharges    = MaxBlockCharges;
            OnBlockChargesChanged?.Invoke(_blockCharges);
        }
    }

    // ── Spatial AI ─────────────────────────────────────────────────────────────────

    [Header("Spatial AI")]
    public float awarenessRadius      = 8.0f;
    public float enterSeparationRange = 3.0f;
    public float exitSeparationRange  = 4.0f;
    public float commitRange          = 0.4f;
    public float separationForceStrength = 2f;
    public float separationSmoothingRate = 8f; // higher = snappier, lower = smoother/laggier
    public bool  retargetOnHit        = true;

    private Vector2 _smoothedSeparationForce;
    private int     _lastSeparationForceFrame = -1;

    public bool showDebug= false;

    public struct NearbyFight
    {
        public CharacterBase Host;
        public Vector2 Centre;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        SpawnPoint = transform.position;
        _blockCharges = MaxBlockCharges;
    }

    protected virtual void Start()
    {
        ChangeState(GetInitialState());
    }

    protected virtual void Update()
    {
        TickBlockCooldown();

        if (!_aiEnabled) return;

        if (!IsActionLocked)
        {
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= searchInterval)
            {
                _searchTimer = 0f;
                OnTargetSearchTick();
            }
        }

        CurrentState?.OnUpdate(this);
    }

    private void OnDestroy()
    {
        ReleaseEngagementSlot();
    }

    // ── FSM ────────────────────────────────────────────────────────────────────────

    public void ChangeState(AIStateBase newState)
    {
        if (newState == null) return;
        CurrentState?.OnExit(this);
        //if(showDebug) print(this.name + "is leaving" + CurrentState?.ToString());
        CurrentState = newState;
        CurrentState.OnEnter(this);
        // Log the state THIS call entered (newState), not the live CurrentState field — OnEnter
        // can itself call ChangeState reentrantly (e.g. CombatBlockState bailing straight to
        // CombatPressureState when the guard is broken), which reassigns CurrentState before
        // this line runs. Reading the field here would misattribute the nested transition's
        // target state to this (outer) call's log line.
        if (showDebug) print(this.name + "has entered" + newState.ToString());
    }

    protected abstract AIStateBase GetInitialState();

    // ── Target search — override in subclasses ─────────────────────────────────────

    protected virtual void OnTargetSearchTick() { }

    // ── Engagement helpers ──────────────────────────────────────────────────────────

    public void ReleaseEngagementSlot()
    {
        CurrentSlotHost?.ReleaseSlot(Controller);
        CurrentSlotHost = null;
    }

    /// <summary>Keeps the claimed slot's compass direction in sync with live position, same cadence as FaceTowards.</summary>
    public void RefreshEngagementSlot() => CurrentSlotHost?.UpdateSlotAngle(Controller);

    // ── Spatial AI helpers ──────────────────────────────────────────────────────────

    public void RefreshNearbyFighters()
    {
        NearbyFighters.Clear();
        NearbyEnemies.Clear();
        NearbyAllies.Clear();

        if (Controller == null) return;
        var hits = Physics2D.OverlapCircleAll(transform.position, awarenessRadius);
        foreach (var hit in hits)
        {
            var cb = hit.GetComponent<CharacterBase>();
            if (cb == null || cb == Controller) continue;
            if (cb.GetComponent<TargetHealth>()?.IsDead() == true) continue;
            NearbyFighters.Add(cb);
            if (cb.characterFaction != Controller.characterFaction)
                NearbyEnemies.Add(cb);
            else
                NearbyAllies.Add(cb);
        }
    }

    public int GetBlockCount()
    {
        return _blockCharges;
    }

    public List<NearbyFight> GetNearbyFightCentres()
    {
        var result = new List<NearbyFight>();
        if (Controller == null) return result;

        foreach (var fighter in NearbyFighters)
        {
            if (fighter == null) continue;
            if (fighter.OccupiedCount <= 0) continue;   // not hosting an active fight
            if (fighter == Controller) continue;        // my own fight (I'm the host)
            if (fighter == CurrentSlotHost) continue;    // my own fight (I'm an attacker in it)

            // Collapse mutual 1-1 duels (both sides host each other's slot) into one entry —
            // otherwise a symmetric duel counts twice (once per host) and pushes observers
            // twice as hard as a one-sided N-attacker fight.
            var mutualPartner = fighter.CurrentTarget;
            if (mutualPartner != null && mutualPartner.CurrentTarget == fighter
                && mutualPartner.OccupiedCount > 0
                && fighter.GetHashCode() > mutualPartner.GetHashCode())
                continue;

            result.Add(new NearbyFight { Host = fighter, Centre = fighter.transform.position });
        }

        return result;
    }

    public Vector2 CalculateSeparationForce(List<NearbyFight> fights)
    {
        Vector2 force = Vector2.zero;
        Vector2 myPos = transform.position;

        foreach (var fight in fights)
        {
            Vector2 away = myPos - fight.Centre;
            float dist = away.magnitude;

            // 1 at/inside enterSeparationRange, 0 at/beyond exitSeparationRange, linear between.
            float t = Mathf.Clamp01((exitSeparationRange - dist) / (exitSeparationRange - enterSeparationRange));
            if (t <= 0f) continue;

            Vector2 dir = dist > 0.001f ? away / dist : Vector2.up;
            force += dir * t * separationForceStrength;
        }

        // Each fight's contribution is individually normalized (t in [0,1]) but nothing bounds
        // the SUM across multiple simultaneous nearby fights — in a busy scene (3+ fights in
        // range) this scaled linearly with fight count, well past the single-fight magnitude the
        // rest of the code assumes (SeparationClearThreshold, the avoidOtherFights blend in
        // MoveWithSeparation). Clamp the total so it stays bounded regardless of how many fights
        // contribute, while still summing directions from multiple sources.
        return Vector2.ClampMagnitude(force, separationForceStrength);
    }

    /// <summary>
    /// Smoothed version of the raw per-fight separation force. GetNearbyFightCentres() depends on
    /// other live characters' positions who are themselves reacting the same way — an unsmoothed
    /// instantaneous read here creates a live multi-agent feedback loop that swings the resulting
    /// movement direction every frame (the "runs around in strange patterns" jitter seen once a
    /// second nearby fight exists). Cached once per frame so both same-frame callers (the
    /// clearOfFights gate in CombatApproachState and MoveWithSeparation below) see one value.
    /// </summary>
    public Vector2 CalculateSeparationForce()
    {
        if (Time.frameCount != _lastSeparationForceFrame)
        {
            Vector2 raw = CalculateSeparationForce(GetNearbyFightCentres());
            float t = 1f - Mathf.Exp(-separationSmoothingRate * Time.deltaTime);
            _smoothedSeparationForce = Vector2.Lerp(_smoothedSeparationForce, raw, t);
            _lastSeparationForceFrame = Time.frameCount;
        }
        return _smoothedSeparationForce;
    }

    /// <summary>
    /// Moves toward destination in a straight line, with an optional inter-fight separation
    /// push blended in. Host-body arc-around avoidance (not walking through your own target)
    /// always applies regardless — that's about not clipping the fight you're heading INTO, not
    /// about avoiding OTHER fights.
    /// </summary>
    /// <param name="avoidOtherFights">
    /// Whether to blend in CalculateSeparationForce() (push away from OTHER nearby fights).
    /// Should be false while still travelling to reach your own assigned slot — you're not yet
    /// genuinely locked into that engagement, and blending in a push-away force while your
    /// destination is already a specific, correct point just fights the straight-line pull and
    /// produces jitter. Should be true only once you've actually arrived and a different fight's
    /// zone is still crowding that position — that's the case where moving away makes sense.
    /// </param>
    public void MoveWithSeparation(Vector2 destination, bool avoidOtherFights = true)
    {
        Vector2 currentPos = transform.position;
        Vector2 toDestination = destination - currentPos;
        if (toDestination.sqrMagnitude < 0.0001f) return;

        Vector2 dir  = toDestination.normalized;
        Vector2 push = Vector2.zero;

        if (avoidOtherFights)
            push += CalculateSeparationForce();

        push += CalculateHostBodyAvoidance(currentPos, dir, toDestination.magnitude);

        // Guard on the vector we're actually about to normalize, not a proxy for it (e.g. push's
        // own magnitude) — if push happens to land nearly opposite dir, dir+push can be a tiny,
        // numerically noisy vector even when push itself isn't small. Normalizing that noise
        // produces an unstable direction that changes wildly frame to frame; falling back to the
        // plain destination direction is more predictable than steering by rounding error.
        Vector2 combined = dir + push;
        Vector2 finalDir = combined.sqrMagnitude > 0.0001f ? combined.normalized : dir;

        Controller.MoveTo(currentPos + finalDir * MoveSpeed * Time.deltaTime * 10f);
    }

    /// <summary>
    /// A straight line to an off-side slot (e.g. directly opposite another attacker) can cut
    /// right through the host's own body. Returns a push away from the host if the path's
    /// closest approach comes inside its body radius, so the fighter arcs around instead of
    /// walking through it — Vector2.zero if there's no host, or no clipping risk.
    /// </summary>
    private Vector2 CalculateHostBodyAvoidance(Vector2 currentPos, Vector2 dir, float distanceToDestination)
    {
        if (CurrentSlotHost == null) return Vector2.zero;

        Vector2 hostPos = CurrentSlotHost.transform.position;
        Vector2 toHost  = hostPos - currentPos;

        // How far along our travel direction the host sits. If it's behind us (<= 0), we're
        // already moving away from it — no clipping risk, regardless of how close we currently
        // happen to be standing to it.
        float along = Vector2.Dot(toHost, dir);
        if (along <= 0f) return Vector2.zero;

        along = Mathf.Min(along, distanceToDestination); // don't project past the destination itself
        Vector2 closestPoint = currentPos + dir * along;
        float clearance = Vector2.Distance(closestPoint, hostPos);

        float bodyRadius = CurrentSlotHost.slotDistance * 0.6f;
        if (clearance >= bodyRadius) return Vector2.zero;
        if (toHost.sqrMagnitude < 0.0001f) return Vector2.zero; // standing on the host — no defined direction to push

        float strength = (bodyRadius - clearance) / bodyRadius; // 0..1, 1 = dead centre
        return -toHost.normalized * strength * 2f;
    }

    public CharacterBase SelectBestTarget()
    {
        if (NearbyEnemies.Count == 0) return null;
        return NearbyEnemies
            .OrderBy(e => e.OccupiedCount)
            .ThenBy(e => IsEngaged(e) ? 1 : 0)
            .ThenBy(e => Vector2.Distance(transform.position, e.transform.position))
            .FirstOrDefault();
    }

    /// <summary>
    /// True if candidate is in a genuine mutual engagement — it holds a slot on some host, and
    /// that host's own CurrentTarget points back at candidate. At most one attacker per host can
    /// ever be true (a host's CurrentTarget can only name one attacker back), so every other
    /// attacker piled onto the same host is, by definition, the peelable "extra." Deliberately
    /// keyed off CurrentTarget rather than slot occupancy: a villager routed through
    /// VillagerPrepareCombatState (finding a shield before engaging) has CurrentTarget set via
    /// ForceReciprocalEngagement immediately, before it's claimed a slot back — checking slot
    /// occupancy here would transiently misreport it as an unengaged extra.
    /// </summary>
    private static bool IsEngaged(CharacterBase candidate)
    {
        var ai = candidate.GetComponent<CharacterAI>();
        return ai != null && ai.CurrentSlotHost != null && ai.CurrentSlotHost.CurrentTarget == candidate;
    }

    // ── AI toggle ──────────────────────────────────────────────────────────────────

    public virtual void SetAIEnabled(bool enabled)
    {
        _aiEnabled = enabled;
        if (!enabled)
        {
            Controller?.Stop();
            CurrentTarget = null;
            ChangeState(GetInitialState());
        }
    }

    public bool IsAIEnabled => _aiEnabled;

    // ── Utility ────────────────────────────────────────────────────────────────────

    public bool IsPointWalkable(Vector2 point)
    {
        if (ObstacleLayer == 0) return true;
        Collider2D overlap = Physics2D.OverlapCircle(point, 0.3f, ObstacleLayer);
        return overlap == null || overlap.isTrigger || overlap.gameObject == gameObject;
    }
}
