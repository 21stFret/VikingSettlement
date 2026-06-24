using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Faction
{
    Player,
    Enemy,
    Neutral
}

public enum FacingDirection { East, West, North, South }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class CharacterBase : MonoBehaviour
{
    [Header("Movement Settings")]
    protected float moveSpeed = 2f;
    [SerializeField] protected float stopDistance = 0.1f;
    public bool canMove = true;
    private int _immobilizeCount = 0; // reference-counted; movement locked while > 0

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    [SerializeField] protected float obstacleCheckDistance = 0.8f;
    [SerializeField] protected float stuckTimeout = 3f; // Give up after this long

    [Header("Animation")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected bool flipSpriteOnDirection = true;
    [SerializeField] protected bool use4DirectionalSprites = false;

    [Header("Attack Settings")]
    [SerializeField] protected Vector2 swordAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 spearAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 axeAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 swordAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected Vector2 spearAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected Vector2 axeAttackOffset = new Vector2(1f, 0f);
    [SerializeField] public LayerMask attackTargetLayer;
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
    [HideInInspector]
    public FacingDirection facingDirection = FacingDirection.South;
    [HideInInspector]
    public CharacterBase CurrentTarget;

    [Header("Combat Slots")]
    [SerializeField] public float slotDistance = 0.8f;
    [SerializeField] public int MaxAttackers = 4;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.1f;
    private List<(CharacterBase claimer, float angle)> _occupiedSlots = new List<(CharacterBase, float)>();

    // Combat events fired by animation event callbacks — subscribe to observe attack phases
    public event Action OnAttackWindupEvent;
    public event Action OnAttackWindowEvent;
    public event Action OnAttackRecoveryEvent;
    public event Action<CharacterBase> OnHitByAttacker;

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
                // Hitbox size and offset are rotated to match current facing direction
                if (weapon.itemType == EquipableItem.ItemType.Sword)
                {
                    SafeSetTrigger(SwordAttackTrigger);
                    currentHitboxSize   = RotateSizeToFacing(swordAttackSize);
                    currentHitboxOffset = RotateOffsetToFacing(swordAttackOffset);
                }
                else if (weapon.itemType == EquipableItem.ItemType.Spear)
                {
                    SafeSetTrigger(SpearAttackTrigger);
                    currentHitboxSize   = RotateSizeToFacing(spearAttackSize);
                    currentHitboxOffset = RotateOffsetToFacing(spearAttackOffset);
                }
                else if (weapon.itemType == EquipableItem.ItemType.Axe)
                {
                    SafeSetTrigger(AxeAttackTrigger);
                    currentHitboxSize   = RotateSizeToFacing(axeAttackSize);
                    currentHitboxOffset = RotateOffsetToFacing(axeAttackOffset);
                }
            }
        }
    }

    /// <summary>
    /// Perform the attack hitbox check - called by animation event
    /// </summary>
    public virtual void PerformAttackHitbox()
    {
        OnAttackWindowEvent?.Invoke();
        if (weapon == null) return;

        // Offset is already rotated to facing direction — set when the swing was committed in Attack()
        currentHitboxPos = (Vector2)transform.position + currentHitboxOffset;
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

                // Attacking a blocking character bounces the attacker back
                if (targetCC != null && (targetCC.isBlocking || targetCC.isParrying))
                    ApplyKnockback(this, hit.transform.position);

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
        OnAttackWindupEvent?.Invoke();

        // Offset is already rotated to facing direction — committed in Attack()
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(
            (Vector2)transform.position + currentHitboxOffset, currentHitboxSize, 0f, attackTargetLayer);

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
    /// Returns true if worldPos is in the hemisphere behind this character's current facing direction.
    /// </summary>
    public bool IsAttackFromBehind(Vector2 worldPos)
    {
        Vector2 toAttacker = (worldPos - (Vector2)transform.position).normalized;
        return Vector2.Dot(toAttacker, FacingDirectionToVector(facingDirection)) < 0f;
    }

    /// <summary>
    /// Called when this character is hit by an attacker. Override to react to being hit.
    /// </summary>
    public virtual void OnHitBy(CharacterBase attacker)
    {
        OnHitByAttacker?.Invoke(attacker);
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

        // Notify the target who hit them (used by EnemyAI retargetOnHit, etc.)
        hit.GetComponent<CharacterBase>()?.OnHitBy(this);

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
        OnAttackRecoveryEvent?.Invoke();
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
        Immobilize();
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

        Unimmobilize();
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
            Vector2 facing = FacingOverride ?? lastMoveDirection;

            animator.SetFloat(LastMoveX, facing.x);
            animator.SetFloat(LastMoveY, facing.y);

            if (facing.x != cachedMoveX && Math.Abs(facing.x) > 0.01f)
                cachedMoveX = facing.x;

            facingDirection = ComputeFacingDirection(facing);

            // Handle sprite flipping — locked while blocking so the shield always faces the attacker
            // Skipped in 4D mode (animator blend tree handles direction visually)
            if (flipSpriteOnDirection && !use4DirectionalSprites && spriteRenderer != null && !isBlocking)
            {
                if (facing.x > 0.01f)
                    FlipSprite(false);
                else if (facing.x < -0.01f)
                    FlipSprite(true);
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
    /// Public facing setter — routes through FlipSprite so subclass overrides stay consistent.
    /// </summary>
    public void SetFacingRight(bool faceRight) => FlipSprite(!faceRight);

    /// <summary>
    /// When set, overrides the visual facing direction (animator LastMoveX/Y, sprite flip)
    /// without changing physical movement. Clear by setting null. Used for backpedalling.
    /// </summary>
    public Vector2? FacingOverride { get; set; }

    /// <summary>
    /// Immediately face towards a world position, updating all direction state used by hitboxes and animations.
    /// </summary>
    public void FaceTowards(Vector2 worldPosition)
    {
        if (isBlocking) return;

        Vector2 dir = (worldPosition - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;

        facingDirection = ComputeFacingDirection(dir);
        lastMoveDirection = dir;

        if (animator != null)
        {
            animator.SetFloat(LastMoveX, dir.x);
            animator.SetFloat(LastMoveY, dir.y);
        }

        if (flipSpriteOnDirection && !use4DirectionalSprites && spriteRenderer != null)
        {
            bool left = facingDirection == FacingDirection.West;
            FlipSprite(left);
            cachedMoveX = left ? -1f : 1f;
        }
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
        _immobilizeCount = 0; // clear any in-flight immobilization
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

    #region Facing Direction

    protected FacingDirection ComputeFacingDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return dir.x >= 0f ? FacingDirection.East : FacingDirection.West;
        return dir.y >= 0f ? FacingDirection.North : FacingDirection.South;
    }

    public Vector2 FacingDirectionToVector(FacingDirection dir) => dir switch
    {
        FacingDirection.East  => Vector2.right,
        FacingDirection.West  => Vector2.left,
        FacingDirection.North => Vector2.up,
        _                     => Vector2.down
    };

    // Rotate a +East offset to match whatever direction we're currently facing.
    protected Vector2 RotateOffsetToFacing(Vector2 eastOffset) => facingDirection switch
    {
        FacingDirection.East  => eastOffset,
        FacingDirection.West  => new Vector2(-eastOffset.x,  eastOffset.y),
        FacingDirection.North => new Vector2( eastOffset.y,  eastOffset.x),
        FacingDirection.South => new Vector2( eastOffset.y, -eastOffset.x),
        _                     => eastOffset
    };

    // Swap width/height when the attack is going vertically.
    protected Vector2 RotateSizeToFacing(Vector2 size)
    {
        bool vertical = facingDirection == FacingDirection.North || facingDirection == FacingDirection.South;
        return vertical ? new Vector2(size.y, size.x) : size;
    }

    #endregion

    #region Combat Slots

    public int OccupiedCount => _occupiedSlots.Count;

    public bool TryClaimSlot(CharacterBase claimer, out Vector2 slotWorldPos)
    {
        ReleaseSlot(claimer);

        if (_occupiedSlots.Count >= MaxAttackers)
        {
            slotWorldPos = Vector2.zero;
            return false;
        }

        float newAngle = CalculateBisectAngle();
        _occupiedSlots.Add((claimer, newAngle));
        slotWorldPos = GetSlotWorldPos(claimer);
        return true;
    }

    private float CalculateBisectAngle()
    {
        if (_occupiedSlots.Count == 0)
            return UnityEngine.Random.Range(0f, 360f);

        var angles = _occupiedSlots.Select(s => s.angle).OrderBy(a => a).ToList();
        float largestGap = 0f;
        float gapStart = 0f;

        for (int i = 0; i < angles.Count; i++)
        {
            float next = angles[(i + 1) % angles.Count];
            float gap = (next - angles[i] + 360f) % 360f;
            if (gap > largestGap)
            {
                largestGap = gap;
                gapStart = angles[i];
            }
        }
        return (gapStart + largestGap / 2f) % 360f;
    }

    public Vector2 GetSlotWorldPos(CharacterBase claimer)
    {
        foreach (var slot in _occupiedSlots)
        {
            if (slot.claimer == claimer)
            {
                float rad = slot.angle * Mathf.Deg2Rad;
                return (Vector2)transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * slotDistance;
            }
        }
        return transform.position;
    }

    public void ReleaseSlot(CharacterBase claimer)
    {
        _occupiedSlots.RemoveAll(s => s.claimer == claimer);
    }

    public void ReleaseAllSlots()
    {
        _occupiedSlots.Clear();
    }

    #endregion

    #region Knockback

    // Reference-counted movement lock — safe to call from overlapping coroutines
    private void Immobilize()
    {
        _immobilizeCount++;
        canMove = false;
    }

    private void Unimmobilize()
    {
        _immobilizeCount = Mathf.Max(0, _immobilizeCount - 1);
        if (_immobilizeCount == 0)
            canMove = true;
    }

    private void ApplyKnockback(CharacterBase target, Vector2 sourcePosition)
    {
        var targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null) return;

        Vector2 dir = ((Vector2)target.transform.position - sourcePosition).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = FacingDirectionToVector(facingDirection);

        // Run on target so the coroutine survives if the attacker is destroyed mid-flight
        target.StartCoroutine(target.KnockbackCoroutine(targetRb, knockbackForce, knockbackDuration, dir));
    }

    internal System.Collections.IEnumerator KnockbackCoroutine(Rigidbody2D rb, float force, float duration, Vector2 direction)
    {
        Immobilize();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            rb.MovePosition(rb.position + direction * force * Time.fixedDeltaTime);
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        Unimmobilize();
    }

    #endregion

    #region Debug

    private static readonly Color[] SlotColors = { Color.yellow, Color.magenta, Color.cyan, Color.green };

    private void OnDrawGizmos()
    {
        if (_occupiedSlots == null || _occupiedSlots.Count == 0) return;

        for (int i = 0; i < _occupiedSlots.Count; i++)
        {
            var (claimer, angle) = _occupiedSlots[i];
            float rad      = angle * Mathf.Deg2Rad;
            Vector2 slotWP = (Vector2)transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * slotDistance;

            Gizmos.color = SlotColors[i % SlotColors.Length];
            Gizmos.DrawWireSphere(slotWP, 0.15f);
            Gizmos.DrawLine(transform.position, slotWP);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(slotWP + Vector2.up * 0.2f,
                $"{(claimer ? claimer.name : "?")} ({angle:F0}°)");
#endif
        }
    }

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

        // Draw facing direction arrow
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + FacingDirectionToVector(facingDirection) * 0.5f);

        // Visualize obstacle check distance
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
        Gizmos.DrawWireSphere(transform.position, obstacleCheckDistance);
    }

    #endregion
}
