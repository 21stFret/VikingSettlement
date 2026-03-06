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

    private CharacterController controller;
    private VillagerAI targetAI;
    private Vector2 moveInput;
    private bool inputEnabled = true;
    private bool isAttackHeld = false;
    private float blockPressTime = -999f;
    [SerializeField] private float parryWindowDuration = 0.3f;

    // Shield wall
    private bool _shieldWallActive = false;
    private readonly System.Collections.Generic.List<VillagerAI> _raidAllies
        = new System.Collections.Generic.List<VillagerAI>();
    [Header("Shield Wall")]
    [Tooltip("World-space gap between each villager in the formation.")]
    [SerializeField] private float wallSlotSpacing = 1f;

    // Input System
    private PlayerInputActions inputActions;

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

        // If we have a control target set in inspector, use it
        if (controlTarget != null)
        {
            SetControlTarget(controlTarget);
        }
        else
        {
            // Fallback: try to get CharacterController on this object
            controller = GetComponent<CharacterController>();
        }
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
        inputActions.Player.ShieldWall.performed += OnShieldWall;
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
        inputActions.Player.ShieldWall.performed -= OnShieldWall;
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
        if (!inputEnabled || controller == null) return;
        if (!useMouseMovement) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(inputActions.Player.MousePosition.ReadValue<Vector2>());
        controller.MoveTo(mousePos);
    }

    private void OnStopMove(InputAction.CallbackContext context)
    {
        if (!inputEnabled || controller == null) return;
        if (useMouseMovement)
        {
            controller.Stop();
        }
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (!inputEnabled || controller == null) return;
        if (context.performed)
        {
            controller.SetSprinting(true);
        }
        else if (context.canceled)
        {
            controller.SetSprinting(false);
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!inputEnabled || controller == null) return;
        if (context.performed)
        {
            isAttackHeld = true;
            controller.Attack();
        }
    }

    private void OnAttackReleased(InputAction.CallbackContext context)
    {
        isAttackHeld = false;
    }

    private void OnShieldWall(InputAction.CallbackContext context)
    {
        if (!inputEnabled) return;
        if (_shieldWallActive)
            DeactivateShieldWall();
        else
            ActivateShieldWall();
    }

    private void Update()
    {
        // Skip if we don't have a valid controller
        if (controller == null)
        {
            return;
        }

        // Block input (right mouse button) — resolved first so movement uses current frame's state
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            if (inputEnabled && mouse.rightButton.wasPressedThisFrame)
                blockPressTime = Time.time;

            bool hasShield = controller.shield != null && !controller.shield.IsBroken;
            bool inParryWindow = inputEnabled && hasShield && (Time.time - blockPressTime) < parryWindowDuration;

            controller.isParrying = inParryWindow;
            if (_shieldWallActive)
            {
                // Shield wall: player holds shield permanently, no parrying
                controller.isParrying = false;
                controller.isBlocking = hasShield;
            }
            else
            {
                controller.isBlocking = inParryWindow || (inputEnabled && hasShield && mouse.rightButton.isPressed);
            }
        }

        // Movement (50% speed while blocking is handled inside GetEffectiveMoveSpeed)
        if (!useMouseMovement && inputEnabled)
        {
            controller.SetMovement(moveInput * playerMoveSpeed);
        }
        else if (!inputEnabled)
        {
            // Stop movement when input is disabled
            controller.SetMovement(Vector2.zero);
        }

        // Handle held attack - continue attacking while button is held
        if (isAttackHeld && inputEnabled && controller.CanAttack())
        {
            controller.Attack();
        }

    }
    
    /// <summary>
    /// Toggle between keyboard and mouse movement
    /// </summary>
    public void SetMouseMovement(bool enabled)
    {
        useMouseMovement = enabled;
        if (!enabled && controller != null)
        {
            controller.Stop();
        }
    }
    
    /// <summary>
    /// Get reference to the underlying CharacterController
    /// </summary>
    public CharacterController GetController()
    {
        return controller;
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

        // Re-enable AI on previous target
        if (targetAI != null)
        {
            targetAI.SetAIEnabled(true);
        }

        // Restore reactive blocking on old target and clear its block state
        if (controller != null)
        {
            controller.useReactiveBlocking = true;
            controller.isBlocking = false;
            controller.isParrying = false;
            controller.Stop();
        }

        // Set new target
        controlTarget = target;
        controller = target.GetComponent<CharacterController>();
        targetAI = target.GetComponent<VillagerAI>();

        if (controller == null)
        {
            Debug.LogError($"Control target {target.villagerName} has no CharacterController!");
            return;
        }

        // Disable reactive blocking and AI on new target (player controls this villager)
        controller.useReactiveBlocking = false;
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
            if (controller != null)
            {
                controller.SetMovement(Vector2.zero);
                controller.SetSprinting(false);
                controller.isBlocking = false;
                controller.isParrying = false;
            }
        }
        else
        {
            // Re-read held keys so the player doesn't get stuck if a key was held while input was disabled
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            float sprintValue = inputActions.Player.Sprint.ReadValue<float>();
            if (controller != null)
                controller.SetSprinting(sprintValue > 0.5f);
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
    public void RegisterRaidAlly(VillagerAI ai)
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
        if (controller == null || controlTarget == null) return;

        // Perpendicular to player's last move direction.
        // e.g. facing right → wall runs vertically.
        Vector2 facing = controller.GetLastMoveDirection();
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
            ai.SetRaidBehavior(VillagerAI.RaidBehavior.ShieldWall);

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
            ai.SetRaidBehavior(VillagerAI.RaidBehavior.Follow);
        }

        _shieldWallActive = false;
        Debug.Log("[PlayerController] Shield wall deactivated.");
    }
}

