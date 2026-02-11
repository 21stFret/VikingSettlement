using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Attach to a Light2D (fire, torch, etc.) to make nearby objects cast shadows from this light.
/// Uses a trigger collider to efficiently detect shadow casters entering/leaving range.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class ShadowCastingLight : MonoBehaviour
{
    [Header("Shadow Settings")]
    [Tooltip("Height of this light for shadow calculations (0-1, lower = longer shadows)")]
    [Range(0f, 1f)]
    public float lightHeight = 0.3f;
    [Tooltip("Shadow intensity multiplier at closest distance")]
    [Range(0f, 1f)]
    public float shadowIntensity = 0.5f;
    [Tooltip("Shadow intensity during daytime (0 = invisible, 1 = same as night)")]
    [Range(0f, 1f)]
    public float daytimeIntensity = 0.3f;

    private Light2D light2D;
    private CircleCollider2D triggerCollider;
    private HashSet<DynamicShadow2D> registeredShadows = new HashSet<DynamicShadow2D>();

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        SetupTrigger();
    }

    void Start()
    {
        // Find objects already in range at start
        FindObjectsInRange();
    }

    void SetupTrigger()
    {
        // Find or create trigger collider
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true;
        SyncRadiusToLight();
    }

    void SyncRadiusToLight()
    {
        if (light2D != null && triggerCollider != null)
        {
            triggerCollider.radius = light2D.pointLightOuterRadius;
        }
    }

    void FindObjectsInRange()
    {
        if (light2D == null) return;

        float radius = light2D.pointLightOuterRadius;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var collider in colliders)
        {
            DynamicShadow2D shadow = collider.GetComponent<DynamicShadow2D>();
            if (shadow != null && !registeredShadows.Contains(shadow))
            {
                shadow.RegisterAutoLight(this);
                registeredShadows.Add(shadow);
            }
        }
    }

    void OnValidate()
    {
        if (light2D == null)
        {
            light2D = GetComponent<Light2D>();
        }
        SyncRadiusToLight();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        DynamicShadow2D shadow = other.GetComponent<DynamicShadow2D>();
        if (shadow != null && !registeredShadows.Contains(shadow))
        {
            shadow.RegisterAutoLight(this);
            registeredShadows.Add(shadow);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        DynamicShadow2D shadow = other.GetComponent<DynamicShadow2D>();
        if (shadow != null && registeredShadows.Contains(shadow))
        {
            shadow.UnregisterAutoLight(this);
            registeredShadows.Remove(shadow);
        }
    }

    void OnDisable()
    {
        // Unregister from all shadows when disabled
        foreach (var shadow in registeredShadows)
        {
            if (shadow != null)
            {
                shadow.UnregisterAutoLight(this);
            }
        }
        registeredShadows.Clear();
    }

    void OnDestroy()
    {
        // Unregister from all shadows when destroyed
        foreach (var shadow in registeredShadows)
        {
            if (shadow != null)
            {
                shadow.UnregisterAutoLight(this);
            }
        }
        registeredShadows.Clear();
    }

    public Light2D GetLight() => light2D;
    public float GetLightHeight() => lightHeight;
    public float GetShadowIntensity() => shadowIntensity;
    public float GetDaytimeIntensity() => daytimeIntensity;
    public float GetRadius() => light2D != null ? light2D.pointLightOuterRadius : 5f;
}
