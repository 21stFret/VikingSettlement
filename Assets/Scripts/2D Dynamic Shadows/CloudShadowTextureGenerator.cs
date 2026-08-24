#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that bakes a seamless, pixel-art-friendly cloud shape texture.
/// Menu: Jarlborn > Generate Cloud Shadow Texture
///
/// This is a flat coverage/shape mask (0 = clear sky, up to 1 = thickest cloud) sampled ONCE per
/// receiver at runtime — CloudShadowOverlay.shader does not march or search through it. The
/// "clouds sliding over rooftops differently than over the ground" effect comes from WHERE each
/// receiver samples this texture (a ray/plane offset that depends on the receiver's height — see
/// CloudShadowOverlay.shader and CloudShadowReceiver.cs), not from anything baked into the
/// texture itself.
///
/// Produces fractal value noise (same layered-blob character as Perlin noise, and trivially
/// exact-tileable by construction — see TileableValueNoise), then quantizes it into a handful
/// of hard-edged bands via ApplySteppedBanding so it reads as pixel art rather than a smooth
/// photographic gradient. Re-run any time to try a new seed/threshold without touching code —
/// this only ever writes the one PNG at Output Path.
/// </summary>
public class CloudShadowTextureGenerator : EditorWindow
{
    private int resolution = 256;
    private int basePeriod = 4;
    private int octaves = 4;
    private float persistence = 0.5f;
    private float threshold = 0.55f;
    private int stepCount = 3;
    private int seed = 12345;
    private string outputPath = "Assets/Art/CloudShadowNoise.png";

    [MenuItem("Jarlborn/Generate Cloud Shadow Texture")]
    public static void ShowWindow()
    {
        GetWindow<CloudShadowTextureGenerator>("Cloud Shadow Texture");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Noise", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntPopup("Resolution", resolution,
            new[] { "128", "256", "512" }, new[] { 128, 256, 512 });
        basePeriod = EditorGUILayout.IntSlider("Base Period (largest blobs)", basePeriod, 2, 16);
        octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 6);
        persistence = EditorGUILayout.Slider("Persistence", persistence, 0.1f, 0.9f);
        seed = EditorGUILayout.IntField("Seed", seed);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
        threshold = EditorGUILayout.Slider("Threshold (higher = clearer sky)", threshold, 0f, 1f);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Pixel Art Quantization", EditorStyles.boldLabel);
        stepCount = EditorGUILayout.IntSlider("Height Bands Above Threshold", stepCount, 0, 8);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("Path", outputPath);

        EditorGUILayout.Space(12);
        if (GUILayout.Button("Generate", GUILayout.Height(32)))
            Generate();

        EditorGUILayout.HelpBox(
            "Assign the resulting texture to CloudShadowOverlay's Cloud Noise Texture field. " +
            "Re-generate any time — it always overwrites the same file at Output Path.",
            MessageType.Info);
    }

    private void Generate()
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
        var pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float fbm = FractalNoise(x, y);
                float height = ApplySteppedBanding(fbm, threshold, stepCount);
                pixels[y * resolution + x] = new Color(height, height, height, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        string dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(outputPath, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(outputPath);
        var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[CloudShadowTextureGenerator] Wrote {outputPath} ({resolution}x{resolution}, seed {seed}).");
    }

    private float FractalNoise(int px, int py)
    {
        float value = 0f, amplitude = 1f, amplitudeSum = 0f;
        int period = basePeriod;
        for (int o = 0; o < octaves; o++)
        {
            float nx = (float)px / resolution * period;
            float ny = (float)py / resolution * period;
            value += TileableValueNoise(nx, ny, period, seed + o * 1013) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= persistence;
            period *= 2;
        }
        return amplitudeSum > 0f ? value / amplitudeSum : 0f;
    }

    // Exactly tileable by construction: the lattice hash is evaluated on integer coordinates
    // wrapped mod `period`, so the value approaching the right/top edge of one tile always
    // matches the value leaving the left/bottom edge of the next — no torus-sampling or
    // seam-blending trick needed, and it holds for any resolution/period combination.
    private static float TileableValueNoise(float x, float y, int period, int seed)
    {
        int xi0 = ((int)Mathf.Floor(x)) % period; if (xi0 < 0) xi0 += period;
        int yi0 = ((int)Mathf.Floor(y)) % period; if (yi0 < 0) yi0 += period;
        int xi1 = (xi0 + 1) % period;
        int yi1 = (yi0 + 1) % period;

        float tx = x - Mathf.Floor(x);
        float ty = y - Mathf.Floor(y);
        float sx = tx * tx * (3f - 2f * tx);
        float sy = ty * ty * (3f - 2f * ty);

        float v00 = Hash(xi0, yi0, seed);
        float v10 = Hash(xi1, yi0, seed);
        float v01 = Hash(xi0, yi1, seed);
        float v11 = Hash(xi1, yi1, seed);

        float ix0 = Mathf.Lerp(v00, v10, sx);
        float ix1 = Mathf.Lerp(v01, v11, sx);
        return Mathf.Lerp(ix0, ix1, sy);
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 2147483647;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x00FFFFFF) / (float)0x01000000;
        }
    }

    // Hard-edged pixel-art quantization (Alex's fix, ported verbatim from the reference video):
    // below threshold is flat clear sky (0), above threshold is split into `stepCount + 1`
    // equal, hard-edged bands via Ceil rather than smoothstepped/dithered. No ordered-dither
    // pattern is introduced, so it doesn't fight with the sprite art's own pixel grid the way
    // Bayer dithering did.
    private static float ApplySteppedBanding(float noise, float threshold, int stepCount)
    {
        if (noise < threshold)
            return 0f;
        if (stepCount <= 0)
            return 1f;

        float normalizedAboveThreshold = Mathf.InverseLerp(threshold, 1f, noise);
        float bandCount = stepCount + 1f;
        return Mathf.Ceil(normalizedAboveThreshold * bandCount) / bandCount;
    }
}
#endif
