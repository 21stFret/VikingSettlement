using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Reflection;

/// <summary>
/// Modifies Unity's 2D Shadow Caster mesh to create height-aware shadows.
/// This actually changes the shadow mesh vertices to control shadow length.
/// Attach this to objects with a ShadowCaster2D component.
/// </summary>
[RequireComponent(typeof(ShadowCaster2D))]
[ExecuteAlways]
public class HeightAwareShadowCaster2D : MonoBehaviour
{
    [Header("Height Settings")]
    [Tooltip("Height of the object above ground (0 = on ground)")]
    [Range(0f, 10f)]
    public float objectHeight = 0f;
    
    [Tooltip("Automatically calculate height from Y position")]
    public bool autoCalculateFromY = false;
    
    [Tooltip("Ground level Y position (when autoCalculateFromY is true)")]
    public float groundLevelY = 0f;
    
    [Header("Shadow Length Control")]
    [Tooltip("Maximum shadow distance when on ground")]
    public float maxShadowDistance = 5f;
    
    [Tooltip("Minimum shadow distance at maximum height")]
    public float minShadowDistance = 0.5f;
    
    [Tooltip("How aggressively shadows shrink with height (0 = linear, 1 = exponential)")]
    [Range(0f, 2f)]
    public float falloffCurve = 1f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool debugMode = false;
    
    private ShadowCaster2D shadowCaster;
    private FieldInfo meshField;
    private FieldInfo trimEdgeField;
    private object shadowMesh2D;
    private MethodInfo updateMeshMethod;
    
    private float lastHeight = -1f;
    private float lastMaxDistance = -1f;
    private float lastMinDistance = -1f;

    void OnEnable()
    {
        InitializeReflection();
    }

    void InitializeReflection()
    {
        shadowCaster = GetComponent<ShadowCaster2D>();
        if (shadowCaster == null)
        {
            Debug.LogError("HeightAwareShadowCaster2D: ShadowCaster2D component not found!");
            enabled = false;
            return;
        }

        // Access the internal m_ShadowMesh field
        var shadowCasterType = typeof(ShadowCaster2D);
        meshField = shadowCasterType.GetField("m_ShadowMesh", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (meshField != null)
        {
            shadowMesh2D = meshField.GetValue(shadowCaster);
            
            if (shadowMesh2D != null)
            {
                var shadowMesh2DType = shadowMesh2D.GetType();
                
                // Get trimEdge field which controls shadow length
                trimEdgeField = shadowMesh2DType.GetField("m_TrimEdge", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                // Try to find the update method
                updateMeshMethod = shadowMesh2DType.GetMethod("UpdateMesh", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (debugMode)
                {
                    Debug.Log($"Reflection setup complete. TrimEdge field: {trimEdgeField != null}, Update method: {updateMeshMethod != null}");
                }
            }
        }
        
        if (meshField == null || shadowMesh2D == null)
        {
            Debug.LogError("HeightAwareShadowCaster2D: Could not access shadow mesh internals. Shadow system may have changed.");
        }
    }

    void LateUpdate()
    {
        if (shadowCaster == null || shadowMesh2D == null || trimEdgeField == null)
        {
            return;
        }
        
        UpdateShadowLength();
    }

    void UpdateShadowLength()
    {
        // Calculate current height
        float currentHeight = autoCalculateFromY ? 
            Mathf.Max(0f, transform.position.y - groundLevelY) : 
            objectHeight;
        
        // Only update if something changed
        if (Mathf.Approximately(currentHeight, lastHeight) && 
            Mathf.Approximately(maxShadowDistance, lastMaxDistance) &&
            Mathf.Approximately(minShadowDistance, lastMinDistance))
        {
            return;
        }
        
        lastHeight = currentHeight;
        lastMaxDistance = maxShadowDistance;
        lastMinDistance = minShadowDistance;
        
        // Calculate shadow distance based on height with falloff curve
        float heightRatio = Mathf.Clamp01(currentHeight / 10f);
        
        // Apply falloff curve (exponential scaling)
        float curvedRatio = Mathf.Pow(heightRatio, Mathf.Max(0.1f, falloffCurve));
        
        // Calculate the trim edge value (negative values extend shadow, positive trim it)
        // At height 0: use maxShadowDistance
        // At max height: use minShadowDistance
        float shadowDistance = Mathf.Lerp(maxShadowDistance, minShadowDistance, curvedRatio);
        
        // TrimEdge in Unity's system: negative = extend, 0 = default, positive = trim
        // We need to convert our distance to Unity's trimEdge value
        float trimValue = -shadowDistance;
        
        // Set the trim edge via reflection
        try
        {
            trimEdgeField.SetValue(shadowMesh2D, trimValue);
            
            // Force shadow caster to update
            if (updateMeshMethod != null)
            {
                updateMeshMethod.Invoke(shadowMesh2D, null);
            }
            
            // Also set it on the public property if available
            shadowCaster.trimEdge = trimValue;
            
            if (debugMode)
            {
                Debug.Log($"Height: {currentHeight:F2}, Shadow Distance: {shadowDistance:F2}, TrimEdge: {trimValue:F2}");
            }
        }
        catch (System.Exception e)
        {
            if (debugMode)
            {
                Debug.LogError($"Failed to update shadow: {e.Message}");
            }
        }
    }

    public void SetHeight(float height)
    {
        objectHeight = Mathf.Max(0f, height);
        autoCalculateFromY = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!enabled) return;
        
        float currentHeight = autoCalculateFromY ? 
            Mathf.Max(0f, transform.position.y - groundLevelY) : 
            objectHeight;
        
        if (currentHeight > 0.01f)
        {
            // Draw height indicator
            Gizmos.color = Color.yellow;
            Vector3 objPos = transform.position;
            Vector3 groundPos = autoCalculateFromY ? 
                new Vector3(objPos.x, groundLevelY, objPos.z) : 
                new Vector3(objPos.x, objPos.y - currentHeight, objPos.z);
            
            Gizmos.DrawLine(objPos, groundPos);
            Gizmos.DrawWireSphere(groundPos, 0.15f);
            
            // Draw shadow distance indicator
            float heightRatio = Mathf.Clamp01(currentHeight / 10f);
            float curvedRatio = Mathf.Pow(heightRatio, Mathf.Max(0.1f, falloffCurve));
            float shadowDistance = Mathf.Lerp(maxShadowDistance, minShadowDistance, curvedRatio);
            
            Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(objPos, shadowDistance);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Re-initialize when values change in inspector
        if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            InitializeReflection();
        }
    }
#endif
}