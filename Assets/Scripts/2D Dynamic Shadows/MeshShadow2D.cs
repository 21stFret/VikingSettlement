using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Builds a parallelogram shadow mesh each frame.
/// Uses sprite.vertices (the tight polygon) to find the real visual bounds,
/// so transparent padding around the sprite does not affect shadow placement.
/// Supports sun shadows via ShadowMaster and fire/torch shadows via ShadowCastingLight.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class MeshShadow2D : MonoBehaviour
{
    [Tooltip("Shadow length multiplier relative to the sprite's visual height. 1 = one sprite-height long.")]
    public float objectHeight = 1f;

    [Tooltip("Nudge the shadow base X in world units")]
    public float shadowOffsetX = 0f;

    [Tooltip("Nudge the shadow base Y in world units")]
    public float shadowOffsetY = 0f;

    [Header("Day/Night Blending")]
    [Tooltip("Sun elevation below which fire shadows are at full intensity")]
    [Range(0f, 1f)]
    public float nightThreshold = 0.4f;
    [Tooltip("Sun elevation above which fire shadows are at minimum intensity")]
    [Range(0f, 1f)]
    public float dayThreshold = 0.6f;

    private SpriteRenderer spriteRenderer;

    // Sun shadow
    private GameObject shadowObject;
    private MeshFilter shadowMeshFilter;
    private MeshRenderer shadowMeshRenderer;
    private Mesh shadowMesh;
    private Material shadowMaterial;
    private float currentAlpha = 0f;

    // Fire/torch light shadows
    private List<ShadowCastingLight> autoLights      = new List<ShadowCastingLight>();
    private List<GameObject>         autoShadowObjects = new List<GameObject>();
    private List<Mesh>               autoMeshes       = new List<Mesh>();
    private List<Material>           autoMaterials    = new List<Material>();

    private Sprite lastSprite;

    // Cached tight visual bounds in sprite local space (from sprite.vertices)
    private float localMinX, localMaxX, localMinY, localMaxY;
    private Vector3[] cachedVerts = new Vector3[4];
    private Color[] cachedColors = new Color[4];

    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Sprite s = spriteRenderer != null ? spriteRenderer.sprite : null;
        if (s == null) return;

        Rect tr = s.textureRect;
        Vector2[] verts = s.vertices;
        if (verts.Length == 0) return;

        float ppu   = s.pixelsPerUnit;
        float rectW = tr.width  / ppu;
        float rectH = tr.height / ppu;

        float vMinX = verts[0].x, vMaxX = verts[0].x;
        float vMinY = verts[0].y, vMaxY = verts[0].y;
        for (int i = 1; i < verts.Length; i++)
        {
            if (verts[i].x < vMinX) vMinX = verts[i].x;
            if (verts[i].x > vMaxX) vMaxX = verts[i].x;
            if (verts[i].y < vMinY) vMinY = verts[i].y;
            if (verts[i].y > vMaxY) vMaxY = verts[i].y;
        }

        float thresholdX = rectW * 0.9f;
        float thresholdY = rectH * 0.9f;
        if ((vMaxX - vMinX) < thresholdX || (vMaxY - vMinY) < thresholdY)
        {
            Debug.LogWarning(
                $"[MeshShadow2D] '{gameObject.name}': sprite '{s.name}' appears to have " +
                $"transparent whitespace padding. Crop the PNG to remove whitespace, or set " +
                $"Mesh Type to Tight in the sprite import settings.", this);
        }
    }
#endif

    // -------------------------------------------------------------------------

    void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetupShadowObject();
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetupShadowObject();
    }

    void SetupShadowObject()
    {
        if (shadowObject != null)
            return;

        if (ShadowMaster.Instance == null)
            return;

        shadowObject = new GameObject(name + "_MeshShadow");
        shadowObject.hideFlags = HideFlags.DontSave;

        shadowMeshFilter   = shadowObject.AddComponent<MeshFilter>();
        shadowMeshRenderer = shadowObject.AddComponent<MeshRenderer>();

        shadowObject.transform.SetParent(transform, false);

        shadowMesh = new Mesh();
        shadowMesh.MarkDynamic();
        shadowMesh.vertices  = new Vector3[4];
        shadowMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        shadowMesh.uv        = new Vector2[4];
        shadowMeshFilter.mesh = shadowMesh;

        var stencilShader = Shader.Find("Custom/Shadow2DStencilOnce");
        shadowMaterial = new Material(stencilShader != null ? stencilShader : Shader.Find("Sprites/Default"));
        shadowMeshRenderer.sharedMaterial = shadowMaterial;
        shadowMeshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        shadowMeshRenderer.sortingOrder   = spriteRenderer.sortingOrder - 1;
    }

    // -------------------------------------------------------------------------

    /// <summary>Called by ShadowCastingLight when this object enters its range.</summary>
    public void RegisterAutoLight(ShadowCastingLight light)
    {
        if (light == null || autoLights.Contains(light)) return;
        autoLights.Add(light);
        CreateAutoShadowMesh(autoLights.Count - 1);
    }

    /// <summary>Called by ShadowCastingLight when this object exits its range.</summary>
    public void UnregisterAutoLight(ShadowCastingLight light)
    {
        int index = autoLights.IndexOf(light);
        if (index < 0) return;

        if (index < autoShadowObjects.Count && autoShadowObjects[index] != null)
        {
            if (Application.isPlaying) Destroy(autoShadowObjects[index]);
            else                       DestroyImmediate(autoShadowObjects[index]);
        }

        autoLights.RemoveAt(index);
        if (index < autoShadowObjects.Count) autoShadowObjects.RemoveAt(index);
        if (index < autoMeshes.Count)        autoMeshes.RemoveAt(index);
        if (index < autoMaterials.Count)     autoMaterials.RemoveAt(index);
    }

    void CreateAutoShadowMesh(int index)
    {
        var go = new GameObject(name + "_MeshAutoShadow_" + index);
        go.hideFlags = HideFlags.DontSave;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        var m = new Mesh();
        m.MarkDynamic();
        m.vertices  = new Vector3[4];
        m.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        m.uv = shadowMesh != null ? shadowMesh.uv : new Vector2[4];
        mf.mesh = m;

        var mat = new Material(Shader.Find("Sprites/Default"));
        if (shadowMaterial != null) mat.mainTexture = shadowMaterial.mainTexture;
        mr.sharedMaterial = mat;
        mr.sortingLayerID = spriteRenderer.sortingLayerID;
        mr.sortingOrder   = spriteRenderer.sortingOrder - 2 - index;

        autoShadowObjects.Add(go);
        autoMeshes.Add(m);
        autoMaterials.Add(mat);
    }

    // -------------------------------------------------------------------------

    void UpdateSpriteData(Sprite s)
    {
        Rect tr  = s.textureRect;
        float tw = s.texture.width;
        float th = s.texture.height;

        float u0 = tr.x / tw;
        float v0 = tr.y / th;
        float u1 = (tr.x + tr.width)  / tw;
        float v1 = (tr.y + tr.height) / th;

        var uvs = new Vector2[]
        {
            new Vector2(u0, v0),
            new Vector2(u1, v0),
            new Vector2(u1, v1),
            new Vector2(u0, v1),
        };

        shadowMesh.uv = uvs;
        shadowMaterial.mainTexture = s.texture;

        // Sync UVs and texture to all auto light shadow meshes
        foreach (var m   in autoMeshes)    m.uv = uvs;
        foreach (var mat in autoMaterials) mat.mainTexture = s.texture;

        // Tight visual bounds from sprite.vertices
        Vector2[] sv = s.vertices;
        localMinX = localMaxX = sv[0].x;
        localMinY = localMaxY = sv[0].y;
        for (int i = 1; i < sv.Length; i++)
        {
            if (sv[i].x < localMinX) localMinX = sv[i].x;
            if (sv[i].x > localMaxX) localMaxX = sv[i].x;
            if (sv[i].y < localMinY) localMinY = sv[i].y;
            if (sv[i].y > localMaxY) localMaxY = sv[i].y;
        }

        lastSprite = s;
    }

    // -------------------------------------------------------------------------

    void LateUpdate()
    {
        var master = ShadowMaster.Instance;
        if (master == null)
            return;

        if (shadowObject == null)
        {
            SetupShadowObject();
            if (shadowObject == null)
                return;
        }

        if (!shadowObject.activeSelf)
            shadowObject.SetActive(true);

        // Shadow objects live at world origin — vertex positions are world positions.
        shadowObject.transform.position   = Vector3.zero;
        shadowObject.transform.rotation   = Quaternion.identity;
        shadowObject.transform.localScale = Vector3.one;

        Sprite s = spriteRenderer.sprite;
        if (s == null)
            return;

        if (s != lastSprite)
            UpdateSpriteData(s);

        // Tight visual corners in world space
        Vector3 bl_world = transform.TransformPoint(new Vector3(localMinX, localMinY, 0f));
        Vector3 br_world = transform.TransformPoint(new Vector3(localMaxX, localMinY, 0f));

        Vector3 offset = new Vector3(shadowOffsetX, shadowOffsetY, 0f);
        bl_world += offset;
        br_world += offset;

        float worldHeight = Mathf.Abs(transform.lossyScale.y) * (localMaxY - localMinY);

        // --- Sun shadow ---
        float sunHeight = master.GetSunHeight();
        float sunElevation = master.GetSunElevation();
        Vector2 shadowDir  = master.GetShadowDirection();

        float sunExtension = Mathf.Lerp(
            master.shadowDistanceMultiplier * Mathf.Max(0f, objectHeight) * worldHeight,
            0f, sunHeight);

        Vector3 sunVec = new Vector3(shadowDir.x, shadowDir.y, 0f) * sunExtension;

        cachedVerts[0] = bl_world;
        cachedVerts[1] = br_world;
        cachedVerts[2] = br_world + sunVec;
        cachedVerts[3] = bl_world + sunVec;


        shadowMesh.vertices = cachedVerts;
        shadowMesh.RecalculateBounds();

        float targetAlpha = sunElevation < master.minSunHeightForShadows
            ? 0f
            : Mathf.Lerp(0.2f, master.shadowIntensity, sunElevation);

        float deltaTime = Application.isPlaying ? Time.deltaTime : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, deltaTime * master.shadowFadeSpeed);

        Color sunColor = Color.black;
        sunColor.a = currentAlpha;
        cachedColors[0] = sunColor;
        cachedColors[1] = sunColor;
        cachedColors[2] = sunColor;
        cachedColors[3] = sunColor;

        shadowMesh.colors = cachedColors;

        // --- Fire/torch light shadows ---
        if (Application.isPlaying)
            UpdateAutoLightShadows(sunHeight, bl_world, br_world, worldHeight, master);
    }

    void UpdateAutoLightShadows(float sunElevation, Vector3 bl_world, Vector3 br_world,
                                float worldHeight, ShadowMaster master)
    {
        // 0 = full day, 1 = full night
        float nightBlend;
        if      (sunElevation >= dayThreshold)   nightBlend = 0f;
        else if (sunElevation <= nightThreshold) nightBlend = 1f;
        else nightBlend = 1f - Mathf.InverseLerp(nightThreshold, dayThreshold, sunElevation);

        for (int i = 0; i < autoLights.Count; i++)
        {
            var light = autoLights[i];
            if (light == null || i >= autoShadowObjects.Count || autoShadowObjects[i] == null)
                continue;

            autoShadowObjects[i].transform.position   = Vector3.zero;
            autoShadowObjects[i].transform.rotation   = Quaternion.identity;
            autoShadowObjects[i].transform.localScale = Vector3.one;

            if (!autoShadowObjects[i].activeSelf)
                autoShadowObjects[i].SetActive(true);

            Vector2 toLight    = (Vector2)(light.transform.position - transform.position);
            float   distance   = toLight.magnitude;
            Vector2 shadowDir  = -toLight.normalized;

            float lightElevation = light.GetLightHeight();
            float extension = Mathf.Lerp(
                master.shadowDistanceMultiplier * Mathf.Max(0f, objectHeight) * worldHeight,
                0f, lightElevation);

            Vector3 sv = new Vector3(shadowDir.x, shadowDir.y, 0f) * extension;

            autoMeshes[i].vertices = new Vector3[]
            {
                bl_world, br_world, br_world + sv, bl_world + sv
            };
            autoMeshes[i].RecalculateBounds();

            // Intensity: light strength × distance falloff × day/night blend
            float intensity = light.GetShadowIntensity();
            Light2D l2d = light.GetLight();
            if (l2d != null) intensity *= l2d.intensity;

            float radius = light.GetRadius();
            intensity *= 1f - Mathf.Clamp01(distance / radius);
            intensity *= Mathf.Lerp(light.GetDaytimeIntensity(), 1f, nightBlend);

            Color c = Color.black;
            c.a = Mathf.Clamp01(intensity);
            autoMeshes[i].colors = new Color[] { c, c, c, c };
        }
    }

    // -------------------------------------------------------------------------

    void OnDisable()
    {
        if (shadowObject != null)
            shadowObject.SetActive(false);

        foreach (var go in autoShadowObjects)
            if (go != null) go.SetActive(false);
    }

    void OnDestroy()
    {
        DestroyObject(shadowObject);
        foreach (var go in autoShadowObjects) DestroyObject(go);
        autoShadowObjects.Clear();
        autoMeshes.Clear();
        autoMaterials.Clear();
        autoLights.Clear();
    }

    void DestroyObject(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else                       DestroyImmediate(go);
    }
}
