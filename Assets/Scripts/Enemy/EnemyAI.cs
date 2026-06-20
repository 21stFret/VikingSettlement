using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterBase))]
[RequireComponent(typeof(Enemy))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool enableAI = true;
    [SerializeField] private float updateInterval = 0.5f; // How often to search for targets
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 3f;

    [Header("Combat Settings")]
    [SerializeField] private bool targetNearestVillager = true;
    [SerializeField] private bool pursueTarget = true;
    [SerializeField] private bool faceTargetWhileAttacking = true;
    [SerializeField] private bool useCombatSlots = false;
    [SerializeField] private float pursuitRange = 15f;
    [SerializeField] private float loseTargetTime = 3f;

    [Header("Retargeting")]
    [Tooltip("Switch to a closer unengaged villager mid-fight if one appears")]
    [SerializeField] private bool retargetOnNearer = false;
    [Tooltip("Turn to face and target whoever just hit this enemy")]
    [SerializeField] private bool retargetOnHit = false;

    [Header("Movement")]
    [SerializeField] private LayerMask obstacleLayerMask; // Layer for obstacles to avoid

    private EnemyController controller;
    private Enemy enemyData;
    private Transform currentTarget;
    private Vector2 spawnPoint;

    private float updateTimer = 0f;
    private float idleTimer = 0f;
    private float nextIdleTime;
    private float targetLostTimer = 0f;

    private AIState currentState = AIState.Idle;

    // Combat slot tracking
    private CharacterBase _currentSlotHost;
    private Vector2 _claimedSlotPos;

    // Fight zone (pair-based spatial separation via FightManager)
    private CharacterBase _registeredTarget;
    private Vector2 _fightZoneCenter;

    private enum AIState
    {
        Idle,
        Wandering,
        Searching,
        Chasing,
        Attacking,
        Returning
    }

    private void Awake()
    {
        controller = GetComponent<EnemyController>();
        enemyData = GetComponent<Enemy>();
        spawnPoint = transform.position;
        nextIdleTime = Random.Range(idleTimeMin, idleTimeMax);
    }

    private void Update()
    {
        if (!enableAI || enemyData.IsDead()) return;

        updateTimer += Time.deltaTime;

        // Periodically search for targets
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateTargetSearch();
        }

        // State machine
        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;

            case AIState.Wandering:
                HandleWanderingState();
                break;

            case AIState.Searching:
                HandleSearchingState();
                break;

            case AIState.Chasing:
                HandleChasingState();
                break;

            case AIState.Attacking:
                HandleAttackingState();
                break;

            case AIState.Returning:
                HandleReturningState();
                break;
        }
    }

    private void UpdateTargetSearch()
    {
        // Never interrupt an active fight via target search — retargetOnHit handles that
        if (currentState == AIState.Attacking) return;

        // While chasing a valid target, stay locked — re-searching every tick lets the
        // FightManager engagement preference pull us off the current target toward idle NPCs
        if (currentTarget != null && currentState == AIState.Chasing)
        {
            var currentVillager = currentTarget.GetComponent<Villager>();
            bool stillValid = currentVillager != null && !currentVillager.IsDead()
                && Vector2.Distance(transform.position, currentTarget.position) <= pursuitRange;

            if (stillValid)
            {
                // Optional: switch to something meaningfully closer (raw distance, no engagement bias)
                if (retargetOnNearer) CheckForNearerTarget();
                return;
            }

            // Target lost — clear and fall through to pick a new one
            ReleaseCurrentSlot();
            currentTarget = null;
        }

        // Search for a new target (engagement preference active here to avoid pile-ons)
        currentTarget = FindNearestVillager();

        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= enemyData.GetDetectionRange())
            {
                targetLostTimer = 0f;
                if (currentState != AIState.Chasing && currentState != AIState.Attacking)
                    currentState = AIState.Chasing;
            }
            else if (currentState == AIState.Chasing && distanceToTarget > pursuitRange)
            {
                ReleaseCurrentSlot();
                currentTarget = null;
                currentState = AIState.Returning;
            }
        }
        else if (currentState == AIState.Chasing || currentState == AIState.Attacking)
        {
            targetLostTimer += updateInterval;
            if (targetLostTimer >= loseTargetTime)
            {
                ReleaseCurrentSlot();
                currentTarget = null;
                currentState = AIState.Searching;
            }
        }
    }

    private void HandleIdleState()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer >= nextIdleTime)
        {
            idleTimer = 0f;
            nextIdleTime = Random.Range(idleTimeMin, idleTimeMax);

            // Randomly choose to wander or search
            if (Random.value > 0.5f)
            {
                currentState = AIState.Wandering;
                WanderToRandomPoint();
            }
            else
            {
                currentState = AIState.Searching;
            }
        }
    }

    private void HandleWanderingState()
    {
        if (!controller.ReturnIsMoving())
        {
            currentState = AIState.Idle;
        }
    }

    private void HandleSearchingState()
    {
        // Already handled in UpdateTargetSearch
        // If no target found after a while, go back to wandering
        idleTimer += Time.deltaTime;

        if (idleTimer >= nextIdleTime)
        {
            idleTimer = 0f;
            currentState = AIState.Wandering;
            WanderToRandomPoint();
        }
    }

    private void HandleChasingState()
    {
        if (currentTarget == null || currentTarget.GetComponent<Villager>() == null || !pursueTarget)
        {
            ReleaseCurrentSlot();
            currentState = AIState.Searching;
            return;
        }

        // Check if target is dead
        var villager = currentTarget.GetComponent<Villager>();
        if (villager != null && villager.IsDead())
        {
            ReleaseCurrentSlot();
            currentTarget = null;
            currentState = AIState.Searching;
            return;
        }

        var targetCB = currentTarget.GetComponent<CharacterBase>();
        var fm = FightManager.Instance;

        // Register (or retrieve) our shared fight zone with this target.
        // Zone center is ~midpoint between the two fighters, pushed away from
        // any other active fight zones so duels stay spatially separated.
        if (targetCB != null && targetCB != _registeredTarget)
        {
            ReleaseCurrentSlot(); // clear old slot if target changed
            _registeredTarget = targetCB;
            _fightZoneCenter = fm != null
                ? fm.RegisterPair(controller, targetCB)
                : (Vector2)currentTarget.position;
        }

        // Slots: only activate when a second attacker arrives on the same target
        if (useCombatSlots && fm != null && fm.IsEngaged(targetCB))
        {
            if (targetCB != null && _currentSlotHost != targetCB)
                TryClaimEngagementSlot(targetCB);
        }
        else
        {
            ReleaseCurrentSlot();
        }

        // Navigate toward slot (oversized) or fight zone center (1v1)
        Vector2 destination = (_currentSlotHost != null)
            ? _currentSlotHost.GetSlotWorldPos(controller)
            : _fightZoneCenter;

        controller.SetMoveSpeed(enemyData.chaseSpeed);
        controller.MoveTo(destination);

        // Enter Attacking when within attack range of the actual target
        if (Vector2.Distance(transform.position, currentTarget.position) <= enemyData.GetAttackRange())
        {
            currentState = AIState.Attacking;
            controller.Stop();
        }
    }

    private void HandleAttackingState()
    {
        if (currentTarget == null)
        {
            ReleaseCurrentSlot();
            currentState = AIState.Searching;
            return;
        }

        // Check if target is dead
        var villager = currentTarget.GetComponent<Villager>();
        if (villager != null && villager.currentLifeStage == LifeStage.Dead)
        {
            ReleaseCurrentSlot();
            currentTarget = null;
            currentState = AIState.Searching;
            return;
        }

        // Attack range gates the fight — slots only control approach direction, not attack eligibility
        float distance = Vector2.Distance(transform.position, currentTarget.position);
        if (distance > enemyData.GetAttackRange())
        {
            currentState = AIState.Chasing;
            return;
        }

        // Ensure fight zone is registered (handles case where we entered Attacking
        // without going through HandleChasingState, e.g. spawning inside attack range)
        var targetCB = currentTarget.GetComponent<CharacterBase>();
        if (targetCB != null && targetCB != _registeredTarget)
        {
            _registeredTarget = targetCB;
            _fightZoneCenter = FightManager.Instance != null
                ? FightManager.Instance.RegisterPair(controller, targetCB)
                : (Vector2)currentTarget.position;
        }

        if (faceTargetWhileAttacking)
            controller.FaceTowards(currentTarget.position);

        if (!controller.IsAttacking())
            controller.Attack();
    }

    private void HandleReturningState()
    {
        // Return to spawn point
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPoint);

        if (distanceToSpawn > 1f)
        {
            controller.SetMoveSpeed(enemyData.moveSpeed);
            controller.MoveTo(spawnPoint);
        }
        else
        {
            controller.Stop();
            currentState = AIState.Idle;
        }
    }

    private Transform FindNearestVillager()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemyData.GetDetectionRange());

        List<Villager> aliveVillagers = new List<Villager>();
        foreach (var hit in hits)
        {
            var villager = hit.GetComponent<Villager>();
            if (villager != null && villager.currentLifeStage != LifeStage.Dead)
                aliveVillagers.Add(villager);
        }

        if (aliveVillagers.Count == 0) return null;
        if (aliveVillagers.Count == 1) return aliveVillagers[0].transform;

        var fm = FightManager.Instance;

        // Prefer the nearest villager who isn't already in an active fight,
        // falling back to nearest overall when everyone is engaged
        Transform nearestFree = null;
        Transform nearestAny  = null;
        float distFree = Mathf.Infinity;
        float distAny  = Mathf.Infinity;

        foreach (var villager in aliveVillagers)
        {
            float d = Vector2.Distance(transform.position, villager.transform.position);
            if (d < distAny) { distAny = d; nearestAny = villager.transform; }

            var cb = villager.GetComponent<CharacterBase>();
            bool free = fm == null || !fm.IsEngaged(cb);
            if (free && d < distFree) { distFree = d; nearestFree = villager.transform; }
        }

        if (!targetNearestVillager)
            return aliveVillagers[Random.Range(0, aliveVillagers.Count)].transform;

        return nearestFree != null ? nearestFree : nearestAny;
    }

    private void WanderToRandomPoint()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(1f, wanderRadius);
            Vector2 wanderPoint = spawnPoint + randomDirection * distance;

            // Check if destination is walkable
            if (!IsPointWalkable(wanderPoint))
            {
                continue;
            }

            // Check if path is clear
            RaycastHit2D hit = Physics2D.Raycast(transform.position, randomDirection, distance, obstacleLayerMask);
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
            {
                continue; // Path blocked
            }

            controller.SetMoveSpeed(enemyData.moveSpeed);
            controller.MoveTo(wanderPoint);
            return;
        }

        // Couldn't find clear path, stay idle
        currentState = AIState.Idle;
    }

    /// <summary>
    /// Check if a point is walkable (not inside an obstacle)
    /// </summary>
    private bool IsPointWalkable(Vector2 point)
    {
        Collider2D overlap = Physics2D.OverlapCircle(point, 0.3f, obstacleLayerMask);
        if (overlap != null && !overlap.isTrigger && overlap.gameObject != gameObject)
        {
            return false;
        }
        return true;
    }

    // ── Retargeting ──────────────────────────────────────────────────────────

    private void CheckForNearerTarget()
    {
        if (currentTarget == null) return;
        float dCurrent = Vector2.Distance(transform.position, currentTarget.position);

        // Scan only within a radius smaller than the current target distance so anything
        // found is guaranteed to be meaningfully closer (avoids engagement-bias flip-flopping)
        float scanRadius = dCurrent - 1.5f;
        if (scanRadius <= 0f) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius);
        Transform nearest = null;
        float nearestDist = scanRadius;

        foreach (var col in hits)
        {
            var v = col.GetComponent<Villager>();
            if (v == null || v.IsDead() || col.transform == currentTarget) continue;
            float d = Vector2.Distance(transform.position, col.transform.position);
            if (d < nearestDist) { nearestDist = d; nearest = col.transform; }
        }

        if (nearest == null) return;
        ReleaseCurrentSlot();
        currentTarget = nearest;
        currentState = AIState.Chasing;
    }

    /// <summary>Called by EnemyController when this enemy is hit by an attacker.</summary>
    public void NotifyHitBy(CharacterBase attacker)
    {
        if (!retargetOnHit) return;
        if (attacker == null) return;
        if (attacker.characterFaction == Faction.Enemy) return; // ignore friendly fire

        var villager = attacker.GetComponent<Villager>();
        if (villager == null || villager.IsDead()) return;

        // Only switch if this is a new target — don't interrupt if already fighting them
        if (currentTarget == attacker.transform) return;

        ReleaseCurrentSlot();
        currentTarget = attacker.transform;
        currentState = AIState.Chasing;
    }

    // ── Combat Slot Helpers ───────────────────────────────────────────────────

    private void TryClaimEngagementSlot(CharacterBase target)
    {
        if (target.TryClaimSlot(controller, out _claimedSlotPos))
        {
            _currentSlotHost = target;
            return;
        }

        // All slots on this target are occupied — find another nearby enemy with a free slot
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, enemyData.GetDetectionRange(), controller.attackTargetLayer);
        foreach (var col in nearby)
        {
            var cb = col.GetComponent<CharacterBase>();
            if (cb == null || cb == target || cb.characterFaction == Faction.Enemy) continue;
            if (cb.TryClaimSlot(controller, out _claimedSlotPos))
            {
                _currentSlotHost = cb;
                currentTarget = col.transform;
                return;
            }
        }
        // All slots full — stay in place, retry next update
    }

    private void ReleaseCurrentSlot()
    {
        _currentSlotHost?.ReleaseSlot(controller);
        _currentSlotHost = null;
        FightManager.Instance?.Unregister(controller);
        _registeredTarget = null;
    }

    private void OnDestroy()
    {
        ReleaseCurrentSlot();
    }

    /// <summary>
    /// Enable or disable AI
    /// </summary>
    public void SetAIEnabled(bool enabled)
    {
        enableAI = enabled;
        if (!enabled)
        {
            controller.Stop();
            currentState = AIState.Idle;
        }
    }

    /// <summary>
    /// Force the enemy to target a specific villager
    /// </summary>
    public void SetTarget(Transform target)
    {
        currentTarget = target;
        currentState = AIState.Chasing;
    }

    /// <summary>
    /// Get the current AI state (for debugging)
    /// </summary>
    public string GetCurrentState()
    {
        return currentState.ToString();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw wander radius around spawn point
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint, wanderRadius);

        // Draw pursuit range
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, pursuitRange);

        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }

        // Display current state
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"State: {currentState}");
        #endif
    }
}
