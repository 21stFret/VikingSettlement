using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Centralized controller for mouse input that broadcasts click events.
/// Clickable objects register with this controller instead of polling in Update.
/// </summary>
public class MouseInputController : MonoBehaviour
{
    public static MouseInputController Instance { get; private set; }

    /// <summary>
    /// Event fired when a world click occurs (not on UI).
    /// Passes the world position of the click.
    /// </summary>
    public event Action<Vector2> OnWorldClick;

    /// <summary>
    /// Event fired when a right click occurs in the world.
    /// </summary>
    public event Action<Vector2> OnWorldRightClick;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private HashSet<IClickable> registeredClickables = new HashSet<IClickable>();
    private List<IClickable> sortedClickables = new List<IClickable>();
    private bool isDirty = true;

    // Queue for clickables that tried to register before Instance was ready
    private static List<IClickable> pendingRegistrations = new List<IClickable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Process any pending registrations
        foreach (var clickable in pendingRegistrations)
        {
            if (clickable != null && (clickable as UnityEngine.Object) != null)
            {
                Register(clickable);
            }
        }
        pendingRegistrations.Clear();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // Left click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }

        // Right click
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    private void HandleLeftClick()
    {
        // Check if clicking on UI
        if (IsPointerOverUI())
        {
            if (debugMode) //Debug.Log("[MouseInputController] Click blocked - pointer over UI");
            return;
        }

        Vector2 worldPosition = GetMouseWorldPosition();

        if (debugMode) Debug.Log($"[MouseInputController] Click at world pos: {worldPosition}, registered clickables: {registeredClickables.Count}");

        // Try to find a clickable object at this position
        IClickable clicked = GetClickableAtPosition(worldPosition);

        if (clicked != null)
        {
            if (debugMode) Debug.Log($"[MouseInputController] Clicked on: {(clicked as UnityEngine.Object)?.name}");
            clicked.OnClicked();
        }
        else
        {
            if (debugMode) Debug.Log("[MouseInputController] No clickable found, firing world click event");
            // No clickable found, fire generic world click event
            OnWorldClick?.Invoke(worldPosition);
        }
    }

    private void HandleRightClick()
    {
        if (IsPointerOverUI()) return;

        Vector2 worldPosition = GetMouseWorldPosition();
        OnWorldRightClick?.Invoke(worldPosition);
    }

    /// <summary>
    /// Register a clickable object to receive click events.
    /// Can be called before Instance exists - will queue for later.
    /// </summary>
    public static void RegisterClickable(IClickable clickable)
    {
        if (Instance != null)
        {
            Instance.Register(clickable);
        }
        else
        {
            // Queue for when Instance is ready
            if (!pendingRegistrations.Contains(clickable))
            {
                pendingRegistrations.Add(clickable);
            }
        }
    }

    /// <summary>
    /// Unregister a clickable object (static version)
    /// </summary>
    public static void UnregisterClickable(IClickable clickable)
    {
        if (Instance != null)
        {
            Instance.Unregister(clickable);
        }
        else
        {
            pendingRegistrations.Remove(clickable);
        }
    }

    /// <summary>
    /// Register a clickable object to receive click events
    /// </summary>
    public void Register(IClickable clickable)
    {
        if (registeredClickables.Add(clickable))
        {
            isDirty = true;
            if (debugMode) Debug.Log($"[MouseInputController] Registered: {(clickable as UnityEngine.Object)?.name}");
        }
    }

    /// <summary>
    /// Unregister a clickable object
    /// </summary>
    public void Unregister(IClickable clickable)
    {
        if (registeredClickables.Remove(clickable))
        {
            isDirty = true;
            if (debugMode) Debug.Log($"[MouseInputController] Unregistered: {(clickable as UnityEngine.Object)?.name}");
        }
    }

    private IClickable GetClickableAtPosition(Vector2 worldPosition)
    {
        if (isDirty)
        {
            RebuildSortedList();
        }

        // Check clickables in priority order (highest first)
        foreach (var clickable in sortedClickables)
        {
            if (clickable == null) continue;
            if (!clickable.IsClickable) continue;
            if (clickable.Collider == null) continue;

            if (clickable.Collider.OverlapPoint(worldPosition))
            {
                return clickable;
            }
        }

        return null;
    }

    private void RebuildSortedList()
    {
        sortedClickables.Clear();

        // Remove any null entries
        registeredClickables.RemoveWhere(c => c == null || (c as UnityEngine.Object) == null);

        sortedClickables.AddRange(registeredClickables);
        sortedClickables.Sort((a, b) => b.ClickPriority.CompareTo(a.ClickPriority));
        isDirty = false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(-1);
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (Camera.main == null) return Vector2.zero;
        return Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
