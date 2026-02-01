using UnityEngine;

/// <summary>
/// ScriptableObject version of Dialogue — a reusable, asset-based dialogue sequence.
/// </summary>
[CreateAssetMenu(fileName = "New Dialogue", menuName = "Viking Settlement/Dialogue")]
public class DialogueSO : ScriptableObject
{
    [Tooltip("Unique ID used by the save system to reference this dialogue")]
    public string dialogueId;

    public DialogueLine[] lines;

    [Tooltip("If true, the dialogue will offer a quest accept/decline prompt at the end")]
    public bool offersQuest;
}
