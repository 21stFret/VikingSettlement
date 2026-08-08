using UnityEngine;

/// <summary>
/// Enemy-specific AI base. Extends CharacterAI with:
/// - Its own combat/movement stat block (attack range, damage, cooldown, speeds) — this is the
///   single owner of per-instance enemy combat tuning, mirroring how VillagerAIBase holds its own
///   combatEngageRange directly rather than delegating to a separate data component.
/// - Retargeting on hit (shared CombatAIBase.HandleHitBy — off by default via inspector)
///
/// Concrete subclasses (EnemyAI, ArcherAI, etc.) extend this and call GetInitialState() to define
/// their starting state. Enemy (the sibling component) only owns health/loot/XP/identity —
/// nothing combat-numeric lives there anymore.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(Enemy))]
public abstract class EnemyAIBase : CombatAIBase
{
    public Enemy           EnemyData       { get; private set; }
    public EnemyController EnemyController { get; private set; }

    [Header("Combat Stats")]
    [SerializeField] private float attackRange    = 1.5f;
    [SerializeField] private float damage         = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    public override float AttackRange => attackRange;

    /// <summary>Flat damage this enemy deals on top of its weapon's own strength — the
    /// "monsters hit harder than their gear alone implies" layer, kept deliberately asymmetric
    /// from villagers (whose damage is purely weapon-derived). Read by EnemyController.OnHitTarget.</summary>
    public float Damage => damage;

    /// <summary>Base attack delay before the equipped weapon's own attackSpeed is added — read by
    /// EnemyController.GetAttackDelay.</summary>
    public float AttackCooldown => attackCooldown;

    [Header("Movement")]
    [SerializeField] private float      moveSpeed         = 1.5f;
    [SerializeField] private float      chaseSpeed        = 2.5f;
    [SerializeField] private float      wanderRadius      = 5f;
    [SerializeField] private float      idleTimeMin       = 1f;
    [SerializeField] private float      idleTimeMax       = 3f;
    [SerializeField] private LayerMask  obstacleLayerMask;

    [Header("Pursuit")]
    [SerializeField] private float pursuitRange   = 15f;
    [SerializeField] private float loseTargetTime = 3f;
    // retargetOnHit lives on CharacterAI base — defaults false for enemies via inspector

    public override float     MoveSpeed       => moveSpeed;
    public override float     ChaseSpeed      => chaseSpeed;
    public override float     WanderRadius    => wanderRadius;
    public override float     IdleTimeMin     => idleTimeMin;
    public override float     IdleTimeMax     => idleTimeMax;
    public override float     PursuitRange    => pursuitRange;
    public override float     LoseTargetTime  => loseTargetTime;
    public override LayerMask ObstacleLayer   => obstacleLayerMask;

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        EnemyData       = GetComponent<Enemy>();
        EnemyController = GetComponent<EnemyController>();
        Controller      = EnemyController;
    }

    protected override void Update()
    {
        if (EnemyData != null && EnemyData.IsDead()) return;
        base.Update();
    }

    // ── CombatAIBase hook overrides ────────────────────────────────────────────

    protected override AIStateBase GetDefaultIdleState() => new IdleState();

    // ── External API ──────────────────────────────────────────────────────────

    public void SetTarget(Transform target)
    {
        CurrentTarget = target;
        ChangeState(GetApproachState());
    }

    // ── Debug gizmos ───────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(SpawnPoint, WanderRadius);

        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, PursuitRange);

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"State: {CurrentState?.GetType().Name ?? "null"}");
#endif
    }
}
