using System;
using System.Collections;
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
public class CharacterBase : MonoBehaviour
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

    [Header("Blocking")]
    public bool isBlocking = false;
    public bool isParrying = false;
    [SerializeField] protected float parryStunDuration = 1.5f;
    [SerializeField] private ParticleSystem stunEffect;

    [Header("AI Blocking")]
    public bool useReactiveBlocking = false;
    public int maxBlockCharges = 1;
    [SerializeField] private float blockCooldown = 5f;
    [Tooltip("How long a reactive block holds before auto-clearing if no hit lands. Should match the attack animation's windup duration.")]
    [SerializeField] protected float reactiveBlockDuration = 1.5f;
    private int currentBlockCharges;
    private float blockCooldownTimer;
    private bool isOnBlockCooldown;

    protected Rigidbody2D rb;
    protected Collider2D characterCollider;
    protected Vector2 movement;
    protected Vector2 lastMoveDirection = Vector2.down;
    protected float cachedMoveX = 0f;

    protected Vector2? targetPosition = null;
    protected bool isMovingToTarget = false;
    protected float lastAttackTime = 0f;
    protected bool isAttacking = false;

    // Roll
    [Header("Roll")]
    [SerializeField] protected float rollSpeed = 6f;
    [SerializeField] protected float rollDuration = 0.35f;
    [SerializeField] protected float rollCooldown = 1f;
    private float lastRollTime = -999f;
    public bool isRolling { get; private set; }

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
    protected static readonly int RollTrigger = Animator.StringToHash("Roll");

    [HideInInspector]
    public EquipableItem weapon;
    [HideInInspector]
    public EquipableItem shield;
    [HideInInspector]
    public EquipableItem torch;
    [HideInInspector]
    public ItemAttachment itemAttachment;
    [HideInInspector]
    public Vector2 lastAttackerPosition;
    [HideInInspector]
    public Faction characterFaction = Faction.Neutral;

    // Cache of valid animator parameter hashes to avoid errors
    private HashSet<int> validAnimatorParams;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        itemAttachment = GetComponent<ItemAttachment>();
        characterCollider = GetComponent<CircleCollider2D>();

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

        currentBlockCharges = maxBlockCharges;
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
        // Tick AI block cooldown regardless of movement state
        if (isOnBlockCooldown)
        {
            blockCooldownTimer -= Time.deltaTime;
            if (blockCooldownTimer <= 0f)
            {
                isOnBlockCooldown = false;
                currentBlockCharges = maxBlockCharges;
            }
        }

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
    /// Get move speed with skill bonuses and wound penalties applied.
    /// </summary>
    protected float GetEffectiveMoveSpeed()
    {
        float speed = moveSpeed;
        var villager = GetComponent<Villager>();

        if (characterFaction == Faction.Player && SkillTreeManager.Instance != null)
        {
            float speedBonus = SkillTreeManager.Instance.GetEffect(SkillEffectType.MoveSpeedPercent);
            speed *= (1f + speedBonus / 100f);
        }

        if (villager != null && villager.activeWounds.Count > 0)
        {
            float woundPenaltyPct = WoundDatabase.TotalMoveSpeedPenaltyPct(villager.activeWounds);
            speed *= (1f - woundPenaltyPct / 100f);
        }

        if (isBlocking)
            speed *= 0.5f;

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

    public bool CanRoll() => !isRolling && !isAttacking && canMove && Time.time - lastRollTime >= rollCooldown;

    public virtual void Roll(Vector2 direction)
    {
        if (!CanRoll()) return;
        if (direction == Vector2.zero) direction = lastMoveDirection;
        if (direction == Vector2.zero) return;

        lastRollTime = Time.time;
        StartCoroutine(RollCoroutine(direction.normalized));
    }

    private IEnumerator RollCoroutine(Vector2 direction)
    {
        isRolling = true;
        characterCollider.enabled = false; // Disable collisions during roll
        SafeSetTrigger(RollTrigger);

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            movement = direction * rollSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        movement = Vector2.zero;
        isRolling = false;
        characterCollider.enabled = true;
    }

    #endregion

    #region Combat

    /// <summary>
    /// Get the current attack delay (uses weapon's attackSpeed if equipped, otherwise default)
    /// Applies skill tree attack speed bonuses.
    /// </summary>
    public virtual float GetAttackDelay()
    {
        float baseDelay = weapon != null ? weapon.attackSpeed : attackDelay;
        var villager = GetComponent<Villager>();

        if (characterFaction == Faction.Player)
        {
            if (SkillTreeManager.Instance != null)
            {
                float speedBonus = SkillTreeManager.Instance.GetEffect(SkillEffectType.AttackSpeedPercent);
                if (speedBonus > 0)
                {
                    baseDelay *= (1f - speedBonus / 100f);
                    baseDelay = Mathf.Max(0.1f, baseDelay);
                }
            }

            if (RunestoneManager.Instance != null)
                baseDelay *= RunestoneManager.Instance.GetJarlCooldownMultiplier();

            if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive)
            {
                float cooldownPercent = DeathTypeBuff.Instance.GetJarlCooldownPercent();
                baseDelay *= (1f - cooldownPercent / 100f);
            }
        }

        // Wound attack speed penalties apply regardless of faction
        if (villager != null && villager.activeWounds.Count > 0)
        {
            float woundPenaltyPct = WoundDatabase.TotalAttackSpeedPenaltyPct(villager.activeWounds);
            baseDelay *= (1f + woundPenaltyPct / 100f);
        }

        return Mathf.Max(0.1f, baseDelay);
    }

    /// <summary>
    /// Check if attack is ready (cooldown has passed)
    /// </summary>
    public bool CanAttack()
    {
        if (isBlocking) return false;
        return Time.time - lastAttackTime >= GetAttackDelay();
    }

    /// <summary>
    /// Returns 0 when the attack was just fired, 1 when ready to attack again.
    /// </summary>
    public float GetAttackCooldownProgress()
    {
        return Mathf.Clamp01((Time.time - lastAttackTime) / GetAttackDelay());
    }

    /// <summary>
    /// Perform an attack
    /// </summary>
    public virtual void Attack()
    {
        if (!CanAttack()) return;

        lastAttackTime = Time.time;
        isAttacking = true;

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
                var hitController = hit.GetComponent<CharacterBase>();
                if (hitController != null && hitController.characterFaction == this.characterFaction)
                {
                    continue; // Skip friendly targets
                }
            }
            
            if (!hitGameObjects.Contains(hit.gameObject))
            {
                hitGameObjects.Add(hit.gameObject);
                // Refresh attacker position at the moment of impact
                var targetCC = hit.GetComponent<CharacterBase>();
                if (targetCC != null)
                    targetCC.lastAttackerPosition = (Vector2)transform.position;
                OnHitTarget(hit);
            }
        }

        // Improve combat skill on every swing, for all factions
        var villager = GetComponent<Villager>();
        if (villager != null)
        {
            villager.skills.ImproveSkill(JobType.Warrior);
            if (RaidManager.Instance != null && RaidManager.Instance.IsOnRaid)
                villager.skills.ImproveSkill(JobType.Warrior);
        }
    }

    /// <summary>
    /// Scans the attack area and notifies targets that an attack is incoming so they can
    /// raise their shield reactively. No damage is dealt here.
    /// Call this from an animation event at the START of the attack windup, before
    /// PerformAttackHitbox fires at the actual hit frame.
    /// </summary>
    public virtual void NotifyAttackWindup()
    {
        Vector2 adjustedOffset = currentHitboxOffset;
        if (cachedMoveX < 0.01f)
            adjustedOffset.x = -Math.Abs(adjustedOffset.x);
        else if (cachedMoveX > 0.01f)
            adjustedOffset.x = Math.Abs(adjustedOffset.x);

        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(
            (Vector2)transform.position + adjustedOffset, currentHitboxSize, 0f, attackTargetLayer);

        HashSet<GameObject> notified = new HashSet<GameObject>();
        foreach (var hit in hitObjects)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (notified.Contains(hit.gameObject)) continue;
            notified.Add(hit.gameObject);

            var targetCC = hit.GetComponent<CharacterBase>();
            if (targetCC != null)
            {
                targetCC.lastAttackerPosition = (Vector2)transform.position;
                targetCC.NotifyIncomingAttack(this);
            }
        }
    }

    /// <summary>
    /// Returns true if worldPos is on the opposite side to this character's facing direction.
    /// Uses transform.localScale.x as the facing ground-truth (set by FlipSprite).
    /// </summary>
    public bool IsAttackFromBehind(Vector2 worldPos)
    {
        float dx = worldPos.x - transform.position.x;
        bool facingRight = transform.localScale.x >= 0f;
        return facingRight ? dx < 0f : dx > 0f;
    }

    /// <summary>
    /// Override this to handle what happens when hitting a target
    /// </summary>
    protected virtual void OnHitTarget(Collider2D hit)
    {
        if (!GameManager.Instance.IsGameActive)
        {
            Debug.Log("Hit detected but game is not active, ignoring damage.");
            return; // Don't apply damage if game is not active (e.g. during scene transitions)
        }
        var target = hit.GetComponent<TargetHealth>();
        if (target == null || weapon == null) return;
        if (target.IsDead()) return;

        float damage = weapon.strength;
        var villager = GetComponent<Villager>();

        if (characterFaction == Faction.Player)
        {
            if (SkillTreeManager.Instance != null)
            {
                float damageBonus = SkillTreeManager.Instance.GetEffect(SkillEffectType.DamagePercent);
                damage *= (1f + damageBonus / 100f);

                float critChance = SkillTreeManager.Instance.GetEffect(SkillEffectType.CriticalChance);
                if (critChance > 0 && UnityEngine.Random.value * 100f < critChance)
                {
                    damage *= 2f;
                    Debug.Log("Critical hit!");
                }
            }

            if (RunestoneManager.Instance != null)
                damage *= RunestoneManager.Instance.GetWarriorDamageMultiplier();

            if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive)
            {
                float deathBuffDmg = DeathTypeBuff.Instance.GetWarriorDamagePercent();
                damage *= (1f + deathBuffDmg / 100f);
            }
        }

        // Wound damage penalties apply regardless of faction
        if (villager != null && villager.activeWounds.Count > 0)
        {
            float woundDmgPenaltyPct = WoundDatabase.TotalAttackDamagePenaltyPct(villager.activeWounds);
            damage *= (1f - woundDmgPenaltyPct / 100f);
        }

        target.TakeDamage(damage, weapon);

        // Life steal (Player only)
        if (characterFaction == Faction.Player && SkillTreeManager.Instance != null)
        {
            float lifeSteal = SkillTreeManager.Instance.GetEffect(SkillEffectType.LifeSteal);
            if (lifeSteal > 0)
            {
                var selfHealth = GetComponent<TargetHealth>();
                selfHealth?.Heal(damage * (lifeSteal / 100f));
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

    /// <summary>
    /// Called when an attack hitbox overlaps this character.
    /// If AI reactive blocking is enabled and charges are available, raises shield for this hit.
    /// </summary>
    public void NotifyIncomingAttack(CharacterBase attacker)
    {
        if (!useReactiveBlocking) return;
        if (!canMove) return;
        if (attacker.characterFaction == characterFaction) return;
        if (shield == null || shield.IsBroken) return;
        if (isOnBlockCooldown || currentBlockCharges <= 0) return;

        isBlocking = true;
        currentBlockCharges--;
        StartCoroutine(ClearBlockAfterDelay(reactiveBlockDuration));

        if (currentBlockCharges <= 0)
        {
            isOnBlockCooldown = true;
            blockCooldownTimer = GetEffectiveBlockCooldown();
        }
    }

    private IEnumerator ClearBlockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isBlocking = false;
    }

    /// <summary>
    /// Override in subclasses to scale block cooldown with character stats.
    /// </summary>
    protected virtual float GetEffectiveBlockCooldown() => blockCooldown;

    /// <summary>
    /// If the target was parrying when we hit them, stun ourselves. Call after TakeDamage.
    /// </summary>
    protected void CheckParryAndStun(Collider2D hit)
    {
        var targetCC = hit.GetComponent<CharacterBase>();
        if (targetCC != null && targetCC.isParrying
            && targetCC.shield != null && !targetCC.shield.IsBroken)
        {
            ApplyStun(parryStunDuration);
        }
    }

    /// <summary>
    /// Stun the character for duration seconds — used by parry and Heavy Strike.
    /// Prevents movement and attacks for the duration.
    /// </summary>
    public void ApplyStun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        canMove = false;
        isBlocking = false;
        isParrying = false;
        lastAttackTime = Time.time + duration;

        if (stunEffect != null)
            stunEffect.Play();

        if (animator != null)
            animator.SetBool("Stunned", true);

        yield return new WaitForSeconds(duration);

        if (stunEffect != null)
            stunEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (animator != null)
            animator.SetBool("Stunned", false);

        canMove = true;
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

            // Handle sprite flipping — locked while blocking so the shield always faces the attacker
            if (flipSpriteOnDirection && spriteRenderer != null && !isBlocking)
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
        var cached = transform.localScale;
        var x = Mathf.Abs(cached.x);
        transform.localScale = new Vector3(flip ? -x : x, cached.y, cached.z);
    }

    /// <summary>
    /// Immediately face towards a world position, updating sprite flip and cached direction used by hitboxes.
    /// </summary>
    public void FaceTowards(Vector2 worldPosition)
    {
        if (!flipSpriteOnDirection || spriteRenderer == null || isBlocking) return;

        float dx = worldPosition.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;

        bool facingLeft = dx < 0f;
        FlipSprite(facingLeft);

        cachedMoveX = facingLeft ? -1f : 1f;
        lastMoveDirection = new Vector2(cachedMoveX, lastMoveDirection.y);
        if (animator != null)
            animator.SetFloat(LastMoveX, lastMoveDirection.x);
    }

    private bool _isSprinting = false;

    /// <summary>
    /// Set sprinting state for animation
    /// </summary>
    public virtual void SetSprinting(bool isSprinting)
    {
        if (isSprinting == _isSprinting) return;
        _isSprinting = isSprinting;

        if (animator != null)
            animator.SetBool(IsSprinting, isSprinting);

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
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (weapon != null)
        {
            weapon.gameObject.SetActive(!isDead);
        }
        if (isDead)
            itemAttachment?.DropShield();
        else if (shield != null)
            shield.gameObject.SetActive(true);

        if (isDead && stunEffect != null)
        {
            StopCoroutine(nameof(StunCoroutine));
            stunEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
