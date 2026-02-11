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

    private void Update()
    {
        // Skip if we don't have a valid controller
        if (controller == null)
        {
            return;
        }

        // Only apply keyboard input if not using mouse movement and input is enabled
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

        // Stop current movement
        if (controller != null)
        {
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
            if (controller != null)
            {
                controller.SetMovement(Vector2.zero);
                controller.SetSprinting(false);
            }
        }
    }

    /// <summary>
    /// Check if player input is currently enabled
    /// </summary>
    public bool IsInputEnabled()
    {
        return inputEnabled;
    }
}

