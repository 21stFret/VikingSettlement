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
    [SerializeField] private float pursuitRange = 15f; // How far to chase before giving up
    [SerializeField] private float loseTargetTime = 3f; // Time before losing interest

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
        if (currentState == AIState.Attacking) return;

        // When slots are active and we've been redirected to a specific target, keep it
        // unless that target is gone — don't let nearest-villager logic override the redirect
        if (useCombatSlots && _currentSlotHost != null)
        {
            var slotVillager = _currentSlotHost.GetComponent<Villager>();
            bool hostAlive = slotVillager != null && !slotVillager.IsDead();
            if (hostAlive) return;
            ReleaseCurrentSlot();
        }

        currentTarget = FindNearestVillager();

        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= enemyData.GetDetectionRange())
            {
                targetLostTimer = 0f;
                // State transitions (including Chasing→Attacking) are handled by state handlers
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

        // Claim or refresh slot on the target (only when toggled on)
        if (useCombatSlots)
        {
            var targetCB = currentTarget.GetComponent<CharacterBase>();
            if (targetCB != null && _currentSlotHost != targetCB)
                TryClaimEngagementSlot(targetCB);
        }

        // Move towards slot position (when slots active) or directly to target
        Vector2 destination;
        if (useCombatSlots && _currentSlotHost != null)
        {
            destination = _currentSlotHost.GetSlotWorldPos(controller);
            controller.SetMoveSpeed(enemyData.chaseSpeed);
            controller.MoveTo(destination);

            // Enter attack state only when standing at the slot, not just near the target
            if (Vector2.Distance(transform.position, destination) <= 0.35f)
            {
                currentState = AIState.Attacking;
                controller.Stop();
            }
        }
        else
        {
            destination = (Vector2)currentTarget.position;
            controller.SetMoveSpeed(enemyData.chaseSpeed);
            controller.MoveTo(destination);

            float distance = Vector2.Distance(transform.position, currentTarget.position);
            if (distance <= enemyData.GetAttackRange())
            {
                currentState = AIState.Attacking;
                controller.Stop();
            }
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

        if (useCombatSlots && _currentSlotHost != null)
        {
            // Stay at the slot; face and attack from there
            Vector2 slotPos = _currentSlotHost.GetSlotWorldPos(controller);
            if (Vector2.Distance(transform.position, slotPos) > 0.5f)
            {
                // Drifted off the slot — return to Chasing to reposition
                currentState = AIState.Chasing;
                return;
            }
        }
        else
        {
            float distance = Vector2.Distance(transform.position, currentTarget.position);
            if (distance > enemyData.GetAttackRange())
            {
                currentState = AIState.Chasing;
                return;
            }
        }

        if (faceTargetWhileAttacking)
            controller.FaceTowards(currentTarget.position);

        if (!controller.IsAttacking())
        {
            controller.Attack();
        }
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
            {
                aliveVillagers.Add(villager);
            }
        }

        if (aliveVillagers.Count == 0) return null;

        // Find nearest
        Transform nearest = null;
        Transform random = null;
        float nearestDistance = Mathf.Infinity;

        if (aliveVillagers.Count == 1)
        {
            return aliveVillagers[0].transform;
        }
            
        for(int i =0; i< aliveVillagers.Count; i++)
        {
            var villager = aliveVillagers[i];
            float distance = Vector2.Distance(transform.position, villager.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = villager.transform;
            }
        }

        random = aliveVillagers[Random.Range(0, aliveVillagers.Count - 1)].transform;

        if(targetNearestVillager)
        {
            return nearest;
        }
        else
        {
            return random;
        }
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
