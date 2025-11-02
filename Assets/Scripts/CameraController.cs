using UnityEngine;

/// <summary>
/// Camera controller that smoothly follows the player
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    [Header("Target")]
    [SerializeField] private Transform target; // The player to follow
    public Transform playerTarget;
    
    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 0.125f; // How smooth the camera follows (lower = smoother)
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // Camera offset from player
    
    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds; // Minimum X and Y position
    [SerializeField] private Vector2 maxBounds; // Maximum X and Y position
    
    [Header("Zoom (Optional)")]
    [SerializeField] private bool allowZoom = false;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float zoomSpeed = 1f;
    
    private Camera cam;
    private float targetZoom;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        
        if (cam != null && cam.orthographic)
        {
            targetZoom = cam.orthographicSize;
        }
        
        // If no target assigned, try to find player
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
            }
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move towards target
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Apply bounds if enabled
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
        }
        
        // Keep the Z offset
        smoothedPosition.z = offset.z;
        
        transform.position = smoothedPosition;
        
        // Handle zoom
        if (allowZoom && cam != null && cam.orthographic)
        {
            HandleZoom();
        }
    }
    
    /// <summary>
    /// Handle zoom with mouse scroll wheel
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        
        if (scroll != 0)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
        
        // Smoothly interpolate to target zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, smoothSpeed);
    }

    /// <summary>
    /// Set the camera target
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetPlayerTarget()
    {
        target = playerTarget;
    }

    /// <summary>
    /// Instantly move camera to target (no smooth)
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        Vector3 newPosition = target.position + offset;

        if (useBounds)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
            newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
        }

        newPosition.z = offset.z;
        transform.position = newPosition;
    }
    
    public Transform GetCurrentTarget()
    {
        return target;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Visualize bounds in editor
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
