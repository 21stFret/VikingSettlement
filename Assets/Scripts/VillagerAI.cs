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
    
    private VillagerController controller;
    private Villager villagerData;
    
    private float idleTimer = 0f;
    private float nextIdleTime;
    private AIState currentState = AIState.Idle;
    
    private enum AIState
    {
        Idle,
        Wandering,
        Working,
        MovingToWork
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
    }
}
