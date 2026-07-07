using UnityEngine;

/// <summary>
/// Villager-specific AI base. Extends CharacterAI with:
/// - Villager/VillagerController component delegation
/// - Enemy threat detection via OverlapCircle (prefer unengaged targets)
/// - Work state management (assigned building, work radius)
/// - Raid mode (Follow / ShieldWall / Aggressive)
/// - Combat slot management (angle-bisect spatial system)
/// - Cutscene movement
///
/// Every villager is a combatant — there is no non-combat job gate. Concrete hierarchy:
///   VillagerAIBase (this)
///   ├── VillagerAI  — standard villager
///   └── JarlAI      — same combat behaviour, distinct only where JarlAI overrides apply
/// </summary>
[RequireComponent(typeof(VillagerController))]
[RequireComponent(typeof(Villager))]
public abstract class VillagerAIBase : CombatAIBase
{
    // ── Config ────────────────────────────────────────────────────────────────

    [Header("AI Settings")]
    [SerializeField] private float idleTimeMin = 2f;
    [SerializeField] private float idleTimeMax = 5f;
    [SerializeField] private float wanderRadius = 5f;

    [Header("Village Boundary")]
    [SerializeField] private Transform villageCentre;
    [SerializeField] private float maxDistanceFromCentre = 20f;

    [Header("Work Behavior")]
    [SerializeField] private bool shouldWander = true;
    [SerializeField] private Transform workLocation;
    [SerializeField] private float workRadius = 2f;

    [Header("Combat Behavior")]
    [SerializeField] private float combatEngageRange = 6f;
    public LayerMask movementLayerMask;

    [Header("Raid Behavior")]
    [SerializeField] private bool isInRaidMode = false;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followDistance = 2f;
    [SerializeField] private RaidBehavior raidBehavior = RaidBehavior.Follow;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationStrength = 1.5f;

    [HideInInspector] public Vector2 wallFormationOffset;

    // ── CharacterAI config overrides ──────────────────────────────────────────

    public override float     WanderRadius   => wanderRadius;
    public override float     IdleTimeMin    => idleTimeMin;
    public override float     IdleTimeMax    => idleTimeMax;
    public override float     AttackRange    => combatEngageRange;
    public override LayerMask ObstacleLayer  => VillagerController != null ? VillagerController.obstacleLayer : default;

    /// <summary>Block cooldown reduced by combat skill — higher skill = charges refill faster.</summary>
    protected override float GetEffectiveBlockCooldown()
        => base.GetEffectiveBlockCooldown() / VillagerController.GetCombatSkillMultiplier();

    // ── References ────────────────────────────────────────────────────────────

    public Villager           VillagerData       { get; private set; }
    public VillagerController VillagerController { get; private set; }

    // ── State-accessible properties ───────────────────────────────────────────

    public bool         IsInRaidMode          => isInRaidMode;
    public Transform    FollowTarget          => followTarget;
    public float        FollowDistance        => followDistance;
    public bool         ShouldWander          => shouldWander;
    public Transform    WorkLocation          => workLocation;
    public float        WorkRadius            => workRadius;
    public Transform    VillageCentre         => villageCentre;
    public float        MaxDistanceFromCentre => maxDistanceFromCentre;
    public float        SeparationRadius      => separationRadius;
    public float        SeparationStrength    => separationStrength;
    public float        CombatEngageRange     => combatEngageRange;
    public RaidBehavior CurrentRaidBehavior   => raidBehavior;
    public bool         IsInCutscene          { get; private set; }
    public Vector3      CutsceneTargetPos     { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        VillagerData       = GetComponent<Villager>();
        VillagerController = GetComponent<VillagerController>();
        Controller         = VillagerController;
    }

    protected virtual void OnEnable()
    {
        if (VillagerController == null) return;
        ReleaseEngagementSlot();
        ChangeState(GetInitialState());
    }

    protected virtual void OnDisable()
    {
        ReleaseEngagementSlot();
        VillagerController?.Stop();
    }

    protected override void Update()
    {
        if (VillagerData != null && VillagerData.IsDead()) return;
        base.Update();
    }

    // ── CombatAIBase hook overrides ───────────────────────────────────────────

    protected override bool ShouldAbortSearchTick()
    {
        if (IsInCutscene) return true;
        if (CurrentState is VillagerShieldWallState) return true;
        if (VillagerData == null || VillagerData.currentLifeStage != LifeStage.Mature) return true;
        return false;
    }

    protected override void OnTargetAcquired(CharacterBase target, bool targetChanged)
    {
        if (VillagerController.combatMoveSpeed > 0f)
            VillagerController.SetMoveSpeed(VillagerController.combatMoveSpeed);

        if (CombatStyle == CombatType.Ranged)
            ChangeState(new CombatRangedPositioningState());
        else if (VillagerController.shield == null && CanFindShield())
            ChangeState(new VillagerPrepareCombatState());
        else
            ChangeState(new CombatApproachState());
    }

    protected override void OnNoTargetFound()
    {
        if (VillagerController.walkMoveSpeed > 0f)
            VillagerController.SetMoveSpeed(VillagerController.walkMoveSpeed);

        if (isInRaidMode && IsInActiveCombatState())
            ChangeState(new VillagerFollowState());
    }

    protected override AIStateBase GetDefaultIdleState() => new VillagerIdleState();

    // Raid-mode villagers don't inherit an ally's target — they follow the raid leader instead
    protected override CharacterBase InheritAllyTarget() =>
        isInRaidMode ? null : base.InheritAllyTarget();

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetRaidMode(bool enabled, Transform target = null)
    {
        isInRaidMode = enabled;
        followTarget  = target;

        if (enabled)
        {
            SetAIEnabled(true);
            ChangeState(target != null ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
        }
        else
        {
            followTarget = null;
            ChangeState(new VillagerIdleState());
        }
    }

    public void SetFollowTarget(Transform target) => followTarget = target;

    public void SetRaidBehavior(RaidBehavior behavior)
    {
        raidBehavior = behavior;
        if (!isInRaidMode) return;

        switch (behavior)
        {
            case RaidBehavior.Follow:
                VillagerController.isBlocking = false;
                ChangeState(new VillagerFollowState());
                break;
            case RaidBehavior.ShieldWall:
                VillagerController.Stop();
                ChangeState(new VillagerShieldWallState());
                break;
            case RaidBehavior.Aggressive:
                VillagerController.isBlocking = false;
                ChangeState(new VillagerIdleState()); // threat tick drives to Combat
                break;
        }
    }

    public void SetCutsceneTarget(Vector3 position)
    {
        IsInCutscene    = true;
        CutsceneTargetPos = position;
        SetAIEnabled(true);
        ChangeState(new VillagerMovingToPositionState());
        VillagerController.MoveTo(position);
    }

    public void ClearCutsceneTarget()
    {
        IsInCutscene = false;
        ChangeState(new VillagerIdleState());
    }

    public void SetWorkLocation(Transform location, float radius = 2f)
    {
        workLocation = location;
        workRadius   = radius;
        ChangeState(new VillagerMoveToWorkState());
    }

    public void SetVillageCentre(Transform centre, float maxDistance = 20f)
    {
        villageCentre          = centre;
        maxDistanceFromCentre  = maxDistance;
    }

    public void SetWandering(bool wander) => shouldWander = wander;

    // ── Helpers for state classes ──────────────────────────────────────────────

    public bool CanFindShield()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var h in hits)
        {
            if (!h.CompareTag("Shield")) continue;
            var item = h.GetComponent<EquipableItem>();
            if (item != null && !item.isEquipped) return true;
        }
        return false;
    }

    public Vector2 GetRandomPointNearWork()
    {
        if (workLocation == null) return transform.position;

        for (int i = 0; i < 8; i++)
        {
            Vector2 pt  = (Vector2)workLocation.position + Random.insideUnitCircle * workRadius;
            if (!IsPointWalkable(pt)) continue;

            Vector2 dir  = (pt - (Vector2)transform.position).normalized;
            float   dist = Vector2.Distance(transform.position, pt);
            if (Physics2D.Raycast(transform.position, dir, dist, movementLayerMask).collider == null)
                return pt;
        }
        return transform.position;
    }

    public Vector2 GetSeparationForce()
    {
        Vector2 force = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, separationRadius, 1 << 6);
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<VillagerController>() == null) continue;

            Vector2 away = (Vector2)transform.position - (Vector2)col.transform.position;
            float   dist = away.magnitude;
            if (dist < 0.001f) { away = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)); dist = 1f; }
            force += away.normalized * (1f - dist / separationRadius);
        }
        return force;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (workLocation != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(workLocation.position, workRadius);
        }

        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, combatEngageRange);

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }

        if (villageCentre != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(villageCentre.position, maxDistanceFromCentre);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"State: {CurrentState?.GetType().Name ?? "null"}");
#endif
    }
}
