using UnityEngine;

/// <summary>
/// ScriptableObject defining a single discoverable entry in the compendium.
/// </summary>
[CreateAssetMenu(fileName = "New Compendium Entry", menuName = "Viking Settlement/Compendium Entry")]
public class CompendiumEntrySO : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public CompendiumCategory category;
    public string displayName;

    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"CompendiumEntrySO '{name}' has an empty id.");
        }
    }
}
