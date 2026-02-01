using System;

/// <summary>
/// Priority levels for succession candidates.
/// Lower values = higher priority.
/// </summary>
public enum SuccessionPriority
{
    DirectChild = 0,
    Sibling = 1,
    Grandchild = 2,
    DistantRelative = 3,
    UnrelatedHighSkill = 4
}

/// <summary>
/// Data class representing a candidate for Jarl succession.
/// Used for ranking and displaying heir options.
/// </summary>
[Serializable]
public class SuccessionCandidate
{
    public Villager Villager;
    public SuccessionPriority Priority;
    public int RelationshipDistance; // 0 = direct child, 1 = grandchild, etc.
    public float SuccessionScore;

    // Score breakdown for UI display
    public float CombatSkillBonus;
    public float AgeBonus;
    public float LineageBonus;

    /// <summary>
    /// Get a human-readable relationship description
    /// </summary>
    public string GetRelationshipText()
    {
        switch (Priority)
        {
            case SuccessionPriority.DirectChild:
                return "Child";
            case SuccessionPriority.Sibling:
                return "Sibling";
            case SuccessionPriority.Grandchild:
                return "Grandchild";
            case SuccessionPriority.DistantRelative:
                return "Distant Relative";
            case SuccessionPriority.UnrelatedHighSkill:
                return "Unrelated";
            default:
                return "Unknown";
        }
    }
}
