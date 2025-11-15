using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optimized master controller for all DynamicShadow2D components in the scene.
/// Calculates shadow properties ONCE per frame and applies to all shadows for better performance.
/// </summary>
[ExecuteInEditMode]
public class ShadowMaster : MonoBehaviour
{
    // Singleton instance
    public static ShadowMaster Instance;

    [Header("Sun Settings")]
    [Tooltip("The position of the sun in world space")]
    public Vector2 sunPosition = new Vector2(0, 10);

    [Tooltip("The height of the sun above the ground plane")]
    [Range(0f, 1f)]
    public float sunHeight = 0.5f;
    
    [Tooltip("Minimum sun height required for shadows to be visible")]
    [Range(0f, 1f)]
    public float minSunHeightForShadows = 0.1f;
    
    [Tooltip("Optional: Reference to a transform that represents the sun")]
    public Transform sunTransform;
    
    [Header("Global Shadow Settings")]
    [Tooltip("How dark the shadow should be (0 = black, 1 = original color)")]
    [Range(0f, 1f)]
    public float shadowDarkness = 0.3f;
    
    [Tooltip("Global multiplier for shadow distance from object")]
    [Range(0f, 5f)]
    public float shadowDistanceMultiplier = 1f;
    
    [Tooltip("Global shadow intensity")]
    [Range(0f, 1f)]
    public float shadowIntensity = 0.5f;
    
    [Tooltip("Maximum shadow length")]
    [Range(1f, 20f)]
    public float maxShadowLength = 5f;
    
    [Tooltip("The order in layer for all shadows (should be below the main sprites)")]
    public int shadowSortingOrder = -1;
    
    [Tooltip("How much the shadow's x-scale shrinks as sun gets lower (0 = no shrink, 1 = maximum shrink)")]
    [Range(0f, 1f)]
    public float shadowXScaleShrinkAmount = 0.3f;
    
    [Header("Performance")]
    [Tooltip("Update shadows every frame")]
    public bool autoUpdate = true;
    
    [Tooltip("If true, automatically finds new shadows added to the scene")]
    public bool autoDetectNewShadows = true;
    
    [Tooltip("How often to check for new shadows (in seconds)")]
    [Range(0.1f, 5f)]
    public float detectionInterval = 1f;
    
    // Cached/calculated values (computed once per frame)
    private Vector2 shadowDirection;
    private float shadowRotation;
    private float sunElevation;
    private Color baseShadowColor;
    private float baseShadowAlpha;
    private float shadowXScale;
    
    private List<DynamicShadow2D> shadows = new List<DynamicShadow2D>();
    private float detectionTimer = 0f;

    public GameObject shadowPrefab;
    public float shadowFadeSpeed = 1f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DestroyImmediate(this);
            return;
        }
    }
    
    void OnEnable()
    {
        RefreshShadows();
    }
    
    void OnValidate()
    {
        // Update shadows when values change in inspector
        if (!Application.isPlaying)
        {
            Instance = this;
            RefreshShadows();
            CalculateShadowProperties();
            ApplyToAllShadows();
        }
    }
    
    void Start()
    {
        RefreshShadows();
    }
    
    void Update()
    {     
        // Update sun position from transform if assigned
        if (sunTransform != null)
        {
            sunPosition = sunTransform.position;
            sunHeight = Mathf.Clamp01(sunTransform.position.z / 10f);
        }
        
        // Auto-detect new shadows (only in play mode to avoid performance issues)
        if (autoDetectNewShadows && Application.isPlaying)
        {
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionInterval)
            {
                detectionTimer = 0f;
                CheckForNewShadows();
            }
        }
        
        // Calculate shadow properties ONCE for this frame
        if (autoUpdate)
        {
            CalculateShadowProperties();
            ApplyToAllShadows();
        }
    }
    
    /// <summary>
    /// Calculate all shadow properties once per frame (optimization)
    /// This is the core optimization - calculate once, apply to all!
    /// </summary>
    void CalculateShadowProperties()
    {
        // Calculate sun elevation
        sunElevation = sunHeight;
        
        if(sunTransform != null)
        {
            sunPosition = sunTransform.position;
            sunElevation = Mathf.Clamp01(sunTransform.position.z / 10f); // XY plane, Z is height
        }
        
        // Calculate shadow direction (we'll use Vector2.zero as reference point for direction)
        // Each shadow will apply this direction from their own position
        Vector2 directionToSun = sunPosition.normalized;
        shadowDirection = new Vector2(-directionToSun.x, -directionToSun.y).normalized;
        
        // Calculate shadow rotation
        shadowRotation = Mathf.Atan2(shadowDirection.y, shadowDirection.x) * Mathf.Rad2Deg - 90f;
        
        // Calculate base shadow color and alpha
        baseShadowColor = Color.Lerp(Color.black, Color.white, shadowDarkness);

        // Calculate target alpha based on sun elevation
        float targetAlpha;
        
        if (sunElevation < minSunHeightForShadows)
        {
            targetAlpha = 0f; // No shadows below minimum height
        }
        else
        {
            targetAlpha = Mathf.Lerp(shadowIntensity, 0.01f, sunElevation);
        }
        
        // Smoothly fade to target alpha
        baseShadowAlpha = Mathf.MoveTowards(baseShadowAlpha, targetAlpha, Time.deltaTime * shadowFadeSpeed);
        
        // Calculate shadow x-scale based on sun elevation
        // At noon (high sun elevation), scale is 1.0 (full width)
        // As sun gets lower (shadows get longer), scale shrinks
        shadowXScale = CalculateShadowXScale();
    }
    
    /// <summary>
    /// Calculates the x-scale for shadows based on sun elevation.
    /// Returns 1.0 at noon (high sun) and shrinks as the sun gets lower.
    /// </summary>
    /// <returns>X-scale multiplier for shadows (1.0 = full width, lower = narrower)</returns>
    float CalculateShadowXScale()
    {
        // At high sun elevation (noon), we want full width (1.0)
        // As sun elevation decreases (shadows get longer), we want to shrink the x-scale
        
        // sunElevation ranges from 0 (horizon) to 1 (directly overhead)
        // We want the inverse: high elevation = 1.0 scale, low elevation = reduced scale
        
        float scaleReduction = (1f - sunElevation) * shadowXScaleShrinkAmount;
        float xScale = 1f - scaleReduction;
        
        // Clamp to ensure we don't go below a reasonable minimum
        return Mathf.Max(xScale, 0.1f);
    }
    
    /// <summary>
    /// Apply pre-calculated shadow properties to all shadows
    /// </summary>
    void ApplyToAllShadows()
    {
        // Clean up null references first
        shadows.RemoveAll(s => s == null);
        
        foreach (DynamicShadow2D shadow in shadows)
        {
            if (shadow != null && shadow.enabled)
            {
                // Pass the pre-calculated values to the shadow
                
                shadow.ApplyShadowFromMaster(
                    new Color(baseShadowColor.r, baseShadowColor.g, baseShadowColor.b, baseShadowAlpha),
                    Quaternion.Euler(0f, 0f, shadowRotation),
                    shadowDistanceMultiplier,
                    sunElevation,
                    shadowXScale
                );
                
            }
        }
    }
    
    /// <summary>
    /// Finds and registers all DynamicShadow2D components in the scene
    /// </summary>
    public void RefreshShadows()
    {
        shadows.Clear();
        DynamicShadow2D[] foundShadows = FindObjectsByType<DynamicShadow2D>(FindObjectsSortMode.None);
        
        foreach (DynamicShadow2D shadow in foundShadows)
        {
            RegisterShadow(shadow);
        }
        
        Debug.Log($"ShadowMaster: Found and registered {shadows.Count} shadows");
        
        // Clean up any orphaned shadow objects in the scene
        CleanupOrphanedShadows();
    }
    
    /// <summary>
    /// Removes any shadow objects that don't have a corresponding DynamicShadow2D component
    /// </summary>
    void CleanupOrphanedShadows()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int cleanedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Check if this is a shadow object
            if (obj.name.Contains("_Shadow"))
            {
                // Check if it has a parent with a DynamicShadow2D component
                bool hasValidParent = false;
                
                if (obj.transform.parent != null)
                {
                    DynamicShadow2D parentShadow = obj.transform.parent.GetComponent<DynamicShadow2D>();
                    if (parentShadow != null && shadows.Contains(parentShadow))
                    {
                        hasValidParent = true;
                    }
                }
                
                // If no valid parent, this is an orphaned shadow
                if (!hasValidParent)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(obj);
                    }
                    else
                    {
                        DestroyImmediate(obj);
                    }
                    cleanedCount++;
                }
            }
        }
        
        if (cleanedCount > 0)
        {
            Debug.Log($"ShadowMaster: Cleaned up {cleanedCount} orphaned shadow objects");
        }
    }
    
    /// <summary>
    /// Checks for new shadows that weren't registered yet
    /// </summary>
    void CheckForNewShadows()
    {
        DynamicShadow2D[] allShadows = FindObjectsByType<DynamicShadow2D>(FindObjectsSortMode.None);
        
        foreach (DynamicShadow2D shadow in allShadows)
        {
            if (!shadows.Contains(shadow))
            {
                RegisterShadow(shadow);
                Debug.Log($"ShadowMaster: Detected and registered new shadow on {shadow.gameObject.name}");
            }
        }
        
        // Remove null references (destroyed objects)
        shadows.RemoveAll(s => s == null);
    }
    
    /// <summary>
    /// Registers a single shadow and marks it as controlled by master
    /// </summary>
    public void RegisterShadow(DynamicShadow2D shadow)
    {
        if (shadow == null || shadows.Contains(shadow))
            return;

        shadows.Add(shadow);
        shadow.shadowMaster = this;
        //shadow.SetMasterControl(true);
    }
    
    /// <summary>
    /// Unregisters a shadow from the master controller
    /// </summary>
    public void UnregisterShadow(DynamicShadow2D shadow)
    {
        if (shadows.Remove(shadow) && shadow != null)
        {
            //shadow.SetMasterControl(false);
        }
    }
    
    /// <summary>
    /// Manually trigger a shadow update for all shadows
    /// </summary>
    public void ForceUpdateAllShadows()
    {
        CalculateShadowProperties();
        ApplyToAllShadows();
    }
    
    /// <summary>
    /// Force cleanup of all duplicate and orphaned shadows in the scene
    /// </summary>
    public void ForceCleanupAllShadows()
    {
        // First, tell all registered shadows to clean up their duplicates
        foreach (DynamicShadow2D shadow in shadows)
        {
            if (shadow != null)
            {
                // Force the shadow to check for duplicates
                shadow.SendMessage("CleanupDuplicateShadows", SendMessageOptions.DontRequireReceiver);
            }
        }
        
        // Then clean up any orphaned shadows
        CleanupOrphanedShadows();
        
        Debug.Log("ShadowMaster: Forced cleanup completed");
    }
    
    
    /// <summary>
    /// Get the current number of registered shadows
    /// </summary>
    public int GetShadowCount()
    {
        shadows.RemoveAll(s => s == null);
        return shadows.Count;
    }
    
    /// <summary>
    /// Get list of all registered shadows
    /// </summary>
    public List<DynamicShadow2D> GetAllShadows()
    {
        shadows.RemoveAll(s => s == null);
        return new List<DynamicShadow2D>(shadows);
    }
    
    /// <summary>
    /// Get the current shadow direction (for external use/debugging)
    /// </summary>
    public Vector2 GetShadowDirection()
    {
        return shadowDirection;
    }
    
    /// <summary>
    /// Get the current sun elevation (for external use/debugging)
    /// </summary>
    public float GetSunElevation()
    {
        return sunElevation;
    }
    
    /// <summary>
    /// Get the calculated shadow x-scale based on sun angle
    /// </summary>
    public float GetShadowXScale()
    {
        return shadowXScale;
    }
    
    // Visualize sun in editor
    void OnDrawGizmos()
    {
        // Calculate and draw sun position
        Vector2 sunPos = sunPosition;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(sunPos.x, sunPos.y, 0), 0.5f);
        
        // Draw sun height indicator
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawLine(new Vector3(sunPos.x, sunPos.y, 0), new Vector3(sunPos.x, sunPos.y, sunHeight * 10f));
    }
    
    void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = new Vector3(center.x + radius, center.y, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius,
                0f
            );
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}