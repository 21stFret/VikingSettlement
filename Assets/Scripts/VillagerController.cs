using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class VillagerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.1f;
    public bool canMove = true;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipSpriteOnDirection = true;

    [Header("Attacking")]
    [SerializeField] private Vector2 swordAttackSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 spearAttackSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 swordAttackOffset = new Vector2(1f, 1f);
    [SerializeField] private Vector2 spearAttackOffset = new Vector2(1f, 1f);
    [SerializeField] private LayerMask enemyLayer;
    private Vector2 _hitboxPos;
    private Vector2 _hitboxsize;
    private Vector2 _hitboxOffset;
    public float attackDelay = 1f;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.down;
    private float cachedMoveX = 0f;
    
    // Target position for autonomous movement
    private Vector2? targetPosition = null;
    private bool isMovingToTarget = false;
    
    // Animation parameter names
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveY = Animator.StringToHash("LastMoveY");
    private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
    private static readonly int IsDead = Animator.StringToHash("IsDead");


    private ItemAttachment _itemAttachment;

    public bool HasItemAttachment => _itemAttachment != null;
    public EquipableItem weapon;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _itemAttachment = GetComponent<ItemAttachment>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    private void Update()
    {
        if (!canMove)
        {
            movement = Vector2.zero;
            return;
        }
        if (isMovingToTarget && targetPosition.HasValue)
        {
            MoveToTarget();
        }
    }
    
    private void FixedUpdate()
    {
        // Move the villager
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
    
    private void LateUpdate()
    {
        UpdateAnimations();
    }
    
    /// <summary>
    /// Move the villager to a specific position autonomously
    /// </summary>
    public void MoveTo(Vector2 destination)
    {
        targetPosition = destination;
        isMovingToTarget = true;
    }

    /// <summary>
    /// Stop the villager's current movement
    /// </summary>
    public void Stop()
    {
        targetPosition = null;
        isMovingToTarget = false;
        movement = Vector2.zero;
    }

    public void Attack()
    {
        if (Time.time - lastAttackTime < attackDelay) return;
        lastAttackTime = Time.time;

        if (_itemAttachment != null)
        {
            _hitboxsize = new Vector2(1f, 1f);
            if (weapon == null)
            {
                animator.SetTrigger("Attack");
                // Additional attack logic can be added here
            }
            else if (weapon != null)
            {
                // Logic for when the weapon is equipped
                if (weapon.itemType == EquipableItem.ItemType.Sword)
                {
                    animator.SetTrigger("SwordAttack");
                    _hitboxsize = swordAttackSize;
                    _hitboxOffset = swordAttackOffset;
                }
                else if (weapon.itemType == EquipableItem.ItemType.Spear)
                {
                    animator.SetTrigger("SpearAttack");
                    _hitboxsize = spearAttackSize;
                    _hitboxOffset = spearAttackOffset;
                }
                else if (weapon.itemType == EquipableItem.ItemType.Axe)
                {
                    animator.SetTrigger("AxeAttack");
                }
            }
        }
        
    }

    /// <summary>
    /// Perform the attack hitbox check by animation event
    /// </summary> 
    public void PerformAttackHitbox()
    {
        if (cachedMoveX < 0.01f)
        {
            _hitboxOffset.x = -_hitboxOffset.x;
        }
        else if (cachedMoveX > 0.01f)
        {
            _hitboxOffset.x = Math.Abs(_hitboxOffset.x);
        }
        _hitboxPos = new Vector2(transform.position.x + cachedMoveX, transform.position.y) + _hitboxOffset;
        var hitObjects = Physics2D.OverlapBoxAll(_hitboxPos, _hitboxsize, 0f, enemyLayer);
        foreach (var hit in hitObjects)
        {
            // Apply damage or effects to hit enemies
            Debug.Log($"Hit enemy: {hit.name}");
            var hittable = hit.GetComponent<TargetHealth>();
            if (hittable != null)
            {
                hittable.TakeDamage(weapon.damage);
            }
        }
        isAttacking = true;
    }

    /// <summary>
    /// Called at the end of the attack animation via event
    /// </summary>
    public void StopAttacking()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Check if villager is currently moving
    /// </summary>
    public bool ReturnIsMoving()
    {
        return movement.magnitude > 0.01f;
    }
    
    private void MoveToTarget()
    {
        if (!targetPosition.HasValue) return;
        
        Vector2 currentPos = rb.position;
        Vector2 targetPos = targetPosition.Value;
        
        // Check if we've reached the target
        float distance = Vector2.Distance(currentPos, targetPos);
        if (distance <= stopDistance)
        {
            Stop();
            return;
        }
        
        // Calculate direction to target
        Vector2 direction = (targetPos - currentPos).normalized;
        movement = direction;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = movement.magnitude > 0.01f;

        // Update movement parameters
        animator.SetFloat(MoveX, movement.x);
        animator.SetFloat(MoveY, movement.y);
        animator.SetBool(IsMoving, isMoving);

        // Update last move direction (for idle animations facing correct direction)
        if (isMoving)
        {
            lastMoveDirection = movement.normalized;
            animator.SetFloat(LastMoveX, lastMoveDirection.x);
            animator.SetFloat(LastMoveY, lastMoveDirection.y);

            if (lastMoveDirection.x != cachedMoveX && Math.Abs(lastMoveDirection.x) > 0.01f)
            {
                cachedMoveX = lastMoveDirection.x;
            }

            // Flip sprite based on horizontal movement
            if (flipSpriteOnDirection)
            {
                if (movement.x > 0.01f)
                {
                    // Facing right - normal scale
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else if (movement.x < -0.01f)
                {
                    // Facing left - flip scale
                    transform.localScale = new Vector3(-1, 1, 1);
                }
            }
        }
    }

    /// <summary>
    /// Manually set movement direction (for direct control if needed)
    /// </summary>
    public void SetMovement(Vector2 direction)
    {
        // Cancel autonomous movement if manually controlled
        isMovingToTarget = false;
        targetPosition = null;

        movement = direction.normalized;
    }

    /// <summary>
    /// Set sprinting state for animation
    /// </summary>
    public void SetSprinting(bool isSprinting)
    {
        if (animator != null)
        {
            animator.SetBool(IsSprinting, isSprinting);
        }
        moveSpeed = isSprinting ? moveSpeed * 2 : moveSpeed / 2;
    }

    /// <summary>
    /// Set death state for animation
    /// </summary>
    public void SetDead(bool isDead)
    {
        if (animator != null)
        {
            animator.SetTrigger(IsDead);
            canMove = !isDead;
        }
    }
    
    /// <summary>
    /// Get current movement direction
    /// </summary>
    public Vector2 GetMovement()
    {
        return movement;
    }
    
    /// <summary>
    /// Get the last direction the villager was facing
    /// </summary>
    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize target position in editor
        if (isMovingToTarget && targetPosition.HasValue)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition.Value, 0.2f);
            Gizmos.DrawLine(transform.position, targetPosition.Value);
        }
        if (isAttacking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_hitboxPos, _hitboxsize);
        }
    }
}
