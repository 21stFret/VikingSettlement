using UnityEngine;

public enum MissionObjectiveType
{
    GatherResource,
    CompleteRaid,
    SurviveTime
}

public enum MissionRewardType
{
    Resource,
    Experience,
    Weapon,
    Shield
}

public enum MissionStatus
{
    Available,
    Active,
    Completed
}

/// <summary>
/// Template for a mission objective — defines type and target but holds no runtime progress.
/// Runtime progress is tracked via ActiveMission.objectiveProgress[].
/// </summary>
[System.Serializable]
public class MissionObjectiveTemplate
{
    public MissionObjectiveType type;

    [Header("GatherResource Settings")]
    public ResourceType resourceType;

    [Header("Target")]
    [Tooltip("Amount to gather, or days to survive")]
    public float targetAmount;
}

/// <summary>
/// A reward granted on mission completion
/// </summary>
[System.Serializable]
public class MissionReward
{
    public MissionRewardType rewardType;

    [Header("Resource Reward")]
    public ResourceType resourceType;
    public float amount;

    [Header("Item Reward")]
    public EquipableItem itemReward;

    /// <summary>
    /// Get a display string for this reward
    /// </summary>
    public string GetDescription()
    {
        switch (rewardType)
        {
            case MissionRewardType.Resource:
                return $"{Mathf.FloorToInt(amount)} {resourceType}";
            case MissionRewardType.Experience:
                return $"{Mathf.FloorToInt(amount)} XP";
            case MissionRewardType.Weapon:
                return itemReward != null ? itemReward.name : "Weapon";
            case MissionRewardType.Shield:
                return itemReward != null ? itemReward.name : "Shield";
            default:
                return "Unknown reward";
        }
    }
}
