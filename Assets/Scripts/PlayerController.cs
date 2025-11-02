using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VillagerController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private bool useMouseMovement = false; // Toggle between WASD and click-to-move
    
    private VillagerController controller;
    private Vector2 moveInput;
    
    // Input System
    private PlayerInputActions inputActions;
    
    private void Awake()
    {
        controller = GetComponent<VillagerController>();
        
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
        inputActions.Disable();
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnClick(InputAction.CallbackContext context)
    {
        if (!useMouseMovement) return;
        
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(inputActions.Player.MousePosition.ReadValue<Vector2>());
        controller.MoveTo(mousePos);
    }

    private void OnStopMove(InputAction.CallbackContext context)
    {
        if (useMouseMovement)
        {
            controller.Stop();
        }
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
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
        if (context.performed)
        {
            controller.Attack();
        }
    }

    private void Update()
    {
        // Only apply keyboard input if not using mouse movement or not currently moving to a target
        if (!useMouseMovement)
        {
            controller.SetMovement(moveInput);
        }
    }
    
    /// <summary>
    /// Toggle between keyboard and mouse movement
    /// </summary>
    public void SetMouseMovement(bool enabled)
    {
        useMouseMovement = enabled;
        if (!enabled)
        {
            controller.Stop();
        }
    }
    
    /// <summary>
    /// Get reference to the underlying VillagerController
    /// </summary>
    public VillagerController GetController()
    {
        return controller;
    }
}

