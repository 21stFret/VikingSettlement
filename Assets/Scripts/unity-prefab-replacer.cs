using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class PrefabReplacer : EditorWindow
{
    private GameObject prefab;

    [MenuItem("Tools/Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacer>("Prefab Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Selected Objects with Prefab", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        if (GUILayout.Button("Replace Selected"))
        {
            ReplaceSelected();
        }
    }

    private void ReplaceSelected()
    {
        if (prefab == null)
        {
            Debug.LogError("Please assign a prefab before replacing.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected. Please select one or more objects to replace.");
            return;
        }

        Undo.RecordObjects(selectedObjects, "Replace with Prefab");

        foreach (GameObject obj in selectedObjects)
        {
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            newObject.transform.SetParent(obj.transform.parent);
            newObject.transform.position = obj.transform.position;
            newObject.transform.rotation = obj.transform.rotation;
            newObject.transform.localScale = obj.transform.localScale;

            Undo.RegisterCreatedObjectUndo(newObject, "Create Prefab Instance");
            Undo.DestroyObjectImmediate(obj);
        }
    }
}
#endif
