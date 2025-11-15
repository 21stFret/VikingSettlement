using UnityEngine;
using System.Collections.Generic;   

/// <summary>
/// Creates a dynamic 2D shadow for sprites based on a sun position.
/// The shadow darkens the sprite and scales/positions it based on the sun's angle and object height.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicShadow2D : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;
    public float objectHeight = 1f;
    public float shadowOffsetY = 0;
    public float shadowHorizontalMovement = 0;
    public ShadowMaster shadowMaster;
    
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
        shadowMaster = ShadowMaster.Instance;
        CleanupDuplicateShadows();
        if (shadowObject == null)
        {
            CreateShadow();
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
        shadowObject = Instantiate(shadowMaster.shadowPrefab);
        shadowObject.name = gameObject.name + "_Shadow";
        shadowObject.transform.SetParent(transform);
        shadowObject.transform.localPosition = new Vector3(0f, shadowOffsetY, 0f);
        shadowObject.transform.localRotation = Quaternion.identity;
        shadowObject.transform.localScale = Vector3.one;
        shadowObject.hideFlags = HideFlags.DontSave; // Don't save shadow to prevent duplicates

        // Add and configure sprite renderer
        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if(shadowRenderer==null)
        {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }
        shadowRenderer.sprite = spriteRenderer.sprite;
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;

        SpriteSorting spriteSorting = GetComponent<SpriteSorting>();
        if (spriteSorting != null)
        {
            spriteSorting.linkedSpriteRenderers.Add(shadowRenderer);
        }
    }

    public void ApplyShadowFromMaster(Color shadowColor, Quaternion shadowRotation, float shadowDistanceMultiplier, float sunElevation)
    {
        UpdateShadow(shadowColor, shadowRotation, shadowDistanceMultiplier, sunElevation);
    }
    
    /// <summary>
    /// Updates the shadow's position, scale, and color based on sun position and object height
    /// </summary>
    public void UpdateShadow(Color shadowColor, Quaternion shadowRotation, float shadowDistanceMultiplier, float sunElevation)
    {
        if (shadowRenderer == null || spriteRenderer == null)
            return;

        // Update sprite if it changed
        if (shadowRenderer.sprite != spriteRenderer.sprite)
        {
            shadowRenderer.sprite = spriteRenderer.sprite;
        }

        shadowObject.transform.localPosition = new Vector3(0f, shadowOffsetY, 0f);

        //position adjustment based on horizontal movement
        // lerp position between -shadowHorizontalMovement to shadowHorizontalMovement based on sun angle
        float sunAngle = shadowRotation.eulerAngles.z;

        // Normalize angle to -180 to 180 range for smooth lerping
        if (sunAngle > 180f)
            sunAngle -= 360f;

        // Convert to 0-1 range: -180 = 0, 180 = 1
        float dividedAngle = (sunAngle + 180f) / 360f;

        float horizontalPosition = Mathf.Lerp(-shadowHorizontalMovement, shadowHorizontalMovement, dividedAngle);
        shadowObject.transform.localPosition += new Vector3(horizontalPosition, 0f, 0f);

        shadowObject.transform.rotation = shadowRotation;

        float shadowLength = shadowDistanceMultiplier * objectHeight;

        float scaleY = Mathf.Lerp(shadowLength, 0.3f, sunElevation);
        shadowObject.transform.localScale = new Vector3(
            transform.localScale.x,
            transform.localScale.y * scaleY,
            transform.localScale.z
        );



        shadowRenderer.color = shadowColor;
        
        // Match flip settings
        shadowRenderer.flipX = spriteRenderer.flipX;
        shadowRenderer.flipY = spriteRenderer.flipY;

    }
    
    void OnDestroy()
    {
        // Clean up shadow object
        if (shadowObject != null)
        {
            if (Application.isPlaying)
            {
                //Destroy(shadowObject);
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
    
}