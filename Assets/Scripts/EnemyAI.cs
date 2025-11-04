using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
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
    [SerializeField] private float pursuitRange = 15f; // How far to chase before giving up
    [SerializeField] private float loseTargetTime = 3f; // Time before losing interest

    private EnemyController controller;
    private Enemy enemyData;
    private Transform currentTarget;
    private Vector2 spawnPoint;

    private float updateTimer = 0f;
    private float idleTimer = 0f;
    private float nextIdleTime;
    private float targetLostTimer = 0f;

    private AIState currentState = AIState.Idle;

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
        // Don't search for new targets while attacking
        if (currentState == AIState.Attacking) return;

        // Find nearest villager
        currentTarget = FindNearestVillager();

        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

            // Check if target is in detection range
            if (distanceToTarget <= enemyData.GetDetectionRange())
            {
                targetLostTimer = 0f;

                // Check if in attack range
                if (distanceToTarget <= enemyData.GetAttackRange())
                {
                    currentState = AIState.Attacking;
                }
                else if (currentState != AIState.Attacking)
                {
                    currentState = AIState.Chasing;
                }
            }
            else if (currentState == AIState.Chasing && distanceToTarget > pursuitRange)
            {
                // Target too far, lose interest
                currentTarget = null;
                currentState = AIState.Returning;
            }
        }
        else if (currentState == AIState.Chasing || currentState == AIState.Attacking)
        {
            // Lost target
            targetLostTimer += updateInterval;
            if (targetLostTimer >= loseTargetTime)
            {
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
        if (currentTarget == null || currentTarget.GetComponent<Villager>() == null)
        {
            currentState = AIState.Searching;
            return;
        }

        // Check if target is dead
        var villager = currentTarget.GetComponent<Villager>();
        if (villager != null && villager.currentLifeStage == LifeStage.Dead)
        {
            currentTarget = null;
            currentState = AIState.Searching;
            return;
        }

        // Move towards target
        controller.SetMoveSpeed(enemyData.chaseSpeed);
        controller.MoveTo(currentTarget.position);

        // Check if close enough to attack
        float distance = Vector2.Distance(transform.position, currentTarget.position);
        if (distance <= enemyData.GetAttackRange())
        {
            currentState = AIState.Attacking;
            controller.Stop();
        }
    }

    private void HandleAttackingState()
    {
        if (currentTarget == null)
        {
            currentState = AIState.Searching;
            return;
        }

        // Check if target is dead
        var villager = currentTarget.GetComponent<Villager>();
        if (villager != null && villager.currentLifeStage == LifeStage.Dead)
        {
            currentTarget = null;
            currentState = AIState.Searching;
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // If target moved out of range, chase again
        if (distance > enemyData.GetAttackRange())
        {
            currentState = AIState.Chasing;
            return;
        }

        // Face the target
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        // The animation system will handle sprite flipping

        // Perform attack
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
        float nearestDistance = Mathf.Infinity;

        if (aliveVillagers.Count == 1)
        {
            return aliveVillagers[0].transform;
        }
            
        foreach (var villager in aliveVillagers)
        {
            float distance = Vector2.Distance(transform.position, villager.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = villager.transform;
            }
        }

        return nearest;
    }

    private void WanderToRandomPoint()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector2 wanderPoint = spawnPoint + randomDirection * Random.Range(1f, wanderRadius);

        controller.SetMoveSpeed(enemyData.moveSpeed);
        controller.MoveTo(wanderPoint);
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
