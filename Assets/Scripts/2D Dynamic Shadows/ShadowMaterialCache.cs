using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shares one Material instance per (shader, texture) pair across all shadow casters.
/// DynamicShadow2D and MeshShadow2D each used to call `new Material(...)` per instance so the
/// custom shader could get a per-object texture — with 100+ shadow casters in a scene that's
/// 100+ unique materials, which defeats Unity's sprite/mesh batching even for shadows cast by
/// visually-identical sprites (e.g. many trees using the same texture). Routing through this
/// cache means identical-texture shadows share a Material again and can batch.
/// </summary>
public static class ShadowMaterialCache
{
    private static readonly Dictionary<(Shader shader, Texture texture), Material> cache = new();

    public static Material Get(Shader shader, Texture texture)
    {
        if (shader == null)
            return null;

        Texture key = texture != null ? texture : Texture2D.whiteTexture;
        var cacheKey = (shader, key);

        if (cache.TryGetValue(cacheKey, out Material mat) && mat != null)
            return mat;

        mat = new Material(shader) { mainTexture = key, enableInstancing = true };
        cache[cacheKey] = mat;
        return mat;
    }

    public static void Clear() => cache.Clear();
}
