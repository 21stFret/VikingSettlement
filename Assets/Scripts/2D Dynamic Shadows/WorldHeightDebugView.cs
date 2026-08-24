using UnityEngine;

/// <summary>
/// Drop this on any GameObject (the WorldHeightCamera itself is a fine place) to see the live
/// baked height map on screen in Play mode. This is currently the ONLY way to see it —
/// WorldHeightCamera.HeightTexture is a runtime RenderTexture, not a project asset, so it never
/// appears in the Assets folder or Project window; and because it's a C# property rather than a
/// serialized field, it doesn't show in the Inspector either.
///
/// White/bright = tall (near WorldHeightContributor.height == 1). Black = nothing contributed
/// there. If this stays solid black no matter what you tag, work through, in order: (1) does the
/// "WorldHeight" Layer actually exist (Project Settings > Tags and Layers)? (2) is a
/// WorldHeightCamera present in the scene, and does its World Origin/World Size (cyan gizmo)
/// actually cover where your tagged buildings are? (3) do those buildings have a
/// WorldHeightContributor with height > 0? Console warnings from WorldHeightCamera/
/// WorldHeightContributor will tell you if the layer is the problem.
/// </summary>
public class WorldHeightDebugView : MonoBehaviour
{
    [Tooltip("Size, in screen pixels, of the on-screen preview square.")]
    public float previewSize = 256f;
    public Vector2 screenOffset = new Vector2(10f, 10f);
    public KeyCode toggleKey = KeyCode.F9;
    public bool visible = true;

    void Update()
    {
    }

    void OnGUI()
    {
        if (!visible)
            return;

        var cam = WorldHeightCamera.Instance;
        var rect = new Rect(screenOffset.x, screenOffset.y, previewSize, previewSize + 20);

        if (cam == null)
        {
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 40),
                "No WorldHeightCamera in the scene.\nAdd one — see CloudShadowOverlay setup notes.");
            return;
        }

        if (cam.HeightTexture == null)
        {
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 20), "WorldHeightCamera: texture not built yet.");
            return;
        }

        GUI.Box(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), GUIContent.none);
        string status = cam.HasBakedAtLeastOnce ? "baked" : "NEVER BAKED — check the WorldHeight layer + contributors";
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 20), $"World Height Map ({status}) [{toggleKey} to hide]");
        GUI.DrawTexture(new Rect(rect.x, rect.y + 20, previewSize, previewSize), cam.HeightTexture, ScaleMode.ScaleToFit, false);
    }
}
