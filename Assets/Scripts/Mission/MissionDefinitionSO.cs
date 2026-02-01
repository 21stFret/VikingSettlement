using UnityEngine;

/// <summary>
/// ScriptableObject defining a mission template.
/// Runtime progress is tracked separately via ActiveMission.
/// </summary>
[CreateAssetMenu(fileName = "New Mission", menuName = "Viking Settlement/Mission")]
public class MissionDefinitionSO : ScriptableObject
{
    [Tooltip("Unique ID used by the save system to reference this mission")]
    public string missionId;

    public string title;

    [TextArea(2, 4)]
    public string description;

    [Header("Objectives")]
    public MissionObjectiveTemplate[] objectives;

    [Header("Rewards")]
    public MissionReward[] rewards;

    [Header("Dialogue")]
    public DialogueSO startDialogue;
    public DialogueSO completionDialogue;
}
