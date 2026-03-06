using UnityEngine;

/// <summary>
/// Runtime wrapper that pairs a MissionDefinitionSO with live progress data.
/// </summary>
[System.Serializable]
public class ActiveMission
{
    public MissionDefinitionSO definition;

    /// <summary>
    /// Parallel array to definition.objectives — tracks current progress for each objective.
    /// </summary>
    public float[] objectiveProgress;

    public MissionStatus status;

    public ActiveMission(MissionDefinitionSO definition)
    {
        this.definition = definition;
        this.status = MissionStatus.Active;

        if (definition.objectives != null)
        {
            objectiveProgress = new float[definition.objectives.Length];
        }
        else
        {
            objectiveProgress = new float[0];
        }
    }

    /// <summary>
    /// Returns true when every objective has met its target.
    /// </summary>
    public bool AreAllObjectivesComplete()
    {
        if (definition.objectives == null) return true;

        for (int i = 0; i < definition.objectives.Length; i++)
        {
            if (objectiveProgress[i] < definition.objectives[i].targetAmount)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check whether a specific objective index is complete.
    /// </summary>
    public bool IsObjectiveComplete(int index)
    {
        if (definition.objectives == null || index < 0 || index >= definition.objectives.Length)
            return false;
        return objectiveProgress[index] >= definition.objectives[index].targetAmount;
    }

    /// <summary>
    /// Get a display string for a specific objective.
    /// </summary>
    public string GetObjectiveDescription(int index)
    {
        if (definition.objectives == null || index < 0 || index >= definition.objectives.Length)
            return "Unknown objective";

        MissionObjectiveTemplate template = definition.objectives[index];
        float current = objectiveProgress[index];

        switch (template.type)
        {
            case MissionObjectiveType.GatherResource:
                return $"{template.resourceType}: {Mathf.FloorToInt(current)}/{Mathf.FloorToInt(template.targetAmount)}";
            case MissionObjectiveType.CompleteRaid:
                string raidStatus = current >= template.targetAmount ? "Done" : "Incomplete";
                return $"Complete a raid: {raidStatus}";
            case MissionObjectiveType.SurviveTime:
                int currentDays = Mathf.FloorToInt(current);
                int targetDays = Mathf.FloorToInt(template.targetAmount);
                return $"Survive: {currentDays}/{targetDays} days";
            case MissionObjectiveType.RepairBuilding:
                return $"Repair {template.targetBuildingType}: {(current >= template.targetAmount ? "Done" : "Incomplete")}";
            case MissionObjectiveType.AssignVillager:
                return $"Assign villager to {template.targetBuildingType}: {(current >= template.targetAmount ? "Done" : "Incomplete")}";
            default:
                return "Unknown objective";
        }
    }
}
