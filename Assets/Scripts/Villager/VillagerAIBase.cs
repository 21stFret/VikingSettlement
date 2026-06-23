using UnityEngine;

/// <summary>
/// Villager-specific AI base. Extends CharacterAI with:
/// - Villager/VillagerController component delegation
/// - Enemy threat detection via OverlapCircle (prefer unengaged targets)
/// - Work state management (assigned building, work radius)
/// - Raid mode (Follow / ShieldWall / Aggressive)
/// - Combat slot + fight-zone pair separation (FightManager)
/// - Cutscene movement
///
/// Concrete hierarchy:
///   VillagerAIBase (this)
///   ├── VillagerAI  — standard villager (job-based combat check)
///   └── JarlAI      — always combat-ready, higher detection, lower flee threshold
/// </summary>
[RequireComponent(typeof(VillagerController))]
[RequireComponent(typeof(Villager))]
public abstract class VillagerAIBase : CharacterAI
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
    [SerializeField] private float threatDetectionRange = 8f;
    [SerializeField] private float combatEngageRange = 6f;
    [SerializeField] private float fleeHealthThreshold = 30f;
    public LayerMask weaponsLayerMask;
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
    public override float     DetectionRange => threatDetectionRange;
    public override float     AttackRange    => combatEngageRange;
    public override LayerMask ObstacleLayer  => VillagerController != null ? VillagerController.obstacleLayer : default;

    public virtual float FleeHealthThreshold => fleeHealthThreshold;

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

    // ── Periodic target search ────────────────────────────────────────────────

    protected override void OnTargetSearchTick()
    {
        if (IsInCutscene) return;
        if (CurrentState is VillagerShieldWallState) return;
        if (VillagerData == null || VillagerData.currentLifeStage != LifeStage.Mature) return;

        // Handle dead current target before searching for a new one
        if (CurrentTarget != null && CurrentTarget.GetComponent<TargetHealth>()?.IsDead() == true)
        {
            ReleaseEngagementSlot();
            CurrentTarget = SwitchToNearestEnemy();
            if (CurrentTarget == null)
                ChangeState(new VillagerIdleState());
            return;
        }

        Transform found = FindNearestTarget();

        if (found != null)
        {
            bool targetChanged  = found != CurrentTarget;
            CurrentTarget       = found;

            if (VillagerController.combatMoveSpeed > 0f)
                VillagerController.SetMoveSpeed(VillagerController.combatMoveSpeed);

            float hp     = VillagerData.currentHealth / VillagerData.maxHealth * 100f;
            float fleeAt = isInRaidMode ? 15f : FleeHealthThreshold;

            if (hp < fleeAt)
            {
                ChangeState(new VillagerFleeState());
                return;
            }

            // Only trigger a new combat state transition when truly entering combat or switching targets
            if (targetChanged || !IsInActiveCombatState())
            {
                if (CombatStyle == CombatType.Ranged)
                {
                    ChangeState(new CombatRangedPositioningState());
                }
                else if (VillagerController.shield == null && CanFindShield())
                {
                    ChangeState(new VillagerPrepareCombatState());
                }
                else
                {
                    ChangeState(new CombatApproachState());
                }
            }
        }
        else
        {
            CurrentTarget = null;
            if (VillagerController.walkMoveSpeed > 0f)
                VillagerController.SetMoveSpeed(VillagerController.walkMoveSpeed);

            if (isInRaidMode && IsInActiveCombatState())
                ChangeState(new VillagerFollowState());

            // Faction assist: help a nearby ally who is being attacked
            if (CurrentTarget == null && !isInRaidMode)
            {
                foreach (var ally in NearbyAllies)
                {
                    if (ally == null) continue;
                    var allyAI = ally.GetComponent<CharacterAI>();
                    if (allyAI == null || allyAI.CurrentTarget == null) continue;
                    if (allyAI.CurrentTarget.GetComponent<TargetHealth>()?.IsDead() == true) continue;
                    CurrentTarget = allyAI.CurrentTarget;
                    if (VillagerController.combatMoveSpeed > 0f)
                        VillagerController.SetMoveSpeed(VillagerController.combatMoveSpeed);
                    ChangeState(new CombatApproachState());
                    return;
                }
            }
        }
    }

    protected bool IsInActiveCombatState()
    {
        return CurrentState is CombatApproachState   ||
               CurrentState is CombatPressureState   ||
               CurrentState is CombatAttackState     ||
               CurrentState is CombatBlockState      ||
               CurrentState is CombatRecoveringState ||
               CurrentState is CombatRetreatState    ||
               CurrentState is VillagerCombatState   ||
               CurrentState is VillagerPrepareCombatState;
    }

    private Transform SwitchToNearestEnemy()
    {
        // Prefer an enemy already attacking one of our allies
        foreach (var enemy in NearbyEnemies)
        {
            if (enemy == null) continue;
            var enemyAI = enemy.GetComponent<CharacterAI>();
            if (enemyAI == null || enemyAI.CurrentTarget == null) continue;
            if (enemyAI.CurrentTarget.GetComponent<CharacterBase>()?.characterFaction == Faction.Player)
                return enemy.transform;
        }
        // Fallback: nearest living enemy
        float closest = Mathf.Infinity;
        Transform nearest = null;
        foreach (var enemy in NearbyEnemies)
        {
            if (enemy == null) continue;
            float d = Vector2.Distance(transform.position, enemy.transform.position);
            if (d < closest) { closest = d; nearest = enemy.transform; }
        }
        return nearest;
    }

    // ── Target finding ────────────────────────────────────────────────────────

    public override Transform FindNearestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DetectionRange);

        // Refresh awareness lists in the same pass — one scan, one radius, one truth.
        NearbyAllies.Clear();
        NearbyEnemies.Clear();

        Enemy nearestEnemy = null, nearestFreeEnemy = null;
        float nearestDist = Mathf.Infinity, nearestFreeDist = Mathf.Infinity;
        var fm = FightManager.Instance;

        foreach (var h in hits)
        {
            var cb = h.GetComponent<CharacterBase>();
            if (cb != null && cb != Controller && cb.GetComponent<TargetHealth>()?.IsDead() != true)
            {
                if (cb.characterFaction == Controller.characterFaction)
                    NearbyAllies.Add(cb);
                else if (cb.characterFaction != Faction.Neutral)
                    NearbyEnemies.Add(cb);
            }

            var enemy = h.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead()) continue;

            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < nearestDist) { nearestDist = d; nearestEnemy = enemy; }

            bool free = fm == null || cb == null || !fm.IsEngaged(cb);
            if (free && d < nearestFreeDist) { nearestFreeDist = d; nearestFreeEnemy = enemy; }
        }

        return (nearestFreeEnemy ?? nearestEnemy)?.transform;
    }

    // ── Combat job check ──────────────────────────────────────────────────────

    public virtual bool IsCombatJob()
    {
        if (VillagerData == null) return false;
        return VillagerData.currentJob == JobType.Warrior
            || VillagerData.currentJob == JobType.Archer
            || VillagerData.currentJob == JobType.Jarl;
    }

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

    public void TryClaimEngagementSlot(CharacterBase target)
    {
        if (target.TryClaimSlot(Controller, out _))
        {
            CurrentSlotHost = target;
            return;
        }

        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position, DetectionRange, Controller.attackTargetLayer);
        foreach (var col in nearby)
        {
            var cb = col.GetComponent<CharacterBase>();
            if (cb == null || cb == target || cb.characterFaction == Faction.Player) continue;
            if (cb.TryClaimSlot(Controller, out _))
            {
                CurrentSlotHost = cb;
                CurrentTarget   = col.transform;
                return;
            }
        }
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, threatDetectionRange);

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
