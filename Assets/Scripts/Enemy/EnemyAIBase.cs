using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Enemy-specific AI base. Extends CharacterAI with:
/// - Enemy component stat delegation (detection/attack range, speeds)
/// - Villager target finding with FightManager engagement preference
/// - Ally awareness (same EnemyType pack behaviour)
/// - Retargeting on hit and on nearer target appearing
///
/// Concrete subclasses (EnemyAI, BerserkerAI, WolfAI, etc.) extend this and
/// call GetInitialState() to define their starting state.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(Enemy))]
public abstract class EnemyAIBase : CharacterAI
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
    [SerializeField] private bool retargetOnHit    = false;
    [SerializeField] private bool retargetOnNearer = false;

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

    // ── Periodic target search ─────────────────────────────────────────────────

    protected override void OnTargetSearchTick()
    {
        if (CurrentState is CombatState) return;

        // Stay locked on a still-valid chase target
        if (CurrentState is ChaseState && CurrentTarget != null)
        {
            var v = CurrentTarget.GetComponent<Villager>();
            bool stillValid = v != null && !v.IsDead()
                && Vector2.Distance(transform.position, CurrentTarget.position) <= PursuitRange;

            if (stillValid)
            {
                if (RetargetOnNearer) CheckForNearerTarget();
                return;
            }

            // Target invalid — clear it; ChaseState's lost-timer takes over
            ReleaseEngagementSlot();
            CurrentTarget = null;
            return;
        }

        // Not chasing or chasing with no valid target — search
        Transform found = FindNearestTarget();

        // Pack behaviour: inherit an ally's target if own scan yields nothing
        if (found == null)
        {
            var allies = GetNearbyAlliesOfSameType(DetectionRange * 1.5f);
            found = allies.FirstOrDefault(a => a.CurrentTarget != null)?.CurrentTarget;
        }

        CurrentTarget = found;

        if (CurrentTarget != null
            && (CurrentState is IdleState
                || CurrentState is WanderState
                || CurrentState is SearchState))
        {
            ChangeState(new ChaseState());
        }
    }

    // ── Target finding ─────────────────────────────────────────────────────────

    public override Transform FindNearestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DetectionRange);

        var alive = new List<Villager>();
        foreach (var h in hits)
        {
            var v = h.GetComponent<Villager>();
            if (v != null && !v.IsDead()) alive.Add(v);
        }

        if (alive.Count == 0) return null;
        if (alive.Count == 1) return alive[0].transform;

        var fm = FightManager.Instance;
        Transform nearestFree = null, nearestAny = null;
        float distFree = Mathf.Infinity, distAny = Mathf.Infinity;

        foreach (var v in alive)
        {
            float d = Vector2.Distance(transform.position, v.transform.position);
            if (d < distAny) { distAny = d; nearestAny = v.transform; }

            bool free = fm == null || !fm.IsEngaged(v.GetComponent<CharacterBase>());
            if (free && d < distFree) { distFree = d; nearestFree = v.transform; }
        }

        return nearestFree ?? nearestAny;
    }

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

    // ── Retargeting ────────────────────────────────────────────────────────────

    public void NotifyHitBy(CharacterBase attacker)
    {
        if (!retargetOnHit || attacker == null) return;
        if (attacker.characterFaction == Faction.Enemy) return;

        var villager = attacker.GetComponent<Villager>();
        if (villager == null || villager.IsDead()) return;
        if (CurrentTarget == attacker.transform) return;

        ReleaseEngagementSlot();
        CurrentTarget = attacker.transform;
        ChangeState(new ChaseState());
    }

    public void CheckForNearerTarget()
    {
        if (CurrentTarget == null) return;

        float dCurrent  = Vector2.Distance(transform.position, CurrentTarget.position);
        float scanRadius = dCurrent - 1.5f;
        if (scanRadius <= 0f) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius);
        Transform nearest = null;
        float nearestDist = scanRadius;

        foreach (var col in hits)
        {
            var v = col.GetComponent<Villager>();
            if (v == null || v.IsDead() || col.transform == CurrentTarget) continue;
            float d = Vector2.Distance(transform.position, col.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = col.transform; }
        }

        if (nearest == null) return;

        ReleaseEngagementSlot();
        CurrentTarget = nearest;
        ChangeState(new ChaseState());
    }

    // ── External API (backwards-compat with old EnemyAI callers) ──────────────

    public void SetTarget(Transform target)
    {
        CurrentTarget = target;
        ChangeState(new ChaseState());
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
