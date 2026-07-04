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
///   └── VillagerAIBase (villager work/combat/raid logic)
///       ├── VillagerAI  (standard villager — job-based combat)
///       └── JarlAI      (always combat, higher detection, lower flee threshold)
/// </summary>
public abstract class CharacterAI : MonoBehaviour
{
    // ── Config — virtual so concrete subclasses can back them with [SerializeField] ──

    public virtual float      DetectionRange => 8f;
    public virtual float      AttackRange    => 1.5f;
    public virtual float      MoveSpeed      => 2f;
    public virtual float      ChaseSpeed     => 3f;
    public virtual float      WanderRadius   => 5f;
    public virtual float      IdleTimeMin    => 1f;
    public virtual float      IdleTimeMax    => 3f;
    public virtual float      PursuitRange   => 15f;
    public virtual float      LoseTargetTime => 3f;
    public virtual bool       PursueTarget   => true;
    public virtual bool       UseCombatSlots => false;
    public virtual LayerMask  ObstacleLayer  => default;
    public virtual bool CanFlee => false;

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
        }
    }

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
    public float driftThreshold       = 1.8f;
    public float attackInterval       = 1.5f;
    public bool  retargetOnHit        = true;

    private Dictionary<(CharacterBase, CharacterBase), bool> _fightPushState
        = new Dictionary<(CharacterBase, CharacterBase), bool>();

    public bool showDebug= false;

    public struct NearbyFight
    {
        public CharacterBase A;
        public CharacterBase B;
        public Vector2 Centre;
        public (CharacterBase, CharacterBase) Key
        {
            get { return A.GetInstanceID() < B.GetInstanceID() ? (A, B) : (B, A); }
        }
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
        if (showDebug) print(this.name + "has entered" + CurrentState.ToString());
    }

    protected abstract AIStateBase GetInitialState();

    // ── Target search — override in subclasses ─────────────────────────────────────

    protected virtual void OnTargetSearchTick() { }

    public virtual Transform FindNearestTarget() => null;

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

        // Purge stale fight-push entries where either fighter is destroyed or dead
        var staleKeys = _fightPushState.Keys
            .Where(k => k.Item1 == null || k.Item2 == null ||
                        k.Item1.GetComponent<TargetHealth>()?.IsDead() == true ||
                        k.Item2.GetComponent<TargetHealth>()?.IsDead() == true)
            .ToList();
        foreach (var key in staleKeys)
            _fightPushState.Remove(key);
    }

    public int GetBlockCount()
    {
        return _blockCharges;
    }

    public List<NearbyFight> GetNearbyFightCentres()
    {
        var result = new List<NearbyFight>();
        if (Controller == null) return result;

        var seen = new HashSet<CharacterBase>();
        var myTargetCB = _currentTarget?.GetComponent<CharacterBase>();

        foreach (var fighter in NearbyFighters)
        {
            if (seen.Contains(fighter)) continue;

            CharacterBase theirTarget = fighter.CurrentTarget;

            bool fighterIsMyTarget     = fighter     == myTargetCB;
            bool theirTargetIsMyTarget = theirTarget == myTargetCB;
            bool fighterIsSelf         = fighter     == Controller;
            bool theirTargetIsSelf     = theirTarget == Controller;

            if (theirTarget == null)       continue;
            if (fighterIsSelf)             continue;
            if (theirTargetIsSelf)         continue;
            if (fighterIsMyTarget)         continue;
            if (theirTargetIsMyTarget)     continue;

            if (theirTarget.CurrentTarget != fighter) continue;

            float dist = Vector2.Distance(fighter.transform.position, theirTarget.transform.position);
            if (dist > Controller.slotDistance * 2f) continue;

            result.Add(new NearbyFight
            {
                A      = fighter,
                B      = theirTarget,
                Centre = ((Vector2)fighter.transform.position + (Vector2)theirTarget.transform.position) / 2f
            });
            seen.Add(fighter);
            seen.Add(theirTarget);
        }

        return result;
    }

    public Vector2 CalculateSeparationForce(List<NearbyFight> fights)
    {
        Vector2 force = Vector2.zero;
        foreach (var fight in fights)
        {
            var key  = fight.Key;
            float dist = Vector2.Distance(transform.position, fight.Centre);

            if (!_fightPushState.ContainsKey(key)) _fightPushState[key] = false;
            if (!_fightPushState[key] && dist < enterSeparationRange)  _fightPushState[key] = true;
            if (_fightPushState[key]  && dist > exitSeparationRange)   _fightPushState[key] = false;

            if (_fightPushState[key])
            {
                Vector2 away = (Vector2)transform.position - fight.Centre;
                force += away.normalized * (1f / Mathf.Max(dist, 0.1f));
            }
        }
        return force;
    }

    public Vector2 CalculateSeparationForce() => CalculateSeparationForce(GetNearbyFightCentres());

    public void MoveWithSeparation(Vector2 destination)
    {
        Vector2 currentPos = transform.position;
        Vector2 toTarget = destination - currentPos;
        if (toTarget.sqrMagnitude < 0.0001f) return;
        Vector2 dir      = toTarget.normalized;
        Vector2 sep      = CalculateSeparationForce();

        // A straight line to an off-side slot (e.g. directly opposite another attacker) can cut
        // right through the host's own body. Push away from the host if the path's closest
        // approach comes inside its body radius, so the fighter arcs around instead of walking
        // through it.
        if (CurrentSlotHost != null)
        {
            Vector2 hostPos     = CurrentSlotHost.transform.position;
            Vector2 toHost      = hostPos - currentPos;
            float bodyRadius    = CurrentSlotHost.slotDistance * 0.6f;
            float along         = Mathf.Clamp(Vector2.Dot(toHost, dir), 0f, toTarget.magnitude);
            Vector2 closestPoint = currentPos + dir * along;
            float clearance     = Vector2.Distance(closestPoint, hostPos);

            if (clearance < bodyRadius && toHost.sqrMagnitude > 0.0001f)
            {
                Vector2 away = -toHost.normalized;
                sep += away * ((bodyRadius - clearance) / bodyRadius) * 2f;
            }
        }

        Vector2 finalDir = sep.magnitude > 0.01f ? (dir + sep).normalized : dir;
        Controller.MoveTo(currentPos + finalDir * MoveSpeed * Time.deltaTime * 10f);
    }

    public bool IsSlotClearOfFights(Vector2 position)
    {
        foreach (var fight in GetNearbyFightCentres())
        {
            var key  = fight.Key;
            float dist = Vector2.Distance(position, fight.Centre);

            if (!_fightPushState.ContainsKey(key)) _fightPushState[key] = false;
            if (!_fightPushState[key] && dist < enterSeparationRange)  _fightPushState[key] = true;
            if (_fightPushState[key]  && dist > exitSeparationRange)   _fightPushState[key] = false;

            if (_fightPushState[key]) return false;
        }
        return true;
    }

    public CharacterBase SelectBestTarget()
    {
        if (NearbyEnemies.Count == 0) return null;
        return NearbyEnemies
            .Where(e => e.GetComponent<TargetHealth>()?.IsDead() != true)
            .OrderBy(e => e.OccupiedCount)
            .ThenBy(e => Vector2.Distance(transform.position, e.transform.position))
            .FirstOrDefault();
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
