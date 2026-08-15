using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Input Settings")]
    [SerializeField] private bool useMouseMovement = false; // Toggle between WASD and click-to-move
    public float playerMoveSpeed = 3f;

    [Header("Control Target")]
    [SerializeField] private Villager controlTarget;

    private CharacterBase characterBase;
    private VillagerAIBase targetAI;
    private WeaponSwapper weaponSwapper;
    private Vector2 moveInput;
    private bool inputEnabled = true;
    private bool isAttackHeld = false;
    private bool isBlockHeld = false;
    private float blockPressTime = -999f;
    [SerializeField] private float parryWindowDuration = 0.3f;

    [Header("Shield Throw")]
    [SerializeField] private float throwSpeed = 12f;
    [SerializeField] private float throwRange = 8f;
    [SerializeField] private float throwCooldown = 5f;
    private float lastThrowTime = -999f;

    // Shield wall
    private bool _shieldWallActive = false;
    private readonly System.Collections.Generic.List<VillagerAIBase> _raidAllies
        = new System.Collections.Generic.List<VillagerAIBase>();
    [Header("Shield Wall")]
    [Tooltip("World-space gap between each villager in the formation.")]
    [SerializeField] private float wallSlotSpacing = 1f;

    // Input System
    public PlayerInputActions inputActions;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Setup Input System
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Sprint.performed += OnSprint;
        inputActions.Player.Sprint.canceled += OnSprint;
        inputActions.Player.Click.performed += OnClick;
        inputActions.Player.StopMove.performed += OnStopMove;
        inputActions.Player.Attack.performed += OnAttack;
        inputActions.Player.Attack.canceled += OnAttackReleased;
        inputActions.Player.Block.performed += OnBlock;
        inputActions.Player.Block.canceled += OnBlockReleased;
        inputActions.Player.ShieldWall.performed += OnShieldWall;
        inputActions.Player.SwapWeapon.performed += OnSwapWeapon;
        inputActions.Player.Roll.performed += OnRoll;
        inputActions.Player.ThrowShield.performed += OnThrowShield;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Sprint.performed -= OnSprint;
        inputActions.Player.Sprint.canceled -= OnSprint;
        inputActions.Player.Click.performed -= OnClick;
        inputActions.Player.StopMove.performed -= OnStopMove;
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Player.Attack.canceled -= OnAttackReleased;
        inputActions.Player.Block.performed -= OnBlock;
        inputActions.Player.Block.canceled -= OnBlockReleased;
        inputActions.Player.ShieldWall.performed -= OnShieldWall;
        inputActions.Player.SwapWeapon.performed -= OnSwapWeapon;
        inputActions.Player.Roll.performed -= OnRoll;
        inputActions.Player.ThrowShield.performed -= OnThrowShield;
        inputActions.Disable();
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        if (!inputEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null) return;
        if (!useMouseMovement) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(inputActions.Player.MousePosition.ReadValue<Vector2>());
        characterBase.MoveTo(mousePos);
    }

    private void OnStopMove(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null) return;
        if (useMouseMovement)
        {
            characterBase.Stop();
        }
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null) return;
        if (context.performed)
        {
            characterBase.SetSprinting(true);
        }
        else if (context.canceled)
        {
            characterBase.SetSprinting(false);
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null) return;
        if (context.performed)
        {
            isAttackHeld = true;
            characterBase.Attack();
        }
    }

    private void OnAttackReleased(InputAction.CallbackContext context)
    {
        isAttackHeld = false;
    }

    private void OnBlock(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null) return;
        blockPressTime = Time.time;
        isBlockHeld = true;
        characterBase.animator.SetBool("Blocking", true);
    }

    private void OnBlockReleased(InputAction.CallbackContext context)
    {
        isBlockHeld = false;
        characterBase.animator.SetBool("Blocking", false);
    }

    private void OnShieldWall(InputAction.CallbackContext context)
    {
        if (!inputEnabled) return;
        if (_shieldWallActive)
            DeactivateShieldWall();
        else
            ActivateShieldWall();
    }

    private void OnSwapWeapon(InputAction.CallbackContext context)
    {
        if (!inputEnabled || weaponSwapper == null) return;
        weaponSwapper.SwapToNext();
    }

    private void OnRoll(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null) return;
        Vector2 rollDir = moveInput.magnitude > 0.1f ? moveInput.normalized : characterBase.GetLastMoveDirection();
        characterBase.Roll(rollDir);
    }

    private void OnThrowShield(InputAction.CallbackContext context)
    {
        if (!inputEnabled || characterBase == null || controlTarget == null) return;
        if (characterBase.shield == null || characterBase.shield.IsBroken) return;
        if (Time.time - lastThrowTime < throwCooldown) return;

        lastThrowTime = Time.time;

        GameObject shieldGO = characterBase.shield.gameObject;
        EquipableItem shieldItem = characterBase.shield;

        // Detach from the character, clearing all equipped state
        controlTarget.itemAttachment.DropShield();

        // Keep isEquipped true so the pickup scanner ignores the shield while it's in flight
        shieldItem.isEquipped = true;

        Vector2 throwDir = moveInput.magnitude > 0.1f ? moveInput.normalized : characterBase.GetLastMoveDirection();

        var thrower = shieldGO.AddComponent<ShieldThrow>();
        thrower.Launch(throwDir, throwSpeed, throwRange, characterBase);
    }

    private void Update()
    {
        // Skip if we don't have a valid controller
        if (characterBase == null)
        {
            return;
        }

        // Block — driven by the Block input action (right mouse / gamepad right trigger)
        {
            bool hasShield = characterBase.shield != null && !characterBase.shield.IsBroken;
            bool inParryWindow = inputEnabled && hasShield && (Time.time - blockPressTime) < parryWindowDuration;

            characterBase.isParrying = inParryWindow;
            if (_shieldWallActive)
            {
                // Shield wall: player holds shield permanently, no parrying
                characterBase.isParrying = false;
                characterBase.isBlocking = hasShield;
            }
            else
            {
                characterBase.isBlocking = inParryWindow || (inputEnabled && hasShield && isBlockHeld);
            }
        }

        // Movement (50% speed while blocking is handled inside GetEffectiveMoveSpeed)
        if (!useMouseMovement && inputEnabled)
        {
            bool movementLocked = characterBase.IsAttacking() || characterBase.isRolling;
            characterBase.SetMovement(movementLocked ? Vector2.zero : moveInput * playerMoveSpeed);
        }
        else if (!inputEnabled)
        {
            // Stop movement when input is disabled
            characterBase.SetMovement(Vector2.zero);
        }

        // Handle held attack - continue attacking while button is held
        if (isAttackHeld && inputEnabled && characterBase.CanAttack())
        {
            characterBase.Attack();
        }
    }
    
    /// <summary>
    /// Toggle between keyboard and mouse movement
    /// </summary>
    public void SetMouseMovement(bool enabled)
    {
        useMouseMovement = enabled;
        if (!enabled && characterBase != null)
        {
            characterBase.Stop();
        }
    }
    
    /// <summary>
    /// Get reference to the underlying CharacterBase
    /// </summary>
    public CharacterBase GetController()
    {
        return characterBase;
    }

    /// <summary>
    /// Set the villager to control (for succession and Jarl switching)
    /// </summary>
    public void SetControlTarget(Villager target)
    {
        if (target == null)
        {
            Debug.LogError("Cannot set null control target!");
            return;
        }

        // Clear block state on old target — reactive blocking resumes automatically once its AI re-enables
        if (characterBase != null)
        {
            characterBase.isBlocking = false;
            characterBase.isParrying = false;
            characterBase.Stop();
        }

        // Set new target
        controlTarget = target;
        characterBase = target.GetComponent<CharacterBase>();
        var allAIs = target.GetComponents<VillagerAIBase>();
        targetAI = System.Array.Find(allAIs, ai => ai.enabled) ?? (allAIs.Length > 0 ? allAIs[0] : null);
        weaponSwapper = target.GetComponent<WeaponSwapper>();

        if (characterBase == null)
        {
            Debug.LogError($"Control target {target.villagerName} has no CharacterBase!");
            return;
        }

        // Disable AI on new target (player controls this villager)
        if (targetAI != null)
        {
            targetAI.SetAIEnabled(false);
        }

        // Update camera to follow new target (and update playerTarget reference)
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerTarget(target.transform);
        }

        Debug.Log($"Player control transferred to {target.villagerName}");
    }

    /// <summary>
    /// Get the currently controlled villager
    /// </summary>
    public Villager GetControlTarget()
    {
        return controlTarget;
    }

    /// <summary>
    /// Enable or disable player input (for pause states)
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            // Clear movement and stop character when disabling input
            moveInput = Vector2.zero;
            isAttackHeld = false;
            isBlockHeld = false;
            if (characterBase != null)
            {
                characterBase.SetMovement(Vector2.zero);
                characterBase.SetSprinting(false);
                characterBase.isBlocking = false;
                characterBase.isParrying = false;
            }
        }
        else
        {
            // Re-read held keys so the player doesn't get stuck if a key was held while input was disabled
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            float sprintValue = inputActions.Player.Sprint.ReadValue<float>();
            if (characterBase != null)
                characterBase.SetSprinting(sprintValue > 0.5f);
        }
    }

    /// <summary>
    /// Check if player input is currently enabled
    /// </summary>
    public bool IsInputEnabled()
    {
        return inputEnabled;
    }

    // ── Raid ally registration ─────────────────────────────────────────────────

    /// <summary>
    /// Register an ally villager's AI so it can be included in shield wall commands.
    /// Call once per ally when spawning/setting up raid mode.
    /// </summary>
    public void RegisterRaidAlly(VillagerAIBase ai)
    {
        if (ai != null && !_raidAllies.Contains(ai))
            _raidAllies.Add(ai);
    }

    /// <summary>
    /// Remove all registered raid allies (call when raid ends or allies are cleared).
    /// </summary>
    public void ClearRaidAllies()
    {
        _raidAllies.Clear();
        _shieldWallActive = false;
    }

    // ── Shield Wall ───────────────────────────────────────────────────────────

    /// <summary>
    /// Forms a shield wall perpendicular to the player's current facing.
    /// Allies line up alternating above/below with the player at the centre.
    /// Wall direction is fixed at activation; it moves with the party but cannot rotate.
    /// </summary>
    public void ActivateShieldWall()
    {
        if (characterBase == null || controlTarget == null) return;

        // Perpendicular to player's last move direction.
        // e.g. facing right → wall runs vertically.
        Vector2 facing = characterBase.GetLastMoveDirection();
        if (facing == Vector2.zero) facing = Vector2.right;
        Vector2 perp = new Vector2(-facing.y, facing.x); // rotate 90°

        int slotIndex = 0;
        foreach (var ai in _raidAllies)
        {
            if (ai == null) continue;

            // Slot pattern: 0→+1, 1→−1, 2→+2, 3→−2, …  (player stays in centre)
            int magnitude = slotIndex / 2 + 1;
            int side      = slotIndex % 2 == 0 ? 1 : -1;
            ai.wallFormationOffset = perp * (magnitude * side * wallSlotSpacing);
            ai.SetRaidBehavior(RaidBehavior.ShieldWall);

            var villager = ai.GetComponent<Villager>();
            villager?.personalUI?.ShowSpeech("Shield Wall!", 2f);

            slotIndex++;
        }

        // Player calls it out too
        controlTarget.personalUI?.ShowSpeech("Shield Wall!", 2f);

        _shieldWallActive = true;
        Debug.Log($"[PlayerController] Shield wall activated ({slotIndex} villagers).");
    }

    /// <summary>
    /// Dissolves the shield wall and returns all allies to follow mode.
    /// </summary>
    public void DeactivateShieldWall()
    {
        foreach (var ai in _raidAllies)
        {
            if (ai == null) continue;
            ai.SetRaidBehavior(RaidBehavior.Follow);
        }

        _shieldWallActive = false;
        Debug.Log("[PlayerController] Shield wall deactivated.");
    }
}

