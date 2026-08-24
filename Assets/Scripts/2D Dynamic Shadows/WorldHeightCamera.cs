using UnityEngine;

/// <summary>
/// Bakes ONE grayscale height texture covering the level's buildable area, by rendering every
/// WorldHeightContributor's silhouette (on a dedicated culling layer) with an orthographic
/// camera into a RenderTexture. This is what CloudShadowOverlay.shader reads for both the
/// building self/cross-shadow walk AND the cloud warp — "both systems use the height map."
///
/// Not baked every frame — that would be pointless cost for something that only changes when
/// buildings go up or down. WorldHeightContributor calls RequestRebake() on enable/disable/
/// destroy; this camera re-renders at most once per minRebakeInterval so a burst of
/// enable/disable calls in one frame (e.g. scene load) doesn't trigger a bake per object.
///
/// Requires a Unity Layer named "WorldHeight" (see HeightLayerName) — add it in Project
/// Settings > Tags and Layers. Can't be created from a script safely, so this is a one-time
/// manual step; both this camera and WorldHeightContributor log a warning if it's missing.
/// </summary>
public class WorldHeightCamera : MonoBehaviour
{
    public const string HeightLayerName = "WorldHeight";

    public static WorldHeightCamera Instance { get; private set; }

    [Header("Coverage")]
    [Tooltip("World-space bottom-left corner of the area the height bake covers.")]
    public Vector2 worldOrigin = new Vector2(-50f, -50f);
    [Tooltip("World-space size of the covered area. Should be square — orthographicSize only maps to worldSize.y; a non-square value will stretch the bake. Make it comfortably larger than your buildable area.")]
    public Vector2 worldSize = new Vector2(100f, 100f);

    [Header("Resolution")]
    public int textureResolution = 512;

    [Header("Rebake")]
    [Tooltip("Rebake at most this often even if requested more frequently, in seconds. Guards against many objects enabling/disabling in the same frame each triggering their own request.")]
    public float minRebakeInterval = 0.25f;

    /// <summary>
    /// The baked height texture — two independent channels, see WorldHeightStamp.shader:
    /// R = base-to-roof gradient (cloud warp reads this), G = flat per-object height, no
    /// gradient (building shadow march reads this, to stay immune to self-shadowing). Both are
    /// 0 = nothing contributed there, up to 1 = tallest tagged object.
    /// </summary>
    public RenderTexture HeightTexture { get; private set; }

    /// <summary>True once Bake() has actually run at least once. False forever means the layer/contributor setup never fired a successful render — see WorldHeightDebugView.</summary>
    public bool HasBakedAtLeastOnce { get; private set; }

    private Camera captureCamera;
    private static bool rebakeRequested;
    private float lastBakeTime = -999f;

    void OnEnable()
    {
        Instance = this;
        BuildCamera();
        rebakeRequested = true; // always bake once on scene start, before any contributor's own request would fire
    }

    void BuildCamera()
    {
        if (captureCamera != null)
            return;

        // ARGB32 rather than a single-channel format for broad compatibility across Unity/
        // platform versions — only .r is ever read, the rest is unused padding.
        HeightTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32)
        {
            name = "WorldHeightMap",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var camObject = new GameObject("WorldHeightCaptureCamera");
        camObject.hideFlags = HideFlags.DontSave;
        camObject.transform.SetParent(transform, false);

        captureCamera = camObject.AddComponent<Camera>();
        captureCamera.enabled = false; // never runs Unity's automatic per-frame render — Bake() calls Render() manually
        captureCamera.orthographic = true;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = Color.black; // black = height 0 = "nothing here" everywhere nothing was stamped
        captureCamera.targetTexture = HeightTexture;

        int layer = LayerMask.NameToLayer(HeightLayerName);
        captureCamera.cullingMask = layer >= 0 ? (1 << layer) : 0;
        if (layer < 0)
            Debug.LogWarning($"WorldHeightCamera: layer '{HeightLayerName}' doesn't exist yet. Add it in Project Settings > Tags and Layers — until then this bakes an empty (all-zero) height map.");
    }

    /// <summary>Called by WorldHeightContributor whenever a contributing object enables, disables, or is destroyed.</summary>
    public static void RequestRebake()
    {
        rebakeRequested = true;
    }

    void LateUpdate()
    {
        if (!rebakeRequested)
            return;
        if (Application.isPlaying && Time.time - lastBakeTime < minRebakeInterval)
            return;

        Bake();
    }

    void Bake()
    {
        if (captureCamera == null)
            BuildCamera();
        if (captureCamera == null)
            return;

        captureCamera.transform.position = new Vector3(
            worldOrigin.x + worldSize.x * 0.5f,
            worldOrigin.y + worldSize.y * 0.5f,
            -10f);
        captureCamera.orthographicSize = worldSize.y * 0.5f;

        captureCamera.Render();

        rebakeRequested = false;
        lastBakeTime = Time.time;
        HasBakedAtLeastOnce = true;
    }

    /// <summary>World position -> [0,1] UV into HeightTexture, for CloudShadowOverlay to feed the shader.</summary>
    public Vector2 WorldToHeightUV(Vector2 worldPos)
    {
        return new Vector2(
            (worldPos.x - worldOrigin.x) / Mathf.Max(worldSize.x, 0.01f),
            (worldPos.y - worldOrigin.y) / Mathf.Max(worldSize.y, 0.01f));
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnDestroy()
    {
        if (HeightTexture != null)
            HeightTexture.Release();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(worldOrigin.x + worldSize.x * 0.5f, worldOrigin.y + worldSize.y * 0.5f, 0f);
        Gizmos.DrawWireCube(center, new Vector3(worldSize.x, worldSize.y, 0f));
    }
}
