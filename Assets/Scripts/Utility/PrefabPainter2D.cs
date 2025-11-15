using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class PrefabPainter2D : EditorWindow
{
    private PrefabPainterTemplate currentTemplate;
    private string newTemplateName = "New Template";
    private List<GameObject> prefabs = new List<GameObject>();
    private Vector2 scrollPosition;
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private LayerMask paintLayer = -1; // All layers by default
    private float surfaceOffset = 0f;
    private float minSpacing = 1f;
    private bool checkOverlap = true;
    private LayerMask overlapCheckLayers = -1;
    private int sortingLayerID = 0;
    private int orderInLayer = 0;
    
    // Brush settings
    private float brushSize = 1f;
    private float paintDensity = 0.5f; // Objects per unit when dragging
    private bool isDragging = false;
    private Vector2 lastPaintPosition;
    private float paintAccumulator = 0f;

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
        template.minRotation = 0f; // Not used in 2D
        template.maxRotation = 0f; // Not used in 2D
        template.paintLayer = paintLayer;
        template.randomizeRotationY = false; // Not used in 2D
        template.alignToNormal = false; // Not used in 2D
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
        paintLayer = template.paintLayer;
        surfaceOffset = template.surfaceOffset;
        minSpacing = template.minSpacing;
        checkOverlap = template.checkOverlap;
        overlapCheckLayers = template.overlapCheckLayers;
    }

    [MenuItem("Tools/Prefab Painter 2D")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPainter2D>("Prefab Painter 2D");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        if (prefabs == null)
            prefabs = new List<GameObject>();

        titleContent = new GUIContent("Prefab Painter 2D");
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
        EditorGUILayout.LabelField("Prefab Painter 2D Settings", EditorStyles.boldLabel);
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

        // Brush settings
        EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);
        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 10f);
        paintDensity = EditorGUILayout.Slider("Paint Density", paintDensity, 0.1f, 5f);
        EditorGUILayout.HelpBox("Density controls how many objects spawn when dragging. Higher = more objects.", MessageType.Info);

        EditorGUILayout.Space();

        // Scale settings
        EditorGUILayout.LabelField("Scale Range", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);
        EditorGUILayout.EndHorizontal();

        // Surface settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Surface Settings", EditorStyles.boldLabel);
        surfaceOffset = EditorGUILayout.FloatField("Surface Offset", surfaceOffset);
        
        string[] layerNames = new string[32];
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName))
                layerName = "Layer " + i;
            layerNames[i] = layerName;
        }
        paintLayer = EditorGUILayout.MaskField("Paint Layers", paintLayer, layerNames);

        // Sorting Layer settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sprite Sorting", EditorStyles.boldLabel);
        
        // Get all sorting layers
        string[] sortingLayerNames = GetSortingLayerNames();
        int currentSortingLayerIndex = GetSortingLayerIndex(sortingLayerID);
        int newSortingLayerIndex = EditorGUILayout.Popup("Sorting Layer", currentSortingLayerIndex, sortingLayerNames);
        if (newSortingLayerIndex != currentSortingLayerIndex)
        {
            sortingLayerID = GetSortingLayerIDFromIndex(newSortingLayerIndex);
        }
        
        orderInLayer = EditorGUILayout.IntField("Order in Layer", orderInLayer);

        // Overlap settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Overlap Settings", EditorStyles.boldLabel);
        checkOverlap = EditorGUILayout.Toggle("Check Overlap", checkOverlap);
        if (checkOverlap)
        {
            minSpacing = EditorGUILayout.FloatField("Minimum Spacing", minSpacing);
            overlapCheckLayers = EditorGUILayout.MaskField("Overlap Layers", overlapCheckLayers, layerNames);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click to place single prefabs. Click and drag to paint multiple prefabs.", MessageType.Info);
    }

    private string[] GetSortingLayerNames()
    {
        System.Type internalEditorUtilityType = typeof(UnityEditorInternal.InternalEditorUtility);
        var sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        return (string[])sortingLayersProperty.GetValue(null, new object[0]);
    }

    private int GetSortingLayerIndex(int layerID)
    {
        System.Type internalEditorUtilityType = typeof(UnityEditorInternal.InternalEditorUtility);
        var sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        int[] sortingLayerIDs = (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        
        for (int i = 0; i < sortingLayerIDs.Length; i++)
        {
            if (sortingLayerIDs[i] == layerID)
                return i;
        }
        return 0;
    }

    private int GetSortingLayerIDFromIndex(int index)
    {
        System.Type internalEditorUtilityType = typeof(UnityEditorInternal.InternalEditorUtility);
        var sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        int[] sortingLayerIDs = (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        
        if (index >= 0 && index < sortingLayerIDs.Length)
            return sortingLayerIDs[index];
        return 0;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event e = Event.current;

        // Get mouse position in world space for 2D
        Vector3 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        mousePos.z = 0; // Ensure we're working in 2D space

        // Draw brush preview circle
        Handles.color = isDragging ? Color.green : new Color(0f, 1f, 0f, 0.5f);
        Handles.DrawWireDisc(mousePos, Vector3.forward, brushSize);
        
        // Draw inner spacing circle
        if (checkOverlap)
        {
            Handles.color = new Color(1f, 0f, 0f, 0.3f);
            Handles.DrawWireDisc(mousePos, Vector3.forward, minSpacing);
        }

        // Handle mouse down
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isDragging = true;
            lastPaintPosition = mousePos;
            paintAccumulator = 0f;
            
            // Place initial prefab
            PlacePrefabAtPosition(mousePos);
            e.Use();
            sceneView.Repaint();
        }
        
        // Handle mouse drag
        else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
        {
            Vector2 currentPos = mousePos;
            float distance = Vector2.Distance(lastPaintPosition, currentPos);
            
            // Add distance to accumulator
            paintAccumulator += distance * paintDensity;
            
            // Place prefabs based on accumulated distance
            while (paintAccumulator >= 1f)
            {
                // Calculate position along the path
                float t = Mathf.Min(1f / paintDensity / distance, 1f);
                Vector2 paintPos = Vector2.Lerp(lastPaintPosition, currentPos, t);
                
                // Add some randomness within brush size
                Vector2 randomOffset = Random.insideUnitCircle * brushSize;
                paintPos += randomOffset;
                
                PlacePrefabAtPosition(paintPos);
                
                paintAccumulator -= 1f;
            }
            
            lastPaintPosition = currentPos;
            e.Use();
            sceneView.Repaint();
        }
        
        // Handle mouse up
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false;
            paintAccumulator = 0f;
            e.Use();
            sceneView.Repaint();
        }

        // Repaint the scene view while the tool is active
        if (e.type == EventType.Layout)
            HandleUtility.Repaint();
    }

    private bool CheckOverlap(Vector2 position, float objectRadius)
    {
        // Get all colliders within the minimum spacing range
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, minSpacing, overlapCheckLayers);

        // If we found any objects within range, there's an overlap
        return overlaps.Length > 0;
    }

    private float EstimatePrefabRadius(GameObject prefab)
    {
        // Try to get the bounds from various 2D components
        SpriteRenderer spriteRenderer = prefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.bounds.extents.magnitude;
        }

        Collider2D collider = prefab.GetComponent<Collider2D>();
        if (collider != null)
        {
            return collider.bounds.extents.magnitude;
        }

        // Default radius if no bounds can be determined
        return 0.5f;
    }

    private void PlacePrefabAtPosition(Vector2 position2D)
    {
        if (prefabs.Count == 0 || prefabs.TrueForAll(p => p == null))
        {
            return;
        }

        // Select random prefab
        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        while (prefab == null && prefabs.Exists(p => p != null))
        {
            prefab = prefabs[Random.Range(0, prefabs.Count)];
        }

        if (prefab == null) return;

        // Check for overlap if enabled
        if (checkOverlap)
        {
            float prefabRadius = EstimatePrefabRadius(prefab);
            if (CheckOverlap(position2D, prefabRadius))
            {
                return; // Silently skip if overlapping
            }
        }

        // Create the prefab instance
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");

        // Position (convert to Vector3 with z=0)
        Vector3 position = new Vector3(position2D.x, position2D.y, 0);
        instance.transform.position = position;

        // Scale
        float randomScale = Random.Range(minScale, maxScale);
        instance.transform.localScale = Vector3.one * randomScale;

        // Set sorting layer and order if the instance has a SpriteRenderer
        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerID = sortingLayerID;
            spriteRenderer.sortingOrder = orderInLayer;
        }

        // Also check for SpriteRenderer in children
        SpriteRenderer[] childRenderers = instance.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in childRenderers)
        {
            renderer.sortingLayerID = sortingLayerID;
            renderer.sortingOrder = orderInLayer;
        }
    }
}
#endif