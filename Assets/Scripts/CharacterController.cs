using System;
using System.Collections.Generic;
using UnityEngine;

public enum Faction
{
    Player,
    Enemy,
    Neutral
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    protected float moveSpeed = 2f;
    [SerializeField] protected float stopDistance = 0.1f;
    public bool canMove = true;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    [SerializeField] protected float obstacleCheckDistance = 0.8f;
    [SerializeField] protected float stuckTimeout = 3f; // Give up after this long

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
    public bool friendlyFire = false;

    protected Rigidbody2D rb;
    protected Collider2D characterCollider;
    protected Vector2 movement;
    protected Vector2 lastMoveDirection = Vector2.down;
    protected float cachedMoveX = 0f;

    protected Vector2? targetPosition = null;
    protected bool isMovingToTarget = false;
    protected float lastAttackTime = 0f;
    protected bool isAttacking = false;

    // Stuck detection
    protected Vector2 lastPosition;
    protected float stuckTimer = 0f;

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

    public Faction characterFaction = Faction.Neutral;

    // Cache of valid animator parameter hashes to avoid errors
    private HashSet<int> validAnimatorParams;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemAttachment = GetComponent<ItemAttachment>();
        characterCollider = GetComponent<Collider2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Cache valid animator parameters
        CacheAnimatorParameters();

        // Initialize stuck detection
        lastPosition = transform.position;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// Cache valid animator parameters to avoid errors when setting triggers
    /// </summary>
    private void CacheAnimatorParameters()
    {
        validAnimatorParams = new HashSet<int>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var param in animator.parameters)
            {
                validAnimatorParams.Add(param.nameHash);
            }
        }
    }

    /// <summary>
    /// Safely set an animator trigger - does nothing if parameter doesn't exist
    /// </summary>
    protected void SafeSetTrigger(int triggerHash)
    {
        if (animator != null && validAnimatorParams != null && validAnimatorParams.Contains(triggerHash))
        {
            animator.SetTrigger(triggerHash);
        }
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
            float effectiveSpeed = GetEffectiveMoveSpeed();
            rb.MovePosition(rb.position + movement * effectiveSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Get move speed with skill bonuses applied.
    /// </summary>
    protected float GetEffectiveMoveSpeed()
    {
        float speed = moveSpeed;

        if (characterFaction == Faction.Player && SkillTreeManager.Instance != null)
        {
            float speedBonus = SkillTreeManager.Instance.GetEffect(SkillEffectType.MoveSpeedPercent);
            speed *= (1f + speedBonus / 100f);
        }

        return speed;
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
        stuckTimer = 0f;
        lastPosition = rb != null ? rb.position : (Vector2)transform.position;
    }

    /// <summary>
    /// Stop the character's current movement
    /// </summary>
    public virtual void Stop()
    {
        targetPosition = null;
        isMovingToTarget = false;
        movement = Vector2.zero;
        stuckTimer = 0f;
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

        // Check if stuck (not making progress)
        float movedDistance = Vector2.Distance(currentPos, lastPosition);
        if (movedDistance < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                Stop();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = currentPos;

        // Get direction to target
        Vector2 moveDir = (targetPos - currentPos).normalized;

        // Check for obstacle directly ahead
        RaycastHit2D hit = Physics2D.Raycast(currentPos, moveDir, obstacleCheckDistance, obstacleLayer);
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            // Obstacle ahead - try to go around
            Vector2 leftDir = new Vector2(-moveDir.y, moveDir.x); // Perpendicular left
            Vector2 rightDir = new Vector2(moveDir.y, -moveDir.x); // Perpendicular right

            // Check which side is clearer
            RaycastHit2D leftHit = Physics2D.Raycast(currentPos, leftDir, obstacleCheckDistance, obstacleLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(currentPos, rightDir, obstacleCheckDistance, obstacleLayer);

            bool leftClear = leftHit.collider == null || leftHit.collider.gameObject == gameObject;
            bool rightClear = rightHit.collider == null || rightHit.collider.gameObject == gameObject;

            if (leftClear && !rightClear)
            {
                moveDir = (moveDir + leftDir).normalized;
            }
            else if (rightClear && !leftClear)
            {
                moveDir = (moveDir + rightDir).normalized;
            }
            else if (leftClear && rightClear)
            {
                // Both clear, pick one based on which is closer to target direction
                float leftDot = Vector2.Dot(leftDir, (targetPos - currentPos).normalized);
                float rightDot = Vector2.Dot(rightDir, (targetPos - currentPos).normalized);
                moveDir = (moveDir + (leftDot > rightDot ? leftDir : rightDir)).normalized;
            }
            // If both blocked, just keep trying forward
        }

        movement = moveDir;
    }

    #endregion

    #region Combat

    /// <summary>
    /// Get the current attack delay (uses weapon's attackSpeed if equipped, otherwise default)
    /// Applies skill tree attack speed bonuses.
    /// </summary>
    public float GetAttackDelay()
    {
        float baseDelay = weapon != null ? weapon.attackSpeed : attackDelay;

        // Apply attack speed skill bonus (reduces delay)
        if (characterFaction == Faction.Player && SkillTreeManager.Instance != null)
        {
            float speedBonus = SkillTreeManager.Instance.GetEffect(SkillEffectType.AttackSpeedPercent);
            if (speedBonus > 0)
            {
                baseDelay *= (1f - speedBonus / 100f);
                baseDelay = Mathf.Max(0.1f, baseDelay); // Minimum delay cap
            }
        }

        return baseDelay;
    }

    /// <summary>
    /// Check if attack is ready (cooldown has passed)
    /// </summary>
    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= GetAttackDelay();
    }

    /// <summary>
    /// Perform an attack
    /// </summary>
    public virtual void Attack()
    {
        if (!CanAttack()) return;

        lastAttackTime = Time.time;

        if (itemAttachment != null)
        {
            currentHitboxSize = new Vector2(1f, 1f);

            if (weapon == null)
            {
                SafeSetTrigger(AttackTrigger);
            }
            else
            {
                // Trigger appropriate attack animation based on weapon type
                if (weapon.itemType == EquipableItem.ItemType.Sword)
                {
                    SafeSetTrigger(SwordAttackTrigger);
                    currentHitboxSize = swordAttackSize;
                    currentHitboxOffset = swordAttackOffset;
                }
                else if (weapon.itemType == EquipableItem.ItemType.Spear)
                {
                    SafeSetTrigger(SpearAttackTrigger);
                    currentHitboxSize = spearAttackSize;
                    currentHitboxOffset = spearAttackOffset;
                }
                else if (weapon.itemType == EquipableItem.ItemType.Axe)
                {
                    SafeSetTrigger(AxeAttackTrigger);
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
            if (hit.gameObject == this.gameObject) continue;

            if (!friendlyFire)
            {
                var hitController = hit.GetComponent<CharacterController>();
                if (hitController != null && hitController.characterFaction == this.characterFaction)
                {
                    continue; // Skip friendly targets
                }
            }
            
            if (!hitGameObjects.Contains(hit.gameObject))
            {
                hitGameObjects.Add(hit.gameObject); // Mark this gameobject as hit
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
            float damage = weapon.strength;

            // Apply skill bonuses for player faction
            if (characterFaction == Faction.Player && SkillTreeManager.Instance != null)
            {
                // Damage bonus
                float damageBonus = SkillTreeManager.Instance.GetEffect(SkillEffectType.DamagePercent);
                damage *= (1f + damageBonus / 100f);

                // Critical hit
                float critChance = SkillTreeManager.Instance.GetEffect(SkillEffectType.CriticalChance);
                if (critChance > 0 && UnityEngine.Random.value * 100f < critChance)
                {
                    damage *= 2f;
                    Debug.Log("Critical hit!");
                }
            }

            target.TakeDamage(damage, weapon);

            // Life steal
            if (characterFaction == Faction.Player && SkillTreeManager.Instance != null)
            {
                float lifeSteal = SkillTreeManager.Instance.GetEffect(SkillEffectType.LifeSteal);
                if (lifeSteal > 0)
                {
                    float healAmount = damage * (lifeSteal / 100f);
                    var selfHealth = GetComponent<TargetHealth>();
                    if (selfHealth != null)
                    {
                        selfHealth.Heal(healAmount);
                    }
                }
            }
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
        moveSpeed = isSprinting ? moveSpeed * 1.5f : moveSpeed / 1.5f;
    }

    /// <summary>
    /// Set death state for animation
    /// </summary>
    public virtual void SetDead(bool isDead)
    {
        SafeSetTrigger(IsDead);
        canMove = !isDead;
        movement = Vector2.zero;
        characterCollider.enabled = !isDead;
        if (weapon != null)
        {
            weapon.gameObject.SetActive(!isDead);
        }
        if (shield != null)
        {
            shield.gameObject.SetActive(!isDead);
        }
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

        // Visualize obstacle check distance
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
        Gizmos.DrawWireSphere(transform.position, obstacleCheckDistance);
    }

    #endregion
}
