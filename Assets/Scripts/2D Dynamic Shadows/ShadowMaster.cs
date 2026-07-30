using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ShadowMaster : MonoBehaviour
{
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

    [Tooltip("How much the shadow's x-scale shrinks as sun gets lower (0 = no shrink, 1 = maximum shrink)")]
    [Range(0f, 1f)]
    public float shadowXScaleShrinkAmount = 0.3f;

    [Header("Performance")]
    [Tooltip("Update shadows every frame")]
    public bool autoUpdate = true;

    // Cached values, computed once per frame and shared across all shadows
    private Vector2 shadowDirection;
    private float sunElevation;
    private Color baseShadowColor;
    private float baseShadowAlpha;
    private float shadowXScale;
    private Quaternion shadowQuaternion;

    private List<DynamicShadow2D> shadows = new List<DynamicShadow2D>();

    public GameObject shadowPrefab;
    public float shadowFadeSpeed = 1f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            DestroyImmediate(this);
            return;
        }
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnValidate()
    {
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
        if (autoUpdate)
        {
            CalculateShadowProperties();
            ApplyToAllShadows();
        }
    }

    void CalculateShadowProperties()
    {
        if (sunTransform != null)
        {
            sunPosition = sunTransform.position;
            sunElevation = Mathf.Clamp01(sunTransform.position.z / 10f);
        }
        else
        {
            sunElevation = sunHeight;
        }

        Vector2 directionToSun = sunPosition.normalized;
        shadowDirection = new Vector2(-directionToSun.x, -directionToSun.y);

        float angle = Mathf.Atan2(shadowDirection.y, shadowDirection.x) * Mathf.Rad2Deg - 90f;
        shadowQuaternion = Quaternion.Euler(0f, 0f, angle);

        baseShadowColor = Color.Lerp(Color.black, Color.white, shadowDarkness);

        float targetAlpha = sunElevation < minSunHeightForShadows
            ? 0f
            : Mathf.Lerp(shadowIntensity, 0.01f, sunElevation);

        baseShadowAlpha = Mathf.MoveTowards(baseShadowAlpha, targetAlpha, Time.deltaTime * shadowFadeSpeed);
        baseShadowColor.a = baseShadowAlpha;

        shadowXScale = CalculateShadowXScale();
    }

    float CalculateShadowXScale()
    {
        float scaleReduction = (1f - sunElevation) * shadowXScaleShrinkAmount;
        return Mathf.Max(1f - scaleReduction, 0.1f);
    }

    void ApplyToAllShadows()
    {
        for (int i = shadows.Count - 1; i >= 0; i--)
        {
            if (shadows[i] == null) { shadows.RemoveAt(i); continue; }
            shadows[i].ApplyShadowFromMaster(baseShadowColor, shadowQuaternion, shadowDistanceMultiplier, sunElevation, shadowXScale);
        }
    }

    // Finds and registers all DynamicShadow2D components in the scene.
    // Called at startup and from OnValidate in editor. Dynamic objects self-register via OnEnable.
    public void RefreshShadows()
    {
        shadows.Clear();
        DynamicShadow2D[] foundShadows = FindObjectsByType<DynamicShadow2D>();
        foreach (DynamicShadow2D shadow in foundShadows)
            RegisterShadow(shadow);
    }

    public void RegisterShadow(DynamicShadow2D shadow)
    {
        if (shadow == null || shadows.Contains(shadow))
            return;
        shadows.Add(shadow);
        shadow.shadowMaster = this;
    }

    public void UnregisterShadow(DynamicShadow2D shadow)
    {
        shadows.Remove(shadow);
    }

    public void ForceUpdateAllShadows()
    {
        CalculateShadowProperties();
        ApplyToAllShadows();
    }

    public int GetShadowCount() => shadows.Count;

    public Vector2 GetShadowDirection() => shadowDirection;

    public float GetSunElevation() => sunElevation;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(sunPosition.x, sunPosition.y, 0), 0.5f);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawLine(new Vector3(sunPosition.x, sunPosition.y, 0), new Vector3(sunPosition.x, sunPosition.y, sunHeight * 10f));
    }
}
