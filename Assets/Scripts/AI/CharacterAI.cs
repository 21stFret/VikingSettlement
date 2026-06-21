using UnityEngine;

/// <summary>
/// Abstract base for all character AI. Drives the FSM via a periodic search tick and
/// per-frame state update. Subclasses expose config via virtual properties and override
/// OnTargetSearchTick() to supply targets.
///
/// Concrete hierarchy:
///   CharacterAI  (this)
///   └── EnemyAIBase   (enemy target finding, ally detection)
///       └── EnemyAI   (standard enemy — keeps class name for Unity prefab compat)
///           └── (future) BerserkerAI, WolfAI, ArcherAI …
///   └── (future) VillagerAIBase
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

    [Header("AI Core")]
    [SerializeField] private float searchInterval = 0.5f;

    // ── References ─────────────────────────────────────────────────────────────────

    public CharacterBase Controller { get; protected set; }
    public Transform     CurrentTarget { get; set; }
    public Vector2       SpawnPoint    { get; protected set; }

    // ── FSM ────────────────────────────────────────────────────────────────────────

    public AIStateBase CurrentState { get; private set; }

    private bool  _aiEnabled = true;
    private float _searchTimer;

    // ── Engagement (FightManager + CharacterBase slot system) ──────────────────────

    public CharacterBase RegisteredTarget { get; set; }
    public Vector2       FightZoneCenter  { get; set; }
    public CharacterBase CurrentSlotHost  { get; set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        SpawnPoint = transform.position;
    }

    protected virtual void Start()
    {
        ChangeState(GetInitialState());
    }

    protected virtual void Update()
    {
        if (!_aiEnabled) return;

        _searchTimer += Time.deltaTime;
        if (_searchTimer >= searchInterval)
        {
            _searchTimer = 0f;
            OnTargetSearchTick();
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
        CurrentState = newState;
        CurrentState.OnEnter(this);
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
        FightManager.Instance?.Unregister(Controller);
        RegisteredTarget = null;
    }

    public Vector2 RegisterFightZone(CharacterBase target)
    {
        if (target == RegisteredTarget) return FightZoneCenter;

        ReleaseEngagementSlot();
        RegisteredTarget  = target;
        FightZoneCenter   = FightManager.Instance != null
            ? FightManager.Instance.RegisterPair(Controller, target)
            : (Vector2)target.transform.position;

        return FightZoneCenter;
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
