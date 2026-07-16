using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Numeric values pinned explicitly: Player/Neutral keep their original values so any already-
// serialized prefab/scene data referencing them doesn't silently shift meaning. Enemy (1) is
// retired — no code depends on that generic bucket anymore, each hostile clan gets its own
// value instead, so e.g. Raider and Draugr are no longer forced into the same faction.
public enum Faction
{
    Player  = 0,
    Neutral = 2,
    Draugr  = 3,
    Raider1 = 4,
    Raider2 = 5
}

public enum FacingDirection { East, West, North, South }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class CharacterBase : MonoBehaviour
{
    [Header("Movement Settings")]
    protected float moveSpeed = 2f;
    public float sprintMod = 2;
    [SerializeField] protected float stopDistance = 0.1f;
    public bool canMove = true;
    private int _immobilizeCount = 0; // reference-counted; movement locked while > 0

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    [SerializeField] protected float stuckTimeout = 3f; // Give up after this long

    [Header("Pathfinding")]
    [SerializeField] protected int maxPathNodes = 10;
    [SerializeField] protected int maxRepathAttempts = 2; // stuck/partial-path retries before giving up via Stop()
    [SerializeField] protected float waypointArrivalRadius = 0.3f; // arrival radius for intermediate nodes — stopDistance still governs the final node
    [SerializeField] protected float minRepathInterval = 0.15f; // throttles full re-sweeps for callers that re-issue MoveTo() every frame with tiny nudges

    [Header("Animation")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected bool flipSpriteOnDirection = true;
    [SerializeField] protected bool use4DirectionalSprites = false;
    private bool _isDead;

    [Header("Attack Settings")]
    [SerializeField] protected Vector2 swordAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 spearAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 axeAttackSize = new Vector2(1f, 1f);
    [SerializeField] protected Vector2 swordAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected Vector2 spearAttackOffset = new Vector2(1f, 0f);
    [SerializeField] protected Vector2 axeAttackOffset = new Vector2(1f, 0f);
    [Tooltip("Extra vertical bias applied only when facing North/South. The sprite is anchored at the character's feet, so the lift needed to line up an up/down swing isn't the same amount as the offset.y lift used for East/West — this is a separately tunable value rather than reusing offset.x/y.")]
    [SerializeField] protected float swordVerticalAttackOffset = 0f;
    [SerializeField] protected float spearVerticalAttackOffset = 0f;
    [SerializeField] protected float axeVerticalAttackOffset = 0f;
    [SerializeField] public LayerMask attackTargetLayer;
    [SerializeField] public float attackDelay = 1f;
    public bool friendlyFire = false;

    [Header("Blocking")]
    public bool isBlocking = false;
    public bool isParrying = false;
    [SerializeField] protected float parryStunDuration = 1.5f;
    [SerializeField] private ParticleSystem stunEffect;

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

    private float _colliderRadius = 0.3f;

    // Path planning — a waypoint chain computed by RaySweepPathfinder and followed by
    // MoveToTarget(). See RaySweepPathfinder.cs for the algorithm.
    private List<Vector2> _currentPath = new List<Vector2>();
    private int _currentPathIndex = 0;
    private bool _pathReachedTarget = true;
    private int _repathAttempts = 0;
    private float _lastPathComputeTime = -999f;

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

    // Cached once in Awake — CharacterBase and CharacterAI always live on the same GameObject,
    // so this replaces scattered GetComponent<CharacterAI>() calls in hot per-frame combat code.
    [HideInInspector]
    public CharacterAI AI;
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
    // No longer hidden: previously always force-overwritten in code (VillagerController →
    // Player, EnemyController → Enemy) so exposing it was pointless — now that enemies keep
    // whatever faction is set here, it needs to be an actual Inspector-editable choice.
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
        var circleCollider = GetComponent<CircleCollider2D>();
        characterCollider = circleCollider;
        if (circleCollider != null)
            _colliderRadius = circleCollider.radius;
        AI = GetComponent<CharacterAI>();

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

        FacingOverride = Vector2.zero;
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
        // Several AI states re-issue MoveTo() every Update with a freshly recomputed destination
        // (e.g. VillagerFollowState tracking a moving target). Only reset stuck-detection when the
        // destination actually moved meaningfully — otherwise a character wedged against an
        // obstacle, fed a near-identical destination every frame, never accumulates stuck time and
        // can push against geometry indefinitely instead of ever giving up.
        bool isNewDestination = !targetPosition.HasValue || Vector2.Distance(targetPosition.Value, destination) > 0.1f;

        targetPosition = destination;
        isMovingToTarget = true;

        if (isNewDestination)
        {
            stuckTimer = 0f;
            lastPosition = rb != null ? rb.position : (Vector2)transform.position;
            _repathAttempts = 0;

            // Throttle: some callers (e.g. CharacterAI's push-fallback movement) re-issue MoveTo()
            // every frame with a small nudge on the destination. If the last full sweep is still
            // fresh AND the current path is a plain single-node direct line (no obstacle was
            // involved), just retarget that node instead of paying for a full re-sweep every frame.
            // A path that has real waypoints, or an elapsed throttle window, always gets recomputed.
            bool recentlyPlanned = Time.time - _lastPathComputeTime < minRepathInterval;
            if (recentlyPlanned && _currentPath.Count == 1)
            {
                _currentPath[0] = destination;
                _pathReachedTarget = true;
            }
            else
            {
                ComputePath(destination);
            }
        }
    }

    /// <summary>
    /// Computes (or recomputes) the waypoint chain to destination via RaySweepPathfinder and
    /// resets path-following state to walk it from the start.
    /// </summary>
    private void ComputePath(Vector2 destination)
    {
        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        bool reached = RaySweepPathfinder.TryFindPath(origin, destination, obstacleLayer, _colliderRadius,
            out List<Vector2> path, maxPathNodes, gameObject, waypointArrivalRadius);

        if (path != null && path.Count > 0)
        {
            _currentPath = path;
            _pathReachedTarget = reached;
        }
        else
        {
            // Total failure (e.g. spawned/knocked back into overlapping geometry, where every
            // candidate CircleCast reports an immediate hit) — fall back to a direct-line path and
            // let stuck-timeout/repath recover it.
            _currentPath = new List<Vector2> { destination };
            _pathReachedTarget = true;
        }
        _currentPathIndex = 0;
        _lastPathComputeTime = Time.time;
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
        _currentPath.Clear();
        _currentPathIndex = 0;
        _repathAttempts = 0;
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
    /// Sets movement directly at a fractional speed (0..1 of effective move speed), bypassing the
    /// MoveTo/isMovingToTarget pipeline entirely — used for arrival deceleration, where the caller
    /// already knows the real destination and doesn't need MoveToTarget's own stop/obstacle logic.
    /// </summary>
    public virtual void SetMovementScaled(Vector2 direction, float speedScale01)
    {
        isMovingToTarget = false;
        targetPosition = null;
        movement = direction.normalized * Mathf.Clamp01(speedScale01);
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

        // MoveTo() always seeds _currentPath — this only hits defensively (e.g. a subclass sets
        // targetPosition directly without going through MoveTo()).
        if (_currentPath == null || _currentPath.Count == 0)
        {
            _currentPath = new List<Vector2> { targetPosition.Value };
            _pathReachedTarget = true;
            _currentPathIndex = 0;
        }

        Vector2 currentPos = rb.position;

        // Advance through any waypoints already within arrival radius (handles closely-spaced
        // nodes and same-frame multi-arrival cleanly).
        while (true)
        {
            bool isLast = _currentPathIndex == _currentPath.Count - 1;
            float arrivalRadius = isLast ? stopDistance : waypointArrivalRadius;
            if (Vector2.Distance(currentPos, _currentPath[_currentPathIndex]) > arrivalRadius) break;

            if (isLast)
            {
                if (_pathReachedTarget)
                {
                    Stop();
                }
                else
                {
                    // Reached the end of a PARTIAL path without reaching the real destination —
                    // try again from here (bounded by maxRepathAttempts) rather than silently
                    // stopping short of the actual goal.
                    TryRepathOrGiveUp();
                }
                return;
            }
            _currentPathIndex++;
        }

        Vector2 waypoint = _currentPath[_currentPathIndex];

        // Stuck detection — unchanged mechanism, now the trigger for a bounded repath instead of
        // an immediate Stop().
        float movedDistance = Vector2.Distance(currentPos, lastPosition);
        if (movedDistance < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                TryRepathOrGiveUp();
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = currentPos;

        movement = (waypoint - currentPos).normalized;
    }

    /// <summary>
    /// Recomputes the path from the current live position, bounded by maxRepathAttempts before
    /// giving up via Stop() — covers both a stalled waypoint (dynamic obstacle blocking progress)
    /// and a partial path that ran out of nodes without reaching the real destination.
    /// </summary>
    private void TryRepathOrGiveUp()
    {
        _repathAttempts++;
        if (_repathAttempts > maxRepathAttempts || !targetPosition.HasValue)
        {
            Stop();
            return;
        }

        stuckTimer = 0f;
        lastPosition = rb.position;
        ComputePath(targetPosition.Value);
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
        characterCollider.excludeLayers = LayerMask.GetMask("Player", "Enemy");
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
        characterCollider.includeLayers = LayerMask.GetMask("Player", "Enemy");
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
        if(itemAttachment.weapon == null) return false;
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
                    currentHitboxOffset = RotateOffsetToFacing(swordAttackOffset, swordVerticalAttackOffset);
                }
                else if (weapon.itemType == EquipableItem.ItemType.Spear)
                {
                    SafeSetTrigger(SpearAttackTrigger);
                    currentHitboxSize   = RotateSizeToFacing(spearAttackSize);
                    currentHitboxOffset = RotateOffsetToFacing(spearAttackOffset, spearVerticalAttackOffset);
                }
                else if (weapon.itemType == EquipableItem.ItemType.Axe)
                {
                    SafeSetTrigger(AxeAttackTrigger);
                    currentHitboxSize   = RotateSizeToFacing(axeAttackSize);
                    currentHitboxOffset = RotateOffsetToFacing(axeAttackOffset, axeVerticalAttackOffset);
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
    /// Fired at the START of the attack windup, before PerformAttackHitbox fires at the actual
    /// hit frame. Combat AI states listen to this (via CombatAnimationListener) to react to an
    /// opponent's incoming swing — e.g. CombatPressureState transitioning to CombatBlockState.
    /// Call this from an animation event.
    /// </summary>
    public virtual void NotifyAttackWindup()
    {
        OnAttackWindupEvent?.Invoke();
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

    /// <summary>Fired when ApplyStun starts a stun window — AI subscribes to force a CombatStunnedState.</summary>
    public event Action<float> OnStunned;

    public bool IsStunned { get; private set; }

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
        IsStunned = true;
        Immobilize();
        isBlocking = false;
        isParrying = false;
        lastAttackTime = Time.time + duration;

        if (stunEffect != null)
            stunEffect.Play();

        if (animator != null)
            animator.SetBool("Stunned", true);

        OnStunned?.Invoke(duration);

        yield return new WaitForSeconds(duration);

        if (stunEffect != null)
            stunEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (animator != null)
            animator.SetBool("Stunned", false);

        Unimmobilize();
        IsStunned = false;
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
            Vector2 facing = lastMoveDirection;

            animator.SetFloat(LastMoveX, facing.x);
            animator.SetFloat(LastMoveY, facing.y);

            if (FacingOverride != Vector2.zero)
            {
                return; // Don't update facing direction if override is zero
            }

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

        // A player-controlled character's own AI is disabled (CharacterAI.SetAIEnabled(false) via
        // PlayerController.SetControlTarget), so its Update()/OnUpdate() never runs again — but
        // other characters' AI can still force it into CombatApproachState reciprocally
        // (TryForceReciprocalLock) when they target the player, whose OnEnter calls FaceTowards
        // once. With no further updates ever coming (and IdleState/VillagerFollowState, the only
        // places that clear FacingOverride, never reachable either), that single call would
        // permanently freeze the player's facing. The player's own movement input already drives
        // facing via the normal UpdateAnimations() path, so skip the override entirely here.
        if (AI != null && !AI.IsAIEnabled) return;

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
        FacingOverride = dir;
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

        moveSpeed = isSprinting ? moveSpeed * sprintMod : moveSpeed / sprintMod;
    }

    /// <summary>
    /// Set death state for animation
    /// </summary>
    public virtual void SetDead(bool isDead)
    {
        SafeSetTrigger(IsDead);
        _isDead = isDead;
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
            if (animator != null)
                animator.SetBool("Stunned", false);
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

    // Rotate a +East offset to match whatever direction we're currently facing. eastOffset.y is
    // a vertical lift used only for East/West (the sprite is feet-anchored, so the swing needs
    // lifting off the ground) — it doesn't carry over to North/South, since there the reach
    // itself (eastOffset.x) already becomes the vertical component. verticalFacingOffset is the
    // separate North/South equivalent: a fixed vertical bias (also compensating for the
    // feet-anchored origin) that the up/down reach is then added to or subtracted from.
    protected Vector2 RotateOffsetToFacing(Vector2 eastOffset, float verticalFacingOffset = 0f) => facingDirection switch
    {
        FacingDirection.East  => eastOffset,
        FacingDirection.West  => new Vector2(-eastOffset.x,  eastOffset.y),
        FacingDirection.North => new Vector2(0,  verticalFacingOffset + eastOffset.x),
        FacingDirection.South => new Vector2(0,  verticalFacingOffset - eastOffset.x),
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

        float newAngle = CalculateBisectAngle(claimer);
        _occupiedSlots.Add((claimer, newAngle));
        FightManager.Instance.NotifyOccupancyChanged(this);

        if (claimer.AI?.showDebug == true)
        {
            Debug.Log($"[{name}] Slot claimed by {claimer.name} " +
                $"at angle:{newAngle:F1}° " +
                $"existing slots:{_occupiedSlots.Count - 1} " +
                $"existing angles:{string.Join(", ", _occupiedSlots.Where(s => s.claimer != claimer).Select(s => s.angle.ToString("F1")))}");
        }

        slotWorldPos = GetSlotWorldPos(claimer);
        return true;
    }

    private float CalculateBisectAngle(CharacterBase claimer)
    {
        if (_occupiedSlots.Count == 0)
            return ComputeSlotAngleTo(claimer);


        var angles = _occupiedSlots.Select(s => s.angle).OrderBy(a => a).ToList();
        float largestGap = 0f;
        float gapStart = 0f;

        for (int i = 0; i < angles.Count; i++)
        {
            float next = angles[(i + 1) % angles.Count];
            // With a single occupant, (i+1)%count wraps back to the same element, so the
            // generic formula collapses to a 0° gap. The whole circle is actually free
            // (the lone occupant has zero angular width), so treat it as a full 360° gap —
            // this places the second claimant directly opposite the first.
            float gap = angles.Count == 1 ? 360f : (next - angles[i] + 360f) % 360f;
            if (gap > largestGap)
            {
                largestGap = gap;
                gapStart = angles[i];
            }
        }
        var value = (gapStart + largestGap / 2f) % 360f;
        return value;
    }

    /// <summary>
    /// Compass angle (unit-circle degrees, matching GetSlotWorldPos) from this character
    /// towards claimer's live position, snapped to one of the 4 facing directions.
    /// </summary>
    private float ComputeSlotAngleTo(CharacterBase claimer)
    {
        Vector2 dir = (Vector2)claimer.transform.position - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        var facing = ComputeFacingDirection(dir.normalized);
        var facingVector = FacingDirectionToVector(facing);
        return Mathf.Atan2(facingVector.y, facingVector.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Re-snaps every occupied slot's angle to the current live layout. The "main" occupant —
    /// whichever claimer this host is itself reciprocally engaged with (CurrentTarget) — always
    /// tracks its own live bearing from this host, same as the old single-occupant case. Every
    /// other ("extra") occupant is then defined purely as a fixed angular offset from that live
    /// main angle, evenly spread around the remaining arc, so extras follow along for free as
    /// the engaged pair circles each other, instead of each one independently re-bisecting
    /// against the others' shifting positions (which is what made live-tracking multi-occupant
    /// angles unstable before — see bug-history "second attacker's bisect angle" fix).
    /// </summary>
    public void UpdateSlotAngle(CharacterBase claimer)
    {
        if (_occupiedSlots.Count == 0) return;
        if (!_occupiedSlots.Any(s => s.claimer == claimer)) return;

        int mainIndex = _occupiedSlots.FindIndex(s => s.claimer == CurrentTarget);

        if (mainIndex < 0)
        {
            // No reciprocally-engaged occupant to anchor extras to yet. A lone occupant still
            // tracks its own live bearing regardless (matches the old single-occupant case);
            // with 2+ occupants and no main yet, leave claim-time angles alone until one of them
            // becomes genuinely engaged.
            if (_occupiedSlots.Count == 1)
                _occupiedSlots[0] = (_occupiedSlots[0].claimer, ComputeSlotAngleTo(_occupiedSlots[0].claimer));
            return;
        }

        float mainAngle = ComputeSlotAngleTo(_occupiedSlots[mainIndex].claimer);
        _occupiedSlots[mainIndex] = (_occupiedSlots[mainIndex].claimer, mainAngle);

        int extraCount = _occupiedSlots.Count - 1;
        if (extraCount <= 0) return;

        float step = 360f / (extraCount + 1);
        int slot = 0;
        for (int i = 0; i < _occupiedSlots.Count; i++)
        {
            if (i == mainIndex) continue;
            slot++;
            float angle = (mainAngle + step * slot) % 360f;
            _occupiedSlots[i] = (_occupiedSlots[i].claimer, angle);
        }
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
        // Guarded (unlike TryClaimSlot's call above): reachable from CharacterAI.OnDestroy() via
        // ReleaseEngagementSlot(), which can fire during scene teardown after FightManager's own
        // OnDestroy already ran — Instance would otherwise spin up a fresh one mid-unload.
        if (FightManager.Exists) FightManager.Instance.NotifyOccupancyChanged(this);
    }

    public void ReleaseAllSlots()
    {
        _occupiedSlots.Clear();
        if (FightManager.Exists) FightManager.Instance.NotifyOccupancyChanged(this);
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
        // Visualize the planned path — red is the current target waypoint, yellow are future
        // intermediate waypoints, green is a final node that actually reaches the real
        // destination (a yellow final node signals a partial path that will repath on arrival).
        if (_currentPath != null && _currentPath.Count > 0)
        {
            Vector2 prev = transform.position;
            for (int i = 0; i < _currentPath.Count; i++)
            {
                bool isCurrent = i == _currentPathIndex;
                bool isLast    = i == _currentPath.Count - 1;
                Gizmos.color = isCurrent ? Color.red : (isLast && _pathReachedTarget ? Color.green : Color.yellow);
                Gizmos.DrawLine(prev, _currentPath[i]);
                Gizmos.DrawWireSphere(_currentPath[i], isLast ? 0.2f : 0.12f);
                prev = _currentPath[i];
            }
        }
        else if (isMovingToTarget && targetPosition.HasValue)
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
    }

    #endregion
}
