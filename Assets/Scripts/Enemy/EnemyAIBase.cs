using UnityEngine;

/// <summary>
/// Enemy-specific AI base. Extends CharacterAI with:
/// - Enemy component stat delegation (attack range, speeds)
/// - Retargeting on hit (shared CombatAIBase.HandleHitBy — off by default via inspector)
///
/// Concrete subclasses (EnemyAI, BerserkerAI, WolfAI, etc.) extend this and
/// call GetInitialState() to define their starting state.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(Enemy))]
public abstract class EnemyAIBase : CombatAIBase
{
    public Enemy           EnemyData       { get; private set; }
    public EnemyController EnemyController { get; private set; }

    // Delegate CharacterAI virtual config to Enemy component
    public override float AttackRange    => EnemyData != null ? EnemyData.GetAttackRange()    : base.AttackRange;
    public override float MoveSpeed      => EnemyData != null ? EnemyData.moveSpeed           : base.MoveSpeed;
    public override float ChaseSpeed     => EnemyData != null ? EnemyData.chaseSpeed          : base.ChaseSpeed;

    [Header("Movement")]
    [SerializeField] private float      wanderRadius      = 5f;
    [SerializeField] private float      idleTimeMin       = 1f;
    [SerializeField] private float      idleTimeMax       = 3f;
    [SerializeField] private LayerMask  obstacleLayerMask;

    [Header("Pursuit")]
    [SerializeField] private float pursuitRange   = 15f;
    [SerializeField] private float loseTargetTime = 3f;
    // retargetOnHit lives on CharacterAI base — defaults false for enemies via inspector

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
        ChangeState(new CombatApproachState());
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
