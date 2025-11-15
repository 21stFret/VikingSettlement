using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
public class PrefabPainter : EditorWindow
{
    private PrefabPainterTemplate currentTemplate;
    private string newTemplateName = "New Template";
    private List<GameObject> prefabs = new List<GameObject>();
    private Vector2 scrollPosition;
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private float minRotation = 0f;
    private float maxRotation = 360f;
    private LayerMask paintLayer = 0; // All layers by default
    private bool randomizeRotationY = true;
    private bool alignToNormal = true;
    private float surfaceOffset = 0f;
    private float minSpacing = 1f;
    private bool checkOverlap = true;
    private LayerMask overlapCheckLayers = 0;

    private void SaveTemplate()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Template",
            newTemplateName,
            "asset",
            "Save prefab painter template"
        );

        if (string.IsNullOrEmpty(path)) return;

        PrefabPainterTemplate template = ScriptableObject.CreateInstance<PrefabPainterTemplate>();

        // Copy current settings to template
        template.prefabs = new List<GameObject>(prefabs);
        template.minScale = minScale;
        template.maxScale = maxScale;
        template.minRotation = minRotation;
        template.maxRotation = maxRotation;
        template.paintLayer = paintLayer;
        template.randomizeRotationY = randomizeRotationY;
        template.alignToNormal = alignToNormal;
        template.surfaceOffset = surfaceOffset;
        template.minSpacing = minSpacing;
        template.checkOverlap = checkOverlap;
        template.overlapCheckLayers = overlapCheckLayers;

        AssetDatabase.CreateAsset(template, path);
        AssetDatabase.SaveAssets();
    }

    private void LoadTemplate(PrefabPainterTemplate template)
    {
        if (template == null) return;

        prefabs = new List<GameObject>(template.prefabs);
        minScale = template.minScale;
        maxScale = template.maxScale;
        minRotation = template.minRotation;
        maxRotation = template.maxRotation;
        paintLayer = template.paintLayer;
        randomizeRotationY = template.randomizeRotationY;
        alignToNormal = template.alignToNormal;
        surfaceOffset = template.surfaceOffset;
        minSpacing = template.minSpacing;
        checkOverlap = template.checkOverlap;
        overlapCheckLayers = template.overlapCheckLayers;
    }

    [MenuItem("Tools/Prefab Painter")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPainter>("Prefab Painter");
    }

    private void OnEnable()
    {
        // Make sure we get SceneView updates
        SceneView.duringSceneGui += OnSceneGUI;

        // Initialize if needed
        if (prefabs == null)
            prefabs = new List<GameObject>();

        // Set window title
        titleContent = new GUIContent("Prefab Painter");
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Painter Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Template Management", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        currentTemplate = (PrefabPainterTemplate)EditorGUILayout.ObjectField(
            "Current Template",
            currentTemplate,
            typeof(PrefabPainterTemplate),
            false
        );

        if (currentTemplate != null && GUILayout.Button("Load", GUILayout.Width(60)))
        {
            LoadTemplate(currentTemplate);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        newTemplateName = EditorGUILayout.TextField("New Template Name", newTemplateName);
        if (GUILayout.Button("Save", GUILayout.Width(60)))
        {
            SaveTemplate();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Prefab list
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);

            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                prefabs.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add Prefab"))
        {
            prefabs.Add(null);
        }

        EditorGUILayout.Space();

        // Scale settings
        EditorGUILayout.LabelField("Scale Range", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);
        EditorGUILayout.EndHorizontal();

        // Rotation settings
        EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
        randomizeRotationY = EditorGUILayout.Toggle("Randomize Y Rotation", randomizeRotationY);
        if (randomizeRotationY)
        {
            EditorGUILayout.BeginHorizontal();
            minRotation = EditorGUILayout.FloatField("Min Rotation", minRotation);
            maxRotation = EditorGUILayout.FloatField("Max Rotation", maxRotation);
            EditorGUILayout.EndHorizontal();
        }

        // Surface alignment settings
        EditorGUILayout.LabelField("Surface Settings", EditorStyles.boldLabel);
        alignToNormal = EditorGUILayout.Toggle("Align to Surface Normal", alignToNormal);
        surfaceOffset = EditorGUILayout.FloatField("Surface Offset", surfaceOffset);
        string[] layerNames = new string[32];
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName))
                layerName = "Layer " + i;
            layerNames[i] = layerName;
        }
        paintLayer = Mathf.Clamp(paintLayer, 0, 31);
        paintLayer = EditorGUILayout.MaskField("Paint Layers", paintLayer, layerNames);

        // Overlap settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Overlap Settings", EditorStyles.boldLabel);
        checkOverlap = EditorGUILayout.Toggle("Check Overlap", checkOverlap);
        if (checkOverlap)
        {
            minSpacing = EditorGUILayout.FloatField("Minimum Spacing", minSpacing);
            string[] overlapLayerNames = new string[32];
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                    layerName = "Layer " + i;
                overlapLayerNames[i] = layerName;
            }
            overlapCheckLayers = Mathf.Clamp(overlapCheckLayers, 0, 31);
            overlapCheckLayers = EditorGUILayout.MaskField("Overlap Layers", overlapCheckLayers, overlapLayerNames);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Only proceed if the window is open
        if (!Selection.activeGameObject) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event e = Event.current;

        // Draw a handle to show where we're painting
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, paintLayer))
        {
            Handles.color = Color.green;
            Handles.DrawWireDisc(hit.point, hit.normal, minSpacing);

            // Check for left click (without requiring Control)
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                PlacePrefab(hit);
                e.Use();
                // Force the scene view to repaint
                sceneView.Repaint();
            }
        }

        // Repaint the scene view while the tool is active
        if (e.type == EventType.Layout)
            HandleUtility.Repaint();
    }

    private bool CheckOverlap(Vector3 position, float objectRadius, RaycastHit hit)
    {
        // Get all colliders within the minimum spacing range
        Collider[] overlaps = Physics.OverlapSphere(position, minSpacing, overlapCheckLayers);

        // Check if any of the overlapping objects are prefab instances
        foreach (Collider overlap in overlaps)
        {
            // Skip the surface we're painting on
            if (overlap.gameObject == hit.collider.gameObject)
                continue;

            // If we found any other objects within range, there's an overlap
            return true;
        }

        return false;
    }

    private float EstimatePrefabRadius(GameObject prefab)
    {
        // Try to get the bounds from various components
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.extents.magnitude;
        }

        Collider collider = prefab.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds.extents.magnitude;
        }

        // Default radius if no bounds can be determined
        return 0.5f;
    }

    private void PlacePrefab(RaycastHit hit)
    {
        if (prefabs.Count == 0 || prefabs.TrueForAll(p => p == null))
        {
            Debug.LogWarning("No prefabs assigned to the Prefab Painter!");
            return;
        }

        // Select random prefab
        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        while (prefab == null && prefabs.Exists(p => p != null))
        {
            prefab = prefabs[Random.Range(0, prefabs.Count)];
        }

        if (prefab == null) return;

        // Create the prefab instance
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");

        // Position
        Vector3 position = hit.point + hit.normal * surfaceOffset;

        // Check for overlap if enabled
        if (checkOverlap)
        {
            float prefabRadius = EstimatePrefabRadius(prefab);
            if (CheckOverlap(position, prefabRadius, hit))
            {
                Debug.LogWarning("Cannot place prefab: Too close to other objects!");
                return;
            }
        }
        instance.transform.position = position;

        // Rotation
        if (alignToNormal)
        {
            instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            if (randomizeRotationY)
            {
                float randomY = Random.Range(minRotation, maxRotation);
                instance.transform.Rotate(Vector3.up, randomY, Space.Self);
            }
        }
        else if (randomizeRotationY)
        {
            float randomY = Random.Range(minRotation, maxRotation);
            instance.transform.rotation = Quaternion.Euler(0, randomY, 0);
        }

        // Scale
        float randomScale = Random.Range(minScale, maxScale);
        instance.transform.localScale *= randomScale;
    }
}
#endif