using UnityEngine;

/// <summary>
/// Full-view overlay: wind-scrolled cloud coverage darkening the scene, warped by a baked world
/// height map, with pixel-art-quantized edges baked into the texture itself (see
/// CloudShadowTextureGenerator). Builds one camera-covering quad and drives
/// Custom/CloudShadowOverlay.shader.
///
/// Cloud POSITION is driven ONLY by wind — never by the sun. Sun elevation only ever affects
/// overall intensity fading at night. The WARP (this file) is a separate effect: it reads local
/// height from WorldHeightCamera and offsets the SAMPLED cloud UV straight along Y (the height
/// map's own vertical axis) — not along wind, not along sun. Sign is controlled by
/// cloudWarpStrength's own sign; flip it negative in the Inspector if the warp reads backwards.
///
/// Building-cast shadows (a separate use of the same height bake, walked toward the sun) were
/// tried twice and dropped both times (2026-08-24, see project doc) — once for a real
/// self-shadowing bug, once because only 6 discrete march steps produced a sparse "outline"
/// artifact instead of a filled shadow (the reference technique used ~150 steps, a real GPU cost
/// this pass isn't taking on). Explicitly out of scope right now, not a bug to fix here — don't
/// reintroduce it without a plan for the step-count/performance tradeoff.
///
/// Not [ExecuteInEditMode] — nothing here is worth per-placement edit-time preview, and it
/// avoids edit-mode Camera.main edge cases.
/// </summary>
public class CloudShadowOverlay : MonoBehaviour
{
    [Header("Cloud Texture")]
    [Tooltip("Tileable cloud shape texture baked with Jarlborn > Generate Cloud Shadow Texture.")]
    public Texture2D cloudNoiseTexture;

    [Header("Wind")]
    [Tooltip("Direction the cloud pattern drifts. Only the direction matters, magnitude is normalized. Does NOT affect the height warp below — that's a separate, fixed-axis effect.")]
    public Vector2 windDirection = new Vector2(1f, 0.35f);
    [Tooltip("World units per second the cloud pattern drifts.")]
    public float windSpeed = 1.5f;

    [Header("Scale")]
    [Tooltip("World-space size, in units, of one tile of the cloud texture. Bigger = larger cloud patches.")]
    public float noiseTileWorldSize = 40f;
    [Tooltip("How far (world units) the sampled cloud pixel shifts along Y where the baked height map reads at its maximum (1.0). Scales down linearly with height — flat ground (height 0) never warps the cloud at all. Negative flips the direction. Requires a WorldHeightCamera in the scene; with none, warp is simply 0 (a console warning fires once).")]
    public float cloudWarpStrength = 4f;

    [Header("Shadow")]
    [Tooltip("Color multiplied onto the scene at full cloud coverage.")]
    public Color shadowColor = new Color(0.55f, 0.62f, 0.78f, 1f);
    [Range(0f, 1f)]
    [Tooltip("Darkness at full coverage, before day/night blending.")]
    public float maxIntensity = 0.45f;

    [Header("Sun Integration")]
    [Tooltip("Fade the effect out at night, using ShadowMaster's sun elevation. If off, or ShadowMaster isn't in the scene, the effect stays at maxIntensity constantly.")]
    public bool fadeAtNight = true;
    [Range(0f, 1f)] public float nightThreshold = 0.4f;
    [Range(0f, 1f)] public float dayThreshold = 0.6f;

    [Header("Camera")]
    [Tooltip("Camera to cover. Defaults to Camera.main if left empty.")]
    public Camera targetCamera;
    [Tooltip("Extra coverage beyond the camera view, as a fraction (0.25 = 25% larger on each side).")]
    public float coverageMargin = 0.25f;

    [Header("Sorting")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 5000;

    [Header("Debug")]
    [Tooltip("Bypass cloud coverage entirely and paint the shader's own heightHere reading directly onto the screen (white = 0, blue = max). Use to confirm height sampling lines up with what's on screen.")]
    public bool debugVisualizeHeight = false;

    private GameObject quadObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh quadMesh;
    private Material material;
    private Vector2 windOffset;
    private bool warnedAboutMissingHeightCamera;

    void OnEnable()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        BuildQuad();
        ApplyStaticMaterialProperties();
    }

    void BuildQuad()
    {
        if (quadObject != null)
            return;

        quadObject = new GameObject(name + "_Quad");
        quadObject.hideFlags = HideFlags.DontSave;
        quadObject.transform.SetParent(transform, false);
        // Pinned to identity regardless of this component's own transform — rendering doesn't
        // depend on it (the shader goes world->clip via VP directly), but Renderer.bounds does,
        // and this keeps that sane. See CloudShadowOverlay.shader's vert() comment.
        quadObject.transform.position = Vector3.zero;
        quadObject.transform.rotation = Quaternion.identity;
        quadObject.transform.localScale = Vector3.one;

        meshFilter = quadObject.AddComponent<MeshFilter>();
        meshRenderer = quadObject.AddComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;

        quadMesh = new Mesh();
        quadMesh.MarkDynamic();
        quadMesh.vertices = new Vector3[4];
        quadMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        quadMesh.uv = new Vector2[4]; // unused — the shader samples world position instead
        // Huge fixed bounds instead of RecalculateBounds() every frame: the vertices are
        // absolute world coordinates (roughly camera-position-sized numbers), which is a strange
        // thing to interpret as LOCAL bounds — this avoids any risk of the quad getting
        // frustum-culled from that mismatch.
        quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1_000_000f);
        meshFilter.mesh = quadMesh;

        var shader = Shader.Find("Custom/CloudShadowOverlay");
        if (shader == null)
            Debug.LogWarning("CloudShadowOverlay: Custom/CloudShadowOverlay shader not found. Falling back to Sprites/Default, which will NOT darken correctly.");
        material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        meshRenderer.sharedMaterial = material;
    }

    void ApplyStaticMaterialProperties()
    {
        if (material == null)
            return;
        material.mainTexture = cloudNoiseTexture;
        material.SetColor("_ShadowColor", shadowColor);
    }

    void OnValidate()
    {
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
        ApplyStaticMaterialProperties();
    }

    void LateUpdate()
    {
        if (quadObject == null)
            BuildQuad();

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null || material == null)
            return;

        float halfHeight = targetCamera.orthographicSize * (1f + coverageMargin);
        float halfWidth = halfHeight * targetCamera.aspect;
        Vector3 c = targetCamera.transform.position;

        Vector3 bl = new Vector3(c.x - halfWidth, c.y - halfHeight, 0f);
        Vector3 br = new Vector3(c.x + halfWidth, c.y - halfHeight, 0f);
        Vector3 tr = new Vector3(c.x + halfWidth, c.y + halfHeight, 0f);
        Vector3 tl = new Vector3(c.x - halfWidth, c.y + halfHeight, 0f);

        quadMesh.vertices = new Vector3[] { bl, br, tr, tl };

        // --- Cloud: wind ONLY. No sun coupling of any kind here. ---
        float tileSize = Mathf.Max(noiseTileWorldSize, 0.01f);
        float deltaTime = Application.isPlaying ? Time.deltaTime : 0f;
        Vector2 windDirNormalized = windDirection.normalized;
        windOffset += windDirNormalized * (windSpeed * deltaTime / tileSize);

        material.SetVector("_Offset", new Vector4(windOffset.x, windOffset.y, 0f, 0f));
        material.SetFloat("_InvTileSize", 1f / tileSize);
        material.SetFloat("_Intensity", CalculateIntensity());
        material.SetFloat("_DebugVisualizeHeight", debugVisualizeHeight ? 1f : 0f);

        var heightCam = WorldHeightCamera.Instance;
        if (heightCam == null || heightCam.HeightTexture == null)
        {
            // Without this, _WorldHeightTex silently stays at the shader's compiled-in default
            // ("black" — height 0 everywhere), so warp just does nothing, with no error at all.
            if (!warnedAboutMissingHeightCamera)
            {
                Debug.LogWarning(heightCam == null
                    ? "CloudShadowOverlay: no WorldHeightCamera in the scene — cloud warp is disabled until one is added."
                    : "CloudShadowOverlay: WorldHeightCamera found but hasn't baked a texture yet.");
                warnedAboutMissingHeightCamera = true;
            }
            material.SetFloat("_CloudWarpUV", 0f);
        }
        else
        {
            float heightMapWorldSize = Mathf.Max(heightCam.worldSize.x, 0.01f); // worldSize should be square — see WorldHeightCamera
            material.SetTexture("_WorldHeightTex", heightCam.HeightTexture);
            material.SetVector("_WorldHeightOrigin", new Vector4(heightCam.worldOrigin.x, heightCam.worldOrigin.y, 0f, 0f));
            material.SetVector("_WorldHeightInvSize", new Vector4(1f / heightMapWorldSize, 1f / heightMapWorldSize, 0f, 0f));
            material.SetFloat("_CloudWarpUV", cloudWarpStrength / tileSize);
        }
    }

    float CalculateIntensity()
    {
        if (!fadeAtNight || ShadowMaster.Instance == null)
            return maxIntensity;

        float sunElevation = ShadowMaster.Instance.GetSunElevation();

        float nightBlend;
        if (sunElevation >= dayThreshold)
            nightBlend = 0f;
        else if (sunElevation <= nightThreshold)
            nightBlend = 1f;
        else
            nightBlend = 1f - Mathf.InverseLerp(nightThreshold, dayThreshold, sunElevation);

        return maxIntensity * (1f - nightBlend);
    }

    void OnDisable()
    {
        if (quadObject != null)
            quadObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (quadObject != null)
        {
            if (Application.isPlaying) Destroy(quadObject);
            else DestroyImmediate(quadObject);
        }
        if (material != null)
        {
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
