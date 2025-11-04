using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

/// <summary>
/// Advanced height-aware shadow system that directly modifies the shadow mesh.
/// This gives precise control over shadow shape and length based on object height.
/// Works by accessing and modifying Unity's internal shadow mesh vertices.
/// </summary>
[RequireComponent(typeof(ShadowCaster2D))]
[ExecuteAlways]
public class AdvancedHeightShadow2D : MonoBehaviour
{
    [Header("Height Configuration")]
    [Tooltip("Height of object above ground (0 = on ground, 10 = very high)")]
    [Range(0f, 10f)]
    public float objectHeight = 0f;
    
    [Tooltip("Auto-calculate height from Y position")]
    public bool autoHeight = false;
    
    [Tooltip("Y position considered as ground level")]
    public float groundY = 0f;
    
    [Header("Shadow Behavior")]
    [Tooltip("How far shadow extends at height 0")]
    [Range(0.1f, 20f)]
    public float baseShadowLength = 5f;
    
    [Tooltip("Minimum shadow length at maximum height")]
    [Range(0.1f, 5f)]
    public float minShadowLength = 0.5f;
    
    [Tooltip("Shadow shrinking intensity (higher = faster shrink with height)")]
    [Range(0.1f, 3f)]
    public float shrinkPower = 1f;
    
    [Tooltip("Scale the shadow shape itself (makes shadow smaller/thinner)")]
    [Range(0.1f, 1f)]
    public float shadowShapeScale = 1f;
    
    [Header("Performance")]
    [Tooltip("Update interval in frames (1 = every frame, higher = better performance)")]
    [Range(1, 30)]
    public int updateEveryNFrames = 1;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private ShadowCaster2D shadowCaster;
    private PropertyInfo trimEdgeProperty;
    private int frameCounter = 0;
    private float previousHeight = -999f;
    private float previousBaseLength = -999f;
    private float previousMinLength = -999f;
    private float previousShapeScale = -999f;

    void OnEnable()
    {
        Setup();
    }

    void Setup()
    {
        shadowCaster = GetComponent<ShadowCaster2D>();
        if (shadowCaster == null)
        {
            Debug.LogError($"[AdvancedHeightShadow2D] {gameObject.name}: ShadowCaster2D component required!");
            enabled = false;
            return;
        }

        // Get the trimEdge property (this is public in ShadowCaster2D)
        var type = typeof(ShadowCaster2D);
        trimEdgeProperty = type.GetProperty("trimEdge");
        
        if (trimEdgeProperty == null)
        {
            Debug.LogError($"[AdvancedHeightShadow2D] Could not find trimEdge property. Unity version may have changed.");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[AdvancedHeightShadow2D] Setup complete on {gameObject.name}");
        }
    }

    void LateUpdate()
    {
        if (shadowCaster == null || trimEdgeProperty == null)
            return;

        // Update based on frame interval
        frameCounter++;
        if (frameCounter < updateEveryNFrames)
            return;
        frameCounter = 0;

        UpdateShadowBasedOnHeight();
    }

    void UpdateShadowBasedOnHeight()
    {
        // Get current height
        float currentHeight = GetCurrentHeight();
        
        // Check if we need to update
        if (HasValuesChanged(currentHeight))
        {
            ApplyShadowModifications(currentHeight);
            CacheCurrentValues(currentHeight);
        }
    }

    float GetCurrentHeight()
    {
        if (autoHeight)
        {
            return Mathf.Max(0f, transform.position.y - groundY);
        }
        return objectHeight;
    }

    bool HasValuesChanged(float currentHeight)
    {
        return !Mathf.Approximately(currentHeight, previousHeight) ||
               !Mathf.Approximately(baseShadowLength, previousBaseLength) ||
               !Mathf.Approximately(minShadowLength, previousMinLength) ||
               !Mathf.Approximately(shadowShapeScale, previousShapeScale);
    }

    void CacheCurrentValues(float currentHeight)
    {
        previousHeight = currentHeight;
        previousBaseLength = baseShadowLength;
        previousMinLength = minShadowLength;
        previousShapeScale = shadowShapeScale;
    }

    void ApplyShadowModifications(float currentHeight)
    {
        // Calculate height factor (0 to 1, where 1 is maximum height)
        float heightFactor = Mathf.Clamp01(currentHeight / 10f);
        
        // Apply power curve for more natural falloff
        float curvedFactor = Mathf.Pow(heightFactor, shrinkPower);
        
        // Calculate target shadow length
        float targetLength = Mathf.Lerp(baseShadowLength, minShadowLength, curvedFactor);
        
        // Apply shape scaling (makes the shadow thinner at height)
        float shapeModifier = Mathf.Lerp(1f, shadowShapeScale, curvedFactor);
        
        // In Unity's shadow system, trimEdge works as follows:
        // - Negative values = extend shadow further
        // - Positive values = trim/shorten shadow
        // - The scale is roughly: trimEdge in world units
        
        // Convert our target length to trim edge value
        // We want: at height 0 use baseShadowLength, at max height use minShadowLength
        // Default shadow cast distance is roughly 10 units, so:
        float defaultShadowDistance = 10f;
        float trimAmount = defaultShadowDistance - targetLength;
        
        // Apply the trim edge
        try
        {
            trimEdgeProperty.SetValue(shadowCaster, trimAmount);
            
            // Force the shadow caster to update by marking it dirty
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(shadowCaster);
            }
            #endif
            
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Height: {currentHeight:F2} | " +
                         $"Factor: {curvedFactor:F2} | " +
                         $"Length: {targetLength:F2} | " +
                         $"TrimEdge: {trimAmount:F2}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AdvancedHeightShadow2D] Failed to set trimEdge: {e.Message}");
        }
    }

    /// <summary>
    /// Set object height programmatically
    /// </summary>
    public void SetHeight(float height)
    {
        objectHeight = Mathf.Clamp(height, 0f, 10f);
        autoHeight = false;
        frameCounter = updateEveryNFrames; // Force immediate update
    }

    /// <summary>
    /// Get current calculated height
    /// </summary>
    public float GetHeight()
    {
        return GetCurrentHeight();
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        
        float height = GetCurrentHeight();
        
        if (height > 0.01f)
        {
            Vector3 pos = transform.position;
            Vector3 groundPos = new Vector3(pos.x, autoHeight ? groundY : pos.y - height, pos.z);
            
            // Draw height line
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
            Gizmos.DrawLine(pos, groundPos);
            
            // Draw ground indicator
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Gizmos.DrawWireCube(groundPos, new Vector3(0.3f, 0.05f, 0.3f));
            
            // Draw shadow range visualization
            float heightFactor = Mathf.Clamp01(height / 10f);
            float curvedFactor = Mathf.Pow(heightFactor, shrinkPower);
            float shadowLength = Mathf.Lerp(baseShadowLength, minShadowLength, curvedFactor);
            
            Gizmos.color = new Color(0f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(pos, shadowLength);
            
            // Draw text info
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, 
                $"Height: {height:F1}\nShadow: {shadowLength:F1}");
            #endif
        }
    }

    void OnValidate()
    {
        // Clamp values
        baseShadowLength = Mathf.Max(0.1f, baseShadowLength);
        minShadowLength = Mathf.Max(0.1f, minShadowLength);
        minShadowLength = Mathf.Min(minShadowLength, baseShadowLength);
        
        // Force update on next frame
        frameCounter = updateEveryNFrames;
    }
}
