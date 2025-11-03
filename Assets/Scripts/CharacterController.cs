using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float stopDistance = 0.1f;
    public bool canMove = true;

    [Header("Animation")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected bool flipSpriteOnDirection = true;

    [Header("Attack Settings")]
    [SerializeField] protected Vector2 swordAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 spearAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 axeAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 swordAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected Vector2 spearAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected Vector2 axeAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected LayerMask attackTargetLayer;
    [SerializeField] public float attackDelay = 1f;

    protected Rigidbody2D rb;
    protected Vector2 movement;
    protected Vector2 lastMoveDirection = Vector2.down;
    protected float cachedMoveX = 0f;

    protected Vector2? targetPosition = null;
    protected bool isMovingToTarget = false;
    protected float lastAttackTime = 0f;
    protected bool isAttacking = false;

    protected Vector2 currentHitboxPos;
    protected Vector2 currentHitboxSize;
    protected Vector2 currentHitboxOffset;

    // Animation parameter hashes
    protected static readonly int MoveX = Animator.StringToHash("MoveX");
    protected static readonly int MoveY = Animator.StringToHash("MoveY");
    protected static readonly int IsMoving = Animator.StringToHash("IsMoving");
    protected static readonly int LastMoveX = Animator.StringToHash("LastMoveX");
    protected static readonly int LastMoveY = Animator.StringToHash("LastMoveY");
    protected static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
    protected static readonly int IsDead = Animator.StringToHash("IsDead");
    protected static readonly int AttackTrigger = Animator.StringToHash("Attack");
    protected static readonly int SwordAttackTrigger = Animator.StringToHash("SwordAttack");
    protected static readonly int SpearAttackTrigger = Animator.StringToHash("SpearAttack");
    protected static readonly int AxeAttackTrigger = Animator.StringToHash("AxeAttack");

    public EquipableItem weapon;
    public EquipableItem shield;
    public ItemAttachment itemAttachment;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemAttachment = GetComponent<ItemAttachment>();

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

    protected virtual void Update()
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

    protected virtual void FixedUpdate()
    {
        if (canMove)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    protected virtual void LateUpdate()
    {
        UpdateAnimations();
    }

    #region Movement

    /// <summary>
    /// Move the character to a specific position autonomously
    /// </summary>
    public virtual void MoveTo(Vector2 destination)
    {
        targetPosition = destination;
        isMovingToTarget = true;
    }

    /// <summary>
    /// Stop the character's current movement
    /// </summary>
    public virtual void Stop()
    {
        targetPosition = null;
        isMovingToTarget = false;
        movement = Vector2.zero;
    }

    /// <summary>
    /// Manually set movement direction
    /// </summary>
    public virtual void SetMovement(Vector2 direction)
    {
        isMovingToTarget = false;
        targetPosition = null;
        movement = direction.normalized;
    }

    /// <summary>
    /// Set the movement speed
    /// </summary>
    public virtual void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    /// <summary>
    /// Check if character is currently moving
    /// </summary>
    public bool ReturnIsMoving()
    {
        return movement.magnitude > 0.01f;
    }

    /// <summary>
    /// Get current movement direction
    /// </summary>
    public Vector2 GetMovement()
    {
        return movement;
    }

    /// <summary>
    /// Get the last direction the character was facing
    /// </summary>
    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }

    protected virtual void MoveToTarget()
    {
        if (!targetPosition.HasValue) return;

        Vector2 currentPos = rb.position;
        Vector2 targetPos = targetPosition.Value;

        float distance = Vector2.Distance(currentPos, targetPos);
        if (distance <= stopDistance)
        {
            Stop();
            return;
        }

        Vector2 direction = (targetPos - currentPos).normalized;
        movement = direction;
    }

    #endregion

    #region Combat

    /// <summary>
    /// Perform an attack
    /// </summary>
    public virtual void Attack()
    {
        if (Time.time - lastAttackTime < attackDelay) return;

        lastAttackTime = Time.time;

        if (itemAttachment != null)
        {
            currentHitboxSize = new Vector2(1f, 1f);

            if (weapon == null)
            {
                if (animator != null)
                {
                    animator.SetTrigger(AttackTrigger);
                }
            }
            else
            {
                // Trigger appropriate attack animation based on weapon type
                if (weapon.itemType == EquipableItem.ItemType.Sword)
                {
                    if (animator != null) animator.SetTrigger(SwordAttackTrigger);
                    currentHitboxSize = swordAttackSize;
                    currentHitboxOffset = swordAttackOffset;
                }
                else if (weapon.itemType == EquipableItem.ItemType.Spear)
                {
                    if (animator != null) animator.SetTrigger(SpearAttackTrigger);
                    currentHitboxSize = spearAttackSize;
                    currentHitboxOffset = spearAttackOffset;
                }
                else if (weapon.itemType == EquipableItem.ItemType.Axe)
                {
                    if (animator != null) animator.SetTrigger(AxeAttackTrigger);
                    currentHitboxSize = axeAttackSize;
                    currentHitboxOffset = axeAttackOffset;
                }
            }
        }
    }

    /// <summary>
    /// Perform the attack hitbox check - called by animation event
    /// </summary>
    public virtual void PerformAttackHitbox()
    {
        if (weapon == null) return;

        // Adjust hitbox based on facing direction
        Vector2 adjustedOffset = currentHitboxOffset;
        if (cachedMoveX < 0.01f)
        {
            adjustedOffset.x = -Math.Abs(adjustedOffset.x);
        }
        else if (cachedMoveX > 0.01f)
        {
            adjustedOffset.x = Math.Abs(adjustedOffset.x);
        }

        currentHitboxPos = (Vector2)transform.position + adjustedOffset;
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(currentHitboxPos, currentHitboxSize, 0f, attackTargetLayer);

        // Check if any have the same gameobject to avoid multiple hits
        HashSet<GameObject> hitGameObjects = new HashSet<GameObject>();


        foreach (var hit in hitObjects)
        {
            if(hit.gameObject == this.gameObject) continue;
            if (!hitGameObjects.Contains(hit.gameObject))
            {
                hitGameObjects.Add(hit.gameObject);
                OnHitTarget(hit);
            }
        }

        isAttacking = true;
    }

    /// <summary>
    /// Override this to handle what happens when hitting a target
    /// </summary>
    protected virtual void OnHitTarget(Collider2D hit)
    {
        var target = hit.GetComponent<TargetHealth>();
        if (target != null && weapon != null)
        {
            target.TakeDamage(weapon.damage);
        }
    }

    /// <summary>
    /// Called at the end of the attack animation via event
    /// </summary>
    public virtual void StopAttacking()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Check if character is currently attacking
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    #endregion

    #region Animation

    protected virtual void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = movement.magnitude > 0.01f;

        animator.SetFloat(MoveX, movement.x);
        animator.SetFloat(MoveY, movement.y);
        animator.SetBool(IsMoving, isMoving);

        if (isMoving)
        {
            lastMoveDirection = movement.normalized;
            animator.SetFloat(LastMoveX, lastMoveDirection.x);
            animator.SetFloat(LastMoveY, lastMoveDirection.y);

            if (lastMoveDirection.x != cachedMoveX && Math.Abs(lastMoveDirection.x) > 0.01f)
            {
                cachedMoveX = lastMoveDirection.x;
            }

            // Handle sprite flipping
            if (flipSpriteOnDirection && spriteRenderer != null)
            {
                if (movement.x > 0.01f)
                {
                    FlipSprite(false);
                }
                else if (movement.x < -0.01f)
                {
                    FlipSprite(true);
                }
            }
        }
    }

    protected virtual void FlipSprite(bool flip)
    {
        // Can be overridden for different flip methods
        transform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
    }

    /// <summary>
    /// Set sprinting state for animation
    /// </summary>
    public virtual void SetSprinting(bool isSprinting)
    {
        if (animator != null)
        {
            animator.SetBool(IsSprinting, isSprinting);
        }
    }

    /// <summary>
    /// Set death state for animation
    /// </summary>
    public virtual void SetDead(bool isDead)
    {
        if (animator != null)
        {
            animator.SetTrigger(IsDead);
        }
        canMove = !isDead;
        movement = Vector2.zero;
    }

    #endregion

    #region Debug

    protected virtual void OnDrawGizmosSelected()
    {
        // Visualize target position
        if (isMovingToTarget && targetPosition.HasValue)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition.Value, 0.2f);
            Gizmos.DrawLine(transform.position, targetPosition.Value);
        }

        // Visualize attack hitbox
        if (isAttacking)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(currentHitboxPos, currentHitboxSize);
        }
    }

    #endregion
}
