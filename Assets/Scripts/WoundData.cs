using System.Collections.Generic;

public enum WoundType
{
    Lame,           // -8% movement speed
    OneEye,         // -5% max HP, -5% attack damage
    BattleScarred,  // -5% max HP, -5% defense
    BrokenRibs,     // -8% attack damage
    TornShoulder,   // -5% attack speed, -5% attack damage
    IronWill        // -5% max HP (morale resistance deferred)
}

[System.Serializable]
public struct WoundInfo
{
    public WoundType type;
    public string displayName;
    public string description;
    public float moveSpeedPenaltyPct;
    public float maxHPPenaltyPct;
    public float attackDamagePenaltyPct;
    public float defensePenaltyPct;
    public float attackSpeedPenaltyPct;
}

public static class WoundDatabase
{
    public static readonly WoundInfo[] All = new WoundInfo[]
    {
        new WoundInfo { type = WoundType.Lame,          displayName = "Lame",          description = "Leg injury. Slower but still functional.",             moveSpeedPenaltyPct = 8f },
        new WoundInfo { type = WoundType.OneEye,        displayName = "One Eye",        description = "Depth perception lost. Weaker all-round.",             maxHPPenaltyPct = 5f, attackDamagePenaltyPct = 5f },
        new WoundInfo { type = WoundType.BattleScarred, displayName = "Battle-Scarred", description = "Accumulated damage. More fragile.",                    maxHPPenaltyPct = 5f, defensePenaltyPct = 5f },
        new WoundInfo { type = WoundType.BrokenRibs,   displayName = "Broken Ribs",    description = "Torso injury. Swings are weaker.",                     attackDamagePenaltyPct = 8f },
        new WoundInfo { type = WoundType.TornShoulder, displayName = "Torn Shoulder",  description = "Arm injury. Slower, weaker strikes.",                  attackSpeedPenaltyPct = 5f, attackDamagePenaltyPct = 5f },
        new WoundInfo { type = WoundType.IronWill,     displayName = "Iron Will",      description = "Survived trauma. Tougher mentally, weaker physically.", maxHPPenaltyPct = 5f }
    };

    public static WoundInfo Get(WoundType type) => All[(int)type];

    public static float TotalMoveSpeedPenaltyPct(List<WoundType> wounds)
    {
        float t = 0f;
        foreach (var w in wounds) t += All[(int)w].moveSpeedPenaltyPct;
        return t;
    }

    public static float TotalMaxHPPenaltyPct(List<WoundType> wounds)
    {
        float t = 0f;
        foreach (var w in wounds) t += All[(int)w].maxHPPenaltyPct;
        return t;
    }

    public static float TotalAttackDamagePenaltyPct(List<WoundType> wounds)
    {
        float t = 0f;
        foreach (var w in wounds) t += All[(int)w].attackDamagePenaltyPct;
        return t;
    }

    public static float TotalDefensePenaltyPct(List<WoundType> wounds)
    {
        float t = 0f;
        foreach (var w in wounds) t += All[(int)w].defensePenaltyPct;
        return t;
    }

    public static float TotalAttackSpeedPenaltyPct(List<WoundType> wounds)
    {
        float t = 0f;
        foreach (var w in wounds) t += All[(int)w].attackSpeedPenaltyPct;
        return t;
    }
}
