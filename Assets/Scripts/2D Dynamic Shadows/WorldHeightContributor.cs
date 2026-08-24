using UnityEngine;

/// <summary>
/// Makes this object part of the level's baked height map (see WorldHeightCamera), which both
/// the building self/cross-shadow walk AND the cloud warp read from. Attach to buildings, big
/// trees — anything tall enough that you want it casting a real shadow on the ground/other
/// buildings, or visibly deforming the cloud pattern as it drifts overhead.
///
/// This is the "auto silhouette + scalar" approach: the object's existing sprite alpha shape is
/// reused as-is, stamped with a straight-line gradient from 0 at the bottom of the sprite up to
/// <see cref="height"/> at the top (see WorldHeightStamp.shader) — cheap, no new art, but still
/// no real per-pixel shape detail: an L-shaped roof or a chimney still just falls on that same
/// straight ramp, not its own height, and a flipped/rotated sprite's "top" is still just its own
/// texture-space top. That's a deliberate simplification, not an oversight — hand-painting a true
/// per-pixel height/normal sprite per building (matching a 3D-rendered asset pipeline's depth
/// output) would look better but is real, ongoing art production work this project's 2D pipeline
/// doesn't already produce for free. Revisit only if the gradient still reads as visibly wrong in
/// practice, not pre-emptively.
///
/// Superseded CloudShadowReceiver.cs, which drove per-object cloud parallax directly from a
/// hand-set height field on each object. Delete that file — this one replaces it entirely, with
/// height now feeding a real shared height map instead of a per-object shader trick.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WorldHeightContributor : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("Height at the TOP of this sprite (roofline) — the base ramps down to 0. Try ~0.3-0.5 for house rooftops, higher for towers/tall trees. 0 contributes nothing (same as not having this component).")]
    public float height = 0.4f;

    private SpriteRenderer sourceRenderer;
    private GameObject heightObject;
    private SpriteRenderer heightRenderer;
    private Material material;

    void OnEnable()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        BuildHeightSprite();
        WorldHeightCamera.RequestRebake();
    }

    void BuildHeightSprite()
    {
        if (heightObject != null)
            return;

        int layer = LayerMask.NameToLayer(WorldHeightCamera.HeightLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"WorldHeightContributor on {name}: layer '{WorldHeightCamera.HeightLayerName}' doesn't exist. Add it in Project Settings > Tags and Layers, or this object won't contribute to the world height bake.");
            return;
        }

        heightObject = new GameObject(name + "_Height");
        heightObject.hideFlags = HideFlags.DontSave;
        heightObject.layer = layer;
        heightObject.transform.SetParent(transform, false);

        heightRenderer = heightObject.AddComponent<SpriteRenderer>();
        heightRenderer.sprite = sourceRenderer.sprite;
        heightRenderer.flipX = sourceRenderer.flipX;
        heightRenderer.flipY = sourceRenderer.flipY;

        var shader = Shader.Find("Custom/WorldHeightStamp");
        if (shader == null)
            Debug.LogWarning("WorldHeightContributor: Custom/WorldHeightStamp shader not found — this object won't stamp correctly into the height bake.");
        material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        material.SetColor("_Color", new Color(height, height, height, 1f));
        heightRenderer.sharedMaterial = material;
    }

    void LateUpdate()
    {
        // Keep the height silhouette's sprite in sync if the source is animated/swapped.
        if (heightRenderer != null && sourceRenderer != null && heightRenderer.sprite != sourceRenderer.sprite)
        {
            heightRenderer.sprite = sourceRenderer.sprite;
            WorldHeightCamera.RequestRebake();
        }
    }

    void OnValidate()
    {
        if (material != null)
            material.SetColor("_Color", new Color(height, height, height, 1f));
    }

    void OnDisable()
    {
        if (heightObject != null)
            heightObject.SetActive(false);
        WorldHeightCamera.RequestRebake();
    }

    void OnDestroy()
    {
        if (heightObject != null)
        {
            if (Application.isPlaying) Destroy(heightObject);
            else DestroyImmediate(heightObject);
        }
        if (material != null)
        {
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
        WorldHeightCamera.RequestRebake();
    }
}
