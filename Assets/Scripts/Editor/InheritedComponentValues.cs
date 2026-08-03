using UnityEditor;
using UnityEngine;

// Unity's built-in "Paste Component Values" refuses to paste between a base
// class and a subclass because it requires an exact type match. These two
// context-menu items copy/paste by serialized property path instead, so
// values shared with a base class carry over even when the concrete types differ.
public static class InheritedComponentValues
{
    private static Component copiedComponent;

    [MenuItem("CONTEXT/Component/Copy Values (Inherited)", false, 1000)]
    private static void CopyValues(MenuCommand command)
    {
        copiedComponent = command.context as Component;
    }

    [MenuItem("CONTEXT/Component/Paste Values (Inherited)", true, 1001)]
    private static bool ValidatePasteValues(MenuCommand command)
    {
        return copiedComponent != null && command.context is Component;
    }

    [MenuItem("CONTEXT/Component/Paste Values (Inherited)", false, 1001)]
    private static void PasteValues(MenuCommand command)
    {
        var target = command.context as Component;
        if (target == null || copiedComponent == null)
            return;

        var sourceSO = new SerializedObject(copiedComponent);
        var targetSO = new SerializedObject(target);

        Undo.RecordObject(target, "Paste Values (Inherited)");

        int copied = 0;
        var sourceProp = sourceSO.GetIterator();
        bool enterChildren = true;
        while (sourceProp.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (sourceProp.propertyPath == "m_Script")
                continue;

            var targetProp = targetSO.FindProperty(sourceProp.propertyPath);
            if (targetProp == null || targetProp.propertyType != sourceProp.propertyType)
                continue;

            try
            {
                CopyProperty(sourceProp, targetProp);
                copied++;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Skipped '{sourceProp.propertyPath}': {e.Message}");
            }
        }

        targetSO.ApplyModifiedProperties();
        Debug.Log($"Pasted {copied} matching field(s) from {copiedComponent.GetType().Name} into {target.GetType().Name}.");
    }

    // Arrays/lists report propertyType == Generic on the container itself and
    // must be resized and copied element-by-element; boxedValue throws if used
    // directly on them. Everything else (including non-array Generic structs
    // and SerializeReference objects) can go through boxedValue directly.
    private static void CopyProperty(SerializedProperty source, SerializedProperty target)
    {
        if (source.isArray && source.propertyType == SerializedPropertyType.Generic)
        {
            target.arraySize = source.arraySize;
            for (int i = 0; i < source.arraySize; i++)
                CopyProperty(source.GetArrayElementAtIndex(i), target.GetArrayElementAtIndex(i));
            return;
        }

        target.boxedValue = source.boxedValue;
    }
}
