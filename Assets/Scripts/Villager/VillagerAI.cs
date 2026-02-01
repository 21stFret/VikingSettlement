using UnityEngine;

[RequireComponent(typeof(VillagerController))]
public class VillagerAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool enableAI = true;
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
    [SerializeField] private float fleeHealthThreshold = 30f; // Health % below which to flee
    [SerializeField] private float threatCheckInterval = 0.5f;
    public LayerMask weaponsLayerMask;
    public LayerMask movementLayerMask;

    [Header("Raid Behavior")]
    [SerializeField] private bool isInRaidMode = false;
    [SerializeField] private Transform followTarget; // Player-controlled villager to follow
    [SerializeField] private float followDistance = 2f; // How close to stay to the leader
    [SerializeField] private float maxFollowDistance = 8f; // Start following if further than this

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
        Fleeing,
        Following // New state for raid mode
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

            case AIState.Following:
                HandleFollowingState();
                break;
        }
    }
    
    private void HandleIdleState()
    {
        // In raid mode, immediately switch to following if we have a target
        if (isInRaidMode && followTarget != null)
        {
            currentState = AIState.Following;
            return;
        }

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
        for (int i = 0; i < 8; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float distance = Random.Range(2f, wanderRadius);
            Vector2 wanderPoint = (Vector2)transform.position + randomDirection * distance;

            // Check if point is within village boundary
            if (villageCentre != null)
            {
                float distanceFromCentre = Vector2.Distance(wanderPoint, villageCentre.position);
                if (distanceFromCentre > maxDistanceFromCentre)
                {
                    continue;
                }
            }

            // Check if path is clear
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, randomDirection, distance, movementLayerMask);
            bool isClear = true;
            foreach (var hit in hits)
            {
                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    isClear = false;
                    break;
                }
            }
            if (isClear)
            {
                controller.MoveTo(wanderPoint);
                return;
            }
        }

        // Couldn't find clear path, stay idle
        currentState = AIState.Idle;
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

        for (int i = 0; i < 8; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * workRadius;
            Vector2 targetPoint = (Vector2)workLocation.position + randomOffset;
            Vector2 direction = (targetPoint - (Vector2)transform.position).normalized;
            float distance = Vector2.Distance(transform.position, targetPoint);

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, movementLayerMask);
            if (hit.collider == null)
            {
                return targetPoint;
            }
        }

        // Couldn't find clear path, stay in place
        return transform.position;
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
            if (!shield.CompareTag("Shield")) continue;                
            if(shield.GetComponent<EquipableItem>().isEquipped)
            {
                // Already equipped by someone else
                continue;
            }
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
            if(Vector2.Distance(transform.position, _closestShield.transform.position) < 0.1f)
            {

                controller.itemAttachment.EquipShield(_closestShield);
            }
        }
    }
    
    private void HandleCombatState()
    {
        if (currentThreat == null || villagerData == null)
        {
            currentState = isInRaidMode ? AIState.Following : AIState.Idle;
            return;
        }

        // Check if threat is still alive
        var enemy = currentThreat.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead())
        {
            currentThreat = null;
            currentState = isInRaidMode ? AIState.Following : AIState.Idle;
            return;
        }

        // Check health - flee if too low (lower threshold in raid mode)
        float healthPercent = (villagerData.currentHealth / villagerData.maxHealth) * 100f;
        float fleeThreshold = isInRaidMode ? 15f : fleeHealthThreshold;
        if (healthPercent < fleeThreshold)
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
            currentState = isInRaidMode ? AIState.Following : AIState.Idle;
        }

        // add skill exp for combat
        villagerData.skills.ImproveSkill(JobType.Warrior);
    }

    private void HandleFleeingState()
    {
        if (currentThreat == null)
        {
            currentState = isInRaidMode ? AIState.Following : AIState.Idle;
            return;
        }

        // Check if threat is gone
        var enemy = currentThreat.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead())
        {
            currentThreat = null;
            currentState = isInRaidMode ? AIState.Following : AIState.Idle;
            return;
        }

        // Run away from threat, checking for obstacles
        Vector2 directionAway = ((Vector2)transform.position - (Vector2)currentThreat.position).normalized;
        Vector2 fleePoint = (Vector2)transform.position + directionAway * wanderRadius;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionAway, wanderRadius, movementLayerMask);
        if (hit.collider != null)
        {
            // Can't flee directly, try to the sides
            Vector2 leftDir = new Vector2(-directionAway.y, directionAway.x);
            Vector2 rightDir = new Vector2(directionAway.y, -directionAway.x);

            if (!Physics2D.Raycast(transform.position, leftDir, wanderRadius, movementLayerMask))
            {
                fleePoint = (Vector2)transform.position + leftDir * wanderRadius;
            }
            else if (!Physics2D.Raycast(transform.position, rightDir, wanderRadius, movementLayerMask))
            {
                fleePoint = (Vector2)transform.position + rightDir * wanderRadius;
            }
            else
            {
                // Cornered, stop before hitting obstacle
                fleePoint = hit.point - directionAway * 0.5f;
            }
        }
        controller.MoveTo(fleePoint);

        // Check if we're far enough to stop fleeing
        float distanceToThreat = Vector2.Distance(transform.position, currentThreat.position);
        if (distanceToThreat > threatDetectionRange * 2f)
        {
            currentThreat = null;
            currentState = isInRaidMode ? AIState.Following : AIState.Idle;
        }
    }

    private void HandleFollowingState()
    {
        if (followTarget == null)
        {
            // No target to follow, just idle
            currentState = AIState.Idle;
            return;
        }

        float distanceToLeader = Vector2.Distance(transform.position, followTarget.position);

        // If too far from leader, move closer
        if (distanceToLeader > followDistance)
        {
            // Move towards leader but stop at follow distance
            Vector2 directionToLeader = ((Vector2)followTarget.position - (Vector2)transform.position).normalized;
            Vector2 targetPos = (Vector2)followTarget.position - directionToLeader * (followDistance * 0.5f);
            float distanceToTarget = Vector2.Distance(transform.position, targetPos);

            // Check for obstacles
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToLeader, distanceToTarget, movementLayerMask);
            if (hit.collider == null)
            {
                controller.MoveTo(targetPos);
            }
            else
            {
                // Try to go around obstacle
                Vector2 leftDir = new Vector2(-directionToLeader.y, directionToLeader.x);
                if (!Physics2D.Raycast(transform.position, leftDir, 2f, movementLayerMask))
                {
                    controller.MoveTo((Vector2)transform.position + leftDir * 2f);
                }
                else
                {
                    Vector2 rightDir = new Vector2(directionToLeader.y, -directionToLeader.x);
                    controller.MoveTo((Vector2)transform.position + rightDir * 2f);
                }
            }
        }
        else
        {
            // Close enough, stop moving
            if (controller.ReturnIsMoving())
            {
                controller.Stop();
            }
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
            controller.SetMoveSpeed(controller.combatMoveSpeed);

            // In raid mode, always be aggressive
            if (isInRaidMode)
            {
                float healthPercent = (villagerData.currentHealth / villagerData.maxHealth) * 100f;
                // Only flee in raid mode if critically low health (15%)
                if (healthPercent < 15f)
                {
                    currentState = AIState.Fleeing;
                }
                else
                {
                    currentState = AIState.Combat;
                }
                return;
            }

            // React based on job and personality (normal mode)
            if (IsCombatJob())
            {
                // Combat villagers engage
                currentState = AIState.Combat;
            }
            else
            {
                // Non-combat villagers flee
                float healthPercent = (villagerData.currentHealth / villagerData.maxHealth) * 100f;
                if (healthPercent > fleeHealthThreshold)
                {
                    if (controller.shield == null && CanFindShield())
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
        else
        {
            // No threats detected
            currentThreat = null;
            controller.SetMoveSpeed(controller.walkMoveSpeed);

            // In raid mode, go back to following when no threats
            if (isInRaidMode && currentState == AIState.Combat)
            {
                currentState = AIState.Following;
            }
        }
    }
    
    private bool CanFindShield()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var shield in hits)
        {
            if(shield.CompareTag("Shield"))
            {
                if(!shield.GetComponent<EquipableItem>().isEquipped)
                {
                    return true;
                }
            }
        }
        return false;
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
    /// Enable raid mode - villager will follow the target and be more aggressive in combat
    /// </summary>
    public void SetRaidMode(bool enabled, Transform target = null)
    {
        isInRaidMode = enabled;
        followTarget = target;

        if (enabled)
        {
            // Enable AI and start following
            enableAI = true;
            currentState = target != null ? AIState.Following : AIState.Idle;
        }
        else
        {
            // Disable raid mode, return to normal behavior
            followTarget = null;
            currentState = AIState.Idle;
        }
    }

    /// <summary>
    /// Update the follow target (e.g., if leader changes)
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    /// <summary>
    /// Check if currently in raid mode
    /// </summary>
    public bool IsInRaidMode()
    {
        return isInRaidMode;
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

    /// <summary>
    /// Set the village centre and boundary for wandering
    /// </summary>
    public void SetVillageCentre(Transform centre, float maxDistance = 20f)
    {
        villageCentre = centre;
        maxDistanceFromCentre = maxDistance;
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

        // Draw village boundary
        if (villageCentre != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(villageCentre.position, maxDistanceFromCentre);
        }
    }
}
