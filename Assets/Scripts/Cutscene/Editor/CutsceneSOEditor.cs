using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Cutscenes;

[CustomEditor(typeof(CutsceneSO))]
public class CutsceneSOEditor : Editor
{
    private SerializedProperty actionsProperty;
    private string[] actionTypeNames;
    private Type[] actionTypes;
    private int selectedActionType = 0;

    private void OnEnable()
    {
        actionsProperty = serializedObject.FindProperty("actions");

        // Find all CutsceneAction subclasses
        actionTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(CutsceneAction)) && !type.IsAbstract)
            .ToArray();

        actionTypeNames = actionTypes.Select(t => t.Name.Replace("Action", "")).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default fields except actions
        DrawPropertiesExcluding(serializedObject, "actions");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // Draw actions list
        for (int i = 0; i < actionsProperty.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();

            var element = actionsProperty.GetArrayElementAtIndex(i);
            var action = element.managedReferenceValue as CutsceneAction;

            string label = action != null
                ? $"{i + 1}. {action.GetType().Name.Replace("Action", "")}"
                : $"{i + 1}. (null)";

            if (!string.IsNullOrEmpty(action?.actionName))
            {
                label += $" - {action.actionName}";
            }

            EditorGUILayout.PropertyField(element, new GUIContent(label), true);

            // Move up button
            GUI.enabled = i > 0;
            if (GUILayout.Button("↑", GUILayout.Width(25)))
            {
                actionsProperty.MoveArrayElement(i, i - 1);
            }

            // Move down button
            GUI.enabled = i < actionsProperty.arraySize - 1;
            if (GUILayout.Button("↓", GUILayout.Width(25)))
            {
                actionsProperty.MoveArrayElement(i, i + 1);
            }

            GUI.enabled = true;

            // Delete button
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                actionsProperty.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);

        // Add action dropdown and button
        EditorGUILayout.BeginHorizontal();
        selectedActionType = EditorGUILayout.Popup("Add Action", selectedActionType, actionTypeNames);

        if (GUILayout.Button("Add", GUILayout.Width(50)))
        {
            AddAction(actionTypes[selectedActionType]);
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void AddAction(Type actionType)
    {
        var newAction = Activator.CreateInstance(actionType) as CutsceneAction;
        actionsProperty.arraySize++;
        var newElement = actionsProperty.GetArrayElementAtIndex(actionsProperty.arraySize - 1);
        newElement.managedReferenceValue = newAction;
    }
}
