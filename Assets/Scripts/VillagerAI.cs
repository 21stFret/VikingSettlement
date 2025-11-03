using UnityEngine;

[RequireComponent(typeof(VillagerController))]
public class VillagerAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool enableAI = true;
    [SerializeField] private float idleTimeMin = 2f;
    [SerializeField] private float idleTimeMax = 5f;
    [SerializeField] private float wanderRadius = 5f;
    
    [Header("Work Behavior")]
    [SerializeField] private bool shouldWander = true;
    [SerializeField] private Transform workLocation;
    [SerializeField] private float workRadius = 2f;

    [Header("Combat Behavior")]
    [SerializeField] private float threatDetectionRange = 8f;
    [SerializeField] private float combatEngageRange = 6f;
    [SerializeField] private float fleeHealthThreshold = 30f; // Health % below which to flee
    [SerializeField] private float threatCheckInterval = 0.5f;
    public LayerMask weaponsLayerMask;

    private VillagerController controller;
    private Villager villagerData;
    private Transform currentThreat; // Current enemy target
    private float threatCheckTimer = 0f;

    private float idleTimer = 0f;
    private float nextIdleTime;
    private AIState currentState = AIState.Idle;

    private enum AIState
    {
        Idle,
        Wandering,
        Working,
        MovingToWork,
        PrepareCombat,
        Combat,
        Fleeing
    }
    
    private void Awake()
    {
        controller = GetComponent<VillagerController>();
        villagerData = GetComponent<Villager>();
        nextIdleTime = Random.Range(idleTimeMin, idleTimeMax);
    }
    
    private void Update()
    {
        if (!enableAI) return;

        // Periodically check for threats (only if alive and mature)
        if (villagerData != null && villagerData.currentLifeStage == LifeStage.Mature)
        {
            threatCheckTimer += Time.deltaTime;
            if (threatCheckTimer >= threatCheckInterval)
            {
                threatCheckTimer = 0f;
                CheckForThreats();
            }
        }

        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;

            case AIState.Wandering:
                HandleWanderingState();
                break;

            case AIState.Working:
                HandleWorkingState();
                break;

            case AIState.MovingToWork:
                HandleMovingToWorkState();
                break;

            case AIState.Combat:
                HandleCombatState();
                break;

            case AIState.PrepareCombat:
                HandlePrepareForCombat();
                break;

            case AIState.Fleeing:
                HandleFleeingState();
                break;
        }
    }
    
    private void HandleIdleState()
    {
        idleTimer += Time.deltaTime;
        
        if (idleTimer >= nextIdleTime)
        {
            idleTimer = 0f;
            nextIdleTime = Random.Range(idleTimeMin, idleTimeMax);
            
            // Decide next action
            if (villagerData != null && villagerData.assignedBuilding != null)
            {
                // Has a job, move to work location
                workLocation = villagerData.assignedBuilding.transform;
                currentState = AIState.MovingToWork;
                MoveToWorkLocation();
            }
            else if (shouldWander)
            {
                // No job, just wander around
                currentState = AIState.Wandering;
                WanderToRandomPoint();
            }
        }
    }
    
    private void HandleWanderingState()
    {
        if (!controller.ReturnIsMoving())
        {
            // Reached wander destination, go back to idle
            currentState = AIState.Idle;
        }
    }
    
    private void HandleWorkingState()
    {
        // Check if still assigned to building
        if (villagerData == null || villagerData.assignedBuilding == null)
        {
            currentState = AIState.Idle;
            return;
        }
        
        // Occasionally move around work area
        idleTimer += Time.deltaTime;
        if (idleTimer >= nextIdleTime)
        {
            idleTimer = 0f;
            nextIdleTime = Random.Range(idleTimeMin * 2, idleTimeMax * 2); // Longer intervals when working
            
            // Move to a random point near work location
            Vector2 randomPoint = GetRandomPointNearWork();
            controller.MoveTo(randomPoint);
        }
        
        // Perform work (handled by Villager.Work() method)
        if (villagerData != null)
        {
            villagerData.Work(Time.deltaTime);
        }
    }
    
    private void HandleMovingToWorkState()
    {
        if (!controller.ReturnIsMoving())
        {
            // Reached work location
            currentState = AIState.Working;
            idleTimer = 0f;
        }
    }
    
    private void WanderToRandomPoint()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector2 wanderPoint = (Vector2)transform.position + randomDirection * Random.Range(1f, wanderRadius);
        controller.MoveTo(wanderPoint);
    }
    
    private void MoveToWorkLocation()
    {
        if (workLocation != null)
        {
            Vector2 workPoint = GetRandomPointNearWork();
            controller.MoveTo(workPoint);
        }
    }

    private Vector2 GetRandomPointNearWork()
    {
        if (workLocation == null) return transform.position;

        Vector2 randomOffset = Random.insideUnitCircle * workRadius;
        return (Vector2)workLocation.position + randomOffset;
    }

    private void HandlePrepareForCombat()
    {
        if (currentThreat == null || villagerData == null)
        {
            currentState = AIState.Idle;
            return;
        }

        if(controller.shield != null)
        {
            // Already have shield, switch to combat
            currentState = AIState.Combat;
            return;
        }


        // Find closest Shield
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        float closestDistance = Mathf.Infinity;
        GameObject _closestShield = null;
        foreach (var shield in hits)
        {
            if(!shield.CompareTag("Shield")) continue;
            float distance = Vector2.Distance(transform.position, shield.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                _closestShield = shield.gameObject;
            }
        }
        if (_closestShield != null)
        {
            // Equip the closest shield
            controller.MoveTo(_closestShield.transform.position);
            if(Vector2.Distance(transform.position, _closestShield.transform.position) < 0.2f)
            {
                controller.itemAttachment.EquipShield(_closestShield);
            }
        }
    }
    
    private void HandleCombatState()
    {
        if (currentThreat == null || villagerData == null)
        {
            currentState = AIState.Idle;
            return;
        }

        // Check if threat is still alive
        var enemy = currentThreat.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead())
        {
            currentThreat = null;
            currentState = AIState.Idle;
            return;
        }

        // Check health - flee if too low
        float healthPercent = (villagerData.health / villagerData.maxHealth) * 100f;
        if (healthPercent < fleeHealthThreshold)
        {
            currentState = AIState.Fleeing;
            return;
        }

        // Calculate distance to threat
        float distanceToThreat = Vector2.Distance(transform.position, currentThreat.position);

        // If in attack range, stop and attack
        if (distanceToThreat <= combatEngageRange && controller.weapon != null)
        {
            controller.Stop();
            controller.Attack();
        }
        else if (distanceToThreat > combatEngageRange)
        {
            controller.MoveTo(currentThreat.position);
            /*
            // Move towards threat if combat job, otherwise flee
            if (IsCombatJob())
            {
                controller.MoveTo(currentThreat.position);
            }
            else
            {
                currentState = AIState.Fleeing;
            }
            */
        }

        // If threat is too far, stop engaging
        if (distanceToThreat > threatDetectionRange * 1.5f)
        {
            currentThreat = null;
            currentState = AIState.Idle;
        }
    }

    private void HandleFleeingState()
    {
        if (currentThreat == null)
        {
            currentState = AIState.Idle;
            return;
        }

        // Check if threat is gone
        var enemy = currentThreat.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead())
        {
            currentThreat = null;
            currentState = AIState.Idle;
            return;
        }

        // Run away from threat
        Vector2 directionAway = ((Vector2)transform.position - (Vector2)currentThreat.position).normalized;
        Vector2 fleePoint = (Vector2)transform.position + directionAway * wanderRadius;
        controller.MoveTo(fleePoint);

        // Check if we're far enough to stop fleeing
        float distanceToThreat = Vector2.Distance(transform.position, currentThreat.position);
        if (distanceToThreat > threatDetectionRange * 2f)
        {
            currentThreat = null;
            currentState = AIState.Idle;
        }
    }

    private void CheckForThreats()
    {
        // Check for threats if young and old
        if (villagerData == null || villagerData.currentLifeStage == LifeStage.Dead)
        {
            return;
        }

        // Find all enemies in detection range
        Enemy[] allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (var enemy in allEnemies)
        {
            if (enemy.IsDead()) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < threatDetectionRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        // If we found a threat
        if (nearestEnemy != null)
        {
            currentThreat = nearestEnemy.transform;

            // React based on job and personality
            if (IsCombatJob())
            {
                // Combat villagers engage
                currentState = AIState.Combat;
            }
            else
            {
                
                // Non-combat villagers flee
                float healthPercent = (villagerData.health / villagerData.maxHealth) * 100f;
                if (healthPercent > fleeHealthThreshold)
                {
                    if (controller.shield == null)
                    {
                        // If healthy will try to find shield first
                        currentState = AIState.PrepareCombat;
                    }
                    else
                    {
                        // If healthy and armed, might fight
                        currentState = AIState.Combat;
                    }
                }
                else
                {
                    currentState = AIState.Fleeing;
                }
            }
        }
    }

    private bool IsCombatJob()
    {
        if (villagerData == null) return false;

        return villagerData.currentJob == JobType.Warrior ||
               villagerData.currentJob == JobType.Archer ||
               villagerData.currentJob == JobType.Jarl;
    }

    /// <summary>
    /// Enable or disable AI behavior
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
    /// Set whether the villager should wander when idle
    /// </summary>
    public void SetWandering(bool wander)
    {
        shouldWander = wander;
    }
    
    /// <summary>
    /// Assign a work location for the villager
    /// </summary>
    public void SetWorkLocation(Transform location, float radius = 2f)
    {
        workLocation = location;
        workRadius = radius;
        currentState = AIState.MovingToWork;
        MoveToWorkLocation();
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw wander radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Draw work area
        if (workLocation != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(workLocation.position, workRadius);
        }

        // Draw threat detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, threatDetectionRange);

        // Draw combat engage range
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, combatEngageRange);

        // Draw line to current threat
        if (currentThreat != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentThreat.position);
        }
    }
}
