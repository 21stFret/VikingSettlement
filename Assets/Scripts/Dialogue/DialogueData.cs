using UnityEngine;

/// <summary>
/// A single line of dialogue with speaker info
/// </summary>
[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite portrait;
    [TextArea(2, 5)]
    public string text;
}
