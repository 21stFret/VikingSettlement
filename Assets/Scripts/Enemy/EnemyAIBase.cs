using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy-specific AI base. Extends CharacterAI with:
/// - Enemy component stat delegation (detection/attack range, speeds)
/// - Villager target finding (nearest from awareness list)
/// - Ally awareness (same EnemyType pack behaviour)
/// - Retargeting on hit and on nearer target appearing
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
    public override float DetectionRange => EnemyData != null ? EnemyData.GetDetectionRange() : base.DetectionRange;
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
    [SerializeField] private bool  pursueTarget   = true;

    [Header("Combat")]
    [SerializeField] private bool useCombatSlots   = false;
    [SerializeField] private bool retargetOnNearer = false;
    // retargetOnHit lives on CharacterAI base — defaults false for enemies via inspector

    public override float     WanderRadius    => wanderRadius;
    public override float     IdleTimeMin     => idleTimeMin;
    public override float     IdleTimeMax     => idleTimeMax;
    public override float     PursuitRange    => pursuitRange;
    public override float     LoseTargetTime  => loseTargetTime;
    public override bool      PursueTarget    => pursueTarget;
    public override bool      UseCombatSlots  => useCombatSlots;
    public override LayerMask ObstacleLayer   => obstacleLayerMask;
    public          bool      RetargetOnNearer => retargetOnNearer;

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

    // ── Ally awareness ─────────────────────────────────────────────────────────

    public List<EnemyAIBase> GetNearbyAlliesOfSameType(float radius)
    {
        var result = new List<EnemyAIBase>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;
            var ai    = h.GetComponent<EnemyAIBase>();
            var enemy = h.GetComponent<Enemy>();
            if (ai != null && enemy != null && enemy.enemyType == EnemyData.enemyType)
                result.Add(ai);
        }

        return result;
    }

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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

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
