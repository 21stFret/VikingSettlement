using UnityEngine;

/// <summary>
/// Creates a dynamic 2D shadow for sprites based on a sun position.
/// The shadow darkens the sprite and scales/positions it based on the sun's angle and object height.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class FreeDynamicShadow2D : MonoBehaviour
{
    [Header("Sun Settings")]
    [Tooltip("The position of the sun in world space")]
    public Vector2 sunPosition = new Vector2(0, 10);
    [Tooltip("The height of the sun above the ground plane")]
    [Range(0f, 1f)]
    public float sunHeight = 0.5f; // Height of the sun above the ground plane

    public Transform sunTransform;

    [Header("Shadow Settings")]
    [Tooltip("How dark the shadow should be (0 = black, 1 = original color)")]
    [Range(0f, 1f)]
    public float shadowDarkness = 0.3f;
    
    [Tooltip("The fake height of the object above the ground (affects shadow length)")]
    [Range(0f, 10f)]
    public float objectHeight = 1f;

    [Tooltip("Multiplier for shadow distance from object")]
    [Range(0f, 5f)]
    public float shadowDistanceMultiplier = 1f;

    public float shadowIntensity = 0.5f;
    
    public float maxShadowLength = 5f;
    
    [Tooltip("The order in layer for the shadow (should be below the main sprite)")]
    public int shadowSortingOrder = -1;
    
    [Header("Optional Settings")]
    [Tooltip("If true, shadow will update every frame. If false, call UpdateShadow() manually")]
    public bool autoUpdate = true;
    
    private SpriteRenderer spriteRenderer;
    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;
    
    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CleanupDuplicateShadows();
        if (shadowObject == null)
        {
            CreateShadow();
        }
    }
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CleanupDuplicateShadows();
        if (shadowObject == null)
        {
            CreateShadow();
        }
    }
    
    void Update()
    {
        if (autoUpdate)
        {
            UpdateShadow();
        }
    }
    
    /// <summary>
    /// Removes any duplicate shadow objects that may have been created
    /// </summary>
    void CleanupDuplicateShadows()
    {
        // Find all children with "_Shadow" in the name
        Transform[] children = GetComponentsInChildren<Transform>(true);
        int shadowCount = 0;
        
        foreach (Transform child in children)
        {
            if (child != transform && child.name.Contains("_Shadow"))
            {
                shadowCount++;
                
                // Keep the first shadow we find, destroy the rest
                if (shadowCount == 1 && shadowObject == null)
                {
                    shadowObject = child.gameObject;
                    shadowRenderer = child.GetComponent<SpriteRenderer>();
                }
                else
                {
                    // Destroy duplicate shadows
                    if (Application.isPlaying)
                    {
                        //Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Creates the shadow game object and renderer
    /// </summary>
    void CreateShadow()
    {
        // Double-check we don't already have a shadow
        if (shadowObject != null)
            return;
            
        // Create shadow object as child
        shadowObject = new GameObject(gameObject.name + "_Shadow");
        shadowObject.transform.SetParent(transform);
        shadowObject.hideFlags = HideFlags.DontSave; // Don't save shadow to prevent duplicates
        
        // Add and configure sprite renderer
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = spriteRenderer.sprite;
        shadowRenderer.sortingOrder = shadowSortingOrder;
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;       
        // Initial shadow update
        UpdateShadow();
    }
    
    /// <summary>
    /// Updates the shadow's position, scale, and color based on sun position and object height
    /// </summary>
    public void UpdateShadow()
    {
        if (shadowRenderer == null || spriteRenderer == null)
            return;

        // Update sprite if it changed
        if (shadowRenderer.sprite != spriteRenderer.sprite)
        {
            shadowRenderer.sprite = spriteRenderer.sprite;
        }

        // Calculate how much the sun is above the horizon (0 = horizon, 1 = directly above)
        float sunElevation = sunHeight;
        
        if(sunTransform != null)
        {
            sunPosition = sunTransform.position;
            sunElevation = Mathf.Clamp01(sunTransform.position.z / 10f); // XY plane, Z is height
        }
        
        // Calculate direction from object to sun
        Vector2 objectPosition = transform.position;
        Vector2 directionToSun = (sunPosition - objectPosition).normalized;

        // Calculate shadow direction (opposite of sun direction, projected on XY plane)
        Vector2 shadowDirection = new Vector2(-directionToSun.x, -directionToSun.y).normalized;

        // shadow rotation should be aligned with the shadow direction
        shadowObject.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shadowDirection.y, shadowDirection.x) * Mathf.Rad2Deg - 90f);

        // Shadow length is inversely proportional to sun elevation
        // When sun is high (elevation > 1), shadow is short
        // When sun is low (elevation near 0), shadow is very long
        float shadowLength = objectHeight * shadowDistanceMultiplier;
        if (sunElevation > 0.01f) // Avoid division by zero
        {
            shadowLength /= sunElevation;
        }
        else
        {
            shadowLength *= 100f; // Very long shadow when sun is at horizon
        }

        // Clamp shadow length to reasonable values
        shadowLength = Mathf.Clamp(shadowLength, 0f, maxShadowLength);
        
        // Scale shadow based on distance and sun elevation
        // Shadow gets smaller/compressed when sun is higher
        float scaleY = Mathf.Lerp(shadowLength, 0.3f, sunElevation); // Compress shadow vertically when sun is high
        shadowObject.transform.localScale = new Vector3(
            transform.localScale.x,
            transform.localScale.y * scaleY,
            transform.localScale.z
        );

        // Apply shadow darkness
        Color shadowColor = Color.Lerp(Color.black, spriteRenderer.color, shadowDarkness);

        shadowColor.a = spriteRenderer.color.a * Mathf.Lerp(shadowIntensity, 0.1f, sunElevation); // Fade shadow when sun is high

        if(sunElevation <= 0.1f)
        {
            // At or below horizon, make shadow fully invisible
            shadowColor.a = 0;
        }
        
        shadowRenderer.color = shadowColor;
        
        // Match flip settings
        shadowRenderer.flipX = spriteRenderer.flipX;
        shadowRenderer.flipY = spriteRenderer.flipY;

        // Position shadow
        shadowObject.transform.position = objectPosition;
    }
    
    /// <summary>
    /// Updates the sun position and refreshes the shadow
    /// </summary>
    public void SetSunPosition(Vector3 newSunPosition)
    {
        sunPosition = newSunPosition;
        UpdateShadow();
    }
    
    void OnDestroy()
    {
        // Clean up shadow object
        if (shadowObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(shadowObject);
            }
            else
            {
                DestroyImmediate(shadowObject);
            }
        }
    }
    
    void OnDisable()
    {
        // Hide shadow when component is disabled
        if (shadowObject != null)
        {
            shadowObject.SetActive(false);
        }
    }
    
    // Visualize sun position in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sunPosition, 0.5f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, sunPosition);
        
        // Draw shadow direction
        if (Application.isPlaying && shadowObject != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, shadowObject.transform.position);
        }
    }
}