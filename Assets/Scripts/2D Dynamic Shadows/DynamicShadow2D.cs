using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Creates dynamic 2D shadows for sprites based on sun position and nearby fire/torch lights.
/// Sun shadows come from ShadowMaster, fire shadows come from ShadowCastingLight components.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicShadow2D : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;
    public float objectHeight = 1f;
    public float shadowOffsetX = 0;
    public float shadowOffsetY = 0;
    public float shadowHorizontalMovement = 0;
    public ShadowMaster shadowMaster;

    [Header("Day/Night Blending")]
    [Tooltip("Sun elevation threshold below which fire shadows are at full intensity")]
    [Range(0f, 1f)]
    public float nightThreshold = 0.4f;
    [Tooltip("Sun elevation threshold above which fire shadows are at minimum intensity")]
    [Range(0f, 1f)]
    public float dayThreshold = 0.6f;

    // Runtime data for auto-registered lights (from ShadowCastingLight)
    private List<ShadowCastingLight> autoLights = new List<ShadowCastingLight>();
    private List<GameObject> autoShadowObjects = new List<GameObject>();
    private List<SpriteRenderer> autoShadowRenderers = new List<SpriteRenderer>();

    private float nightBlendFactor = 0f;

    // One material per instance — the custom shader doesn't get Unity's automatic
    // per-SpriteRenderer texture override (that only applies to built-in sprite
    // shaders), so a shared material would show whichever texture last got bound to it.
    private Material sunShadowMaterial;

    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CleanupDuplicateShadows();
        if (ShadowMaster.Instance != null)
            ShadowMaster.Instance.RegisterShadow(this);
    }

    void Start()
    {
        shadowMaster = ShadowMaster.Instance;
        if (shadowObject == null)
            CreateShadow();
    }

    void CleanupDuplicateShadows()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        int shadowCount = 0;

        foreach (Transform child in children)
        {
            if (child != transform && child.name.Contains("_Shadow"))
            {
                if (child.name.Contains("_AutoShadow_"))
                {
                    continue;
                }

                shadowCount++;

                if (shadowCount == 1 && shadowObject == null)
                {
                    shadowObject = child.gameObject;
                    shadowRenderer = child.GetComponent<SpriteRenderer>();
                }
                else
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }

    void CreateShadow()
    {
        if (shadowObject != null)
            return;

        if (shadowMaster == null)
        {
            shadowMaster = ShadowMaster.Instance;
            if (shadowMaster == null)
            {
                Debug.LogWarning("DynamicShadow2D: No ShadowMaster found in scene. Cannot create shadow.");
                return;
            }
        }

        shadowObject = Instantiate(shadowMaster.shadowPrefab);
        shadowObject.name = gameObject.name + "_Shadow";
        shadowObject.transform.SetParent(transform);
        shadowObject.transform.localPosition = new Vector3(shadowOffsetX, shadowOffsetY, 0f);
        shadowObject.transform.localRotation = Quaternion.identity;
        shadowObject.transform.localScale = Vector3.one;
        shadowObject.hideFlags = HideFlags.DontSave;

        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if(shadowRenderer == null)
        {
            print($"No sprite renderer found on {shadowObject.name}");
        }
        shadowRenderer.sprite = spriteRenderer.sprite;
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder;
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;

        var stencilShader = Shader.Find("Custom/Shadow2DStencilOnce");
        Shader shader = stencilShader != null ? stencilShader : Shader.Find("Sprites/Default");
        sunShadowMaterial = ShadowMaterialCache.Get(shader, spriteRenderer.sprite != null ? spriteRenderer.sprite.texture : null);
        shadowRenderer.sharedMaterial = sunShadowMaterial;
    }

    /// <summary>
    /// Called by ShadowCastingLight when this object enters its range
    /// </summary>
    public void RegisterAutoLight(ShadowCastingLight light)
    {
        if (light == null || autoLights.Contains(light)) return;

        autoLights.Add(light);
        CreateAutoShadowForLight(autoLights.Count - 1);
    }

    /// <summary>
    /// Called by ShadowCastingLight when this object exits its range
    /// </summary>
    public void UnregisterAutoLight(ShadowCastingLight light)
    {
        int index = autoLights.IndexOf(light);
        if (index < 0) return;

        if (index < autoShadowObjects.Count && autoShadowObjects[index] != null)
        {
            if (Application.isPlaying)
                Destroy(autoShadowObjects[index]);
            else
                DestroyImmediate(autoShadowObjects[index]);
        }

        autoLights.RemoveAt(index);
        if (index < autoShadowObjects.Count) autoShadowObjects.RemoveAt(index);
        if (index < autoShadowRenderers.Count) autoShadowRenderers.RemoveAt(index);
    }

    void CreateAutoShadowForLight(int index)
    {
        if (shadowMaster == null) return;

        GameObject autoShadow = Instantiate(shadowMaster.shadowPrefab);
        autoShadow.name = gameObject.name + "_AutoShadow_" + index;
        autoShadow.transform.SetParent(transform);
        autoShadow.transform.localPosition = new Vector3(shadowOffsetX, shadowOffsetY, 0f);
        autoShadow.transform.localRotation = Quaternion.identity;
        autoShadow.transform.localScale = Vector3.one;
        autoShadow.hideFlags = HideFlags.DontSave;

        SpriteRenderer autoRenderer = autoShadow.GetComponent<SpriteRenderer>();
        autoRenderer.sprite = spriteRenderer.sprite;
        autoRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        autoRenderer.sortingLayerID = spriteRenderer.sortingLayerID;


        autoShadowObjects.Add(autoShadow);
        autoShadowRenderers.Add(autoRenderer);
    }

    public void ApplyShadowFromMaster(Color shadowColor, Quaternion shadowRotation, float shadowDistanceMultiplier, float sunElevation, float xScale)
    {
        // After a script-recompile domain reload, ShadowMaster can re-register and update this
        // shadow before this object's own OnEnable has re-run (cross-object order isn't
        // guaranteed), leaving spriteRenderer still null. Re-fetch defensively rather than throw.
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        // Ensure shadow exists (may not have been created if ShadowMaster wasn't ready in OnEnable)
        if (shadowObject == null)
        {
            CreateShadow();
        }

        if(autoShadowRenderers.Count > 0)
            autoShadowRenderers[0].sortingOrder = spriteRenderer.sortingOrder - 1;
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        // Calculate night blend factor (0 = day, 1 = night)
        if (sunElevation >= dayThreshold)
        {
            nightBlendFactor = 0f;
        }
        else if (sunElevation <= nightThreshold)
        {
            nightBlendFactor = 1f;
        }
        else
        {
            nightBlendFactor = 1f - Mathf.InverseLerp(nightThreshold, dayThreshold, sunElevation);
        }

        // Update main sun shadow
        UpdateShadow(shadowObject, shadowRenderer, shadowColor, shadowRotation, shadowDistanceMultiplier, sunElevation, xScale);

        // Update auto-registered light shadows
        UpdateAutoLightShadows(shadowDistanceMultiplier);
    }

    void UpdateAutoLightShadows(float shadowDistanceMultiplier)
    {
        for (int i = 0; i < autoLights.Count; i++)
        {
            var light = autoLights[i];
            if (light == null) continue;
            if (i >= autoShadowObjects.Count || autoShadowObjects[i] == null) continue;

            Vector2 objectPosition = transform.position;
            Vector2 lightPosition = light.transform.position;
            float distance = Vector2.Distance(objectPosition, lightPosition);

            Vector2 directionToLight = (lightPosition - objectPosition).normalized;
            Vector2 shadowDirection = -directionToLight;

            float rotation = Mathf.Atan2(shadowDirection.y, shadowDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion shadowRotation = Quaternion.Euler(0f, 0f, rotation);

            float lightElevation = light.GetLightHeight();
            float scaleReduction = (1f - lightElevation) * 0.3f;
            float xScale = Mathf.Max(1f - scaleReduction, 0.1f);

            Color shadowColor = CalculateAutoLightShadowColor(light, distance);

            UpdateShadow(autoShadowObjects[i], autoShadowRenderers[i], shadowColor, shadowRotation, shadowDistanceMultiplier, lightElevation, xScale);
        }
    }

    Color CalculateAutoLightShadowColor(ShadowCastingLight light, float distance)
    {
        float intensity = light.GetShadowIntensity();

        Light2D light2D = light.GetLight();
        if (light2D != null)
        {
            intensity *= light2D.intensity;
        }

        // Fade based on distance - stronger when close, fades at edge
        float radius = light.GetRadius();
        float distanceFade = 1f - Mathf.Clamp01(distance / radius);
        intensity *= distanceFade;

        // Apply day/night blend - fire shadows always visible but stronger at night
        // nightBlendFactor: 0 = day, 1 = night
        // Lerp from light's daytime intensity to full intensity based on night blend
        float daytimeIntensity = light.GetDaytimeIntensity();
        float dayNightMultiplier = Mathf.Lerp(daytimeIntensity, 1f, nightBlendFactor);
        intensity *= dayNightMultiplier;

        Color shadowColor = Color.black;
        shadowColor.a = Mathf.Clamp01(intensity);

        return shadowColor;
    }

    void UpdateShadow(GameObject shadowObj, SpriteRenderer shadowRend, Color shadowColor, Quaternion shadowRotation, float shadowDistanceMultiplier, float lightElevation, float xScale)
    {
        if (shadowRend == null || spriteRenderer == null || shadowObj == null)
            return;

        if (shadowRend.sprite != spriteRenderer.sprite)
        {
            shadowRend.sprite = spriteRenderer.sprite;

            // Custom shader doesn't auto-follow the sprite's texture like a built-in sprite
            // shader would, so the main sun shadow needs to be repointed at the shared material
            // for the new texture (sunShadowMaterial is cached/shared — never mutate it in place).
            if (shadowRend == shadowRenderer && spriteRenderer.sprite != null)
            {
                Shader shader = sunShadowMaterial != null ? sunShadowMaterial.shader : Shader.Find("Custom/Shadow2DStencilOnce");
                sunShadowMaterial = ShadowMaterialCache.Get(shader, spriteRenderer.sprite.texture);
                shadowRenderer.sharedMaterial = sunShadowMaterial;
            }
        }

        shadowObj.transform.localPosition = new Vector3(shadowOffsetX, shadowOffsetY, 0f);

        float sunAngle = shadowRotation.eulerAngles.z;
        if (sunAngle > 180f)
            sunAngle -= 360f;

        float dividedAngle = (sunAngle + 180f) / 360f;
        float horizontalPosition = Mathf.Lerp(-shadowHorizontalMovement, shadowHorizontalMovement, dividedAngle);
        shadowObj.transform.localPosition += new Vector3(horizontalPosition, 0f, 0f);

        shadowObj.transform.rotation = shadowRotation;

        float shadowLength = shadowDistanceMultiplier * objectHeight;
        float scaleY = Mathf.Lerp(shadowLength, 0.3f, lightElevation);
        shadowObj.transform.localScale = new Vector3(
            transform.localScale.x * xScale,
            transform.localScale.y * scaleY,
            transform.localScale.z
        );

        shadowRend.color = shadowColor;

        shadowRend.flipX = spriteRenderer.flipX;
        shadowRend.flipY = spriteRenderer.flipY;
    }

    void OnDestroy()
    {
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

        // sunShadowMaterial comes from ShadowMaterialCache and is shared with other shadow
        // casters using the same texture — do not destroy it here.

        foreach (var shadow in autoShadowObjects)
        {
            if (shadow != null)
            {
                if (Application.isPlaying)
                    Destroy(shadow);
                else
                    DestroyImmediate(shadow);
            }
        }
        autoShadowObjects.Clear();
        autoShadowRenderers.Clear();
        autoLights.Clear();
    }

    void OnDisable()
    {
        if (shadowObject != null)
            shadowObject.SetActive(false);
        foreach (var shadow in autoShadowObjects)
            if (shadow != null)
                shadow.SetActive(false);
        if (ShadowMaster.Instance != null)
            ShadowMaster.Instance.UnregisterShadow(this);
    }
}
