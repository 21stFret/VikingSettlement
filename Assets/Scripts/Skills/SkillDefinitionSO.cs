using UnityEngine;

/// <summary>
/// ScriptableObject defining a single skill in the skill tree.
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "Viking Settlement/Skill")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string skillId;
    public string skillName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Classification")]
    public SkillType skillType;
    public SkillTier tier;

    [Header("Cost")]
    public int xpCost = 100;

    [Header("Prerequisites")]
    [Tooltip("Skills that must be unlocked before this one")]
    public SkillDefinitionSO[] prerequisites;

    [Header("Effects")]
    public SkillEffect[] effects;

    [Header("Tree Layout")]
    [Tooltip("Position in the skill tree UI (for visual layout)")]
    public Vector2 treePosition;
}

public enum SkillType
{
    Combat,
    Passive,
    ResourceGathering
}

public enum SkillTier
{
    Tier1,  // Starting skills, no prerequisites
    Tier2,
    Tier3,
    Tier4   // Ultimate skills
}

/// <summary>
/// A single effect granted by a skill.
/// </summary>
[System.Serializable]
public class SkillEffect
{
    public SkillEffectType effectType;
    public float value;

    [Header("Conditional (Optional)")]
    [Tooltip("Only applies to this job type (for resource gathering bonuses)")]
    public JobType specificJob;
    [Tooltip("Only applies to this resource type")]
    public ResourceType specificResource;
}

public enum SkillEffectType
{
    // Combat
    DamagePercent,          // +X% damage
    AttackSpeedPercent,     // +X% attack speed (reduces delay)
    DefenseFlat,            // +X defense
    DefensePercent,         // +X% defense
    MaxHealthFlat,          // +X max health
    MaxHealthPercent,       // +X% max health
    LifeSteal,              // X% of damage heals you
    CriticalChance,         // X% chance for 2x damage

    // Passive
    MoveSpeedPercent,       // +X% movement speed
    XPGainPercent,          // +X% XP gained
    MoraleDecayReduction,   // -X% morale decay rate

    // Resource Gathering
    GatheringSpeedPercent,  // +X% gathering speed (for specificJob or all)
    GatheringYieldPercent,  // +X% yield when gathering
    ResourceCapacityPercent // +X% max storage for specificResource
}
