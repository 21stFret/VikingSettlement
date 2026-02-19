using System.Collections.Generic;
using System.Linq;

public enum DeathCause
{
    Combat,
    OldAge,
    Starvation,
    Cold,
    Other
}

public enum RunestoneType
{
    Rationing,
    TirelessWorkers,
    Education,
    WintersFriend,
    AncestorsFury,
    IronDiscipline,
    WeaponMaintenance,
    ResilientBlood,
    SwiftRecovery,
    FertileLands,
    RunekeepersGift,
    RaidingShips
}

public enum RunestoneCategory
{
    Economy,
    Combat,
    Survival,
    Utility
}

public struct RunestoneInfo
{
    public RunestoneType type;
    public string name;
    public string description;
    public RunestoneCategory category;

    public RunestoneInfo(RunestoneType type, string name, string description, RunestoneCategory category)
    {
        this.type = type;
        this.name = name;
        this.description = description;
        this.category = category;
    }
}

public static class RunestoneDatabase
{
    private static readonly Dictionary<RunestoneType, RunestoneInfo> runestones = new Dictionary<RunestoneType, RunestoneInfo>
    {
        { RunestoneType.Rationing, new RunestoneInfo(RunestoneType.Rationing, "Ar", "Villagers consume 1 less food per day", RunestoneCategory.Economy) },
        { RunestoneType.TirelessWorkers, new RunestoneInfo(RunestoneType.TirelessWorkers, "Madr", "All production buildings +15% output speed", RunestoneCategory.Economy) },
        { RunestoneType.Education, new RunestoneInfo(RunestoneType.Education, "Oss", "New villagers spawn with +2 to 2 random skills", RunestoneCategory.Economy) },
        { RunestoneType.WintersFriend, new RunestoneInfo(RunestoneType.WintersFriend, "Iss", "Firewood consumption -30%", RunestoneCategory.Economy) },
        { RunestoneType.AncestorsFury, new RunestoneInfo(RunestoneType.AncestorsFury, "Tyr", "Jarl ability cooldowns -30%", RunestoneCategory.Combat) },
        { RunestoneType.IronDiscipline, new RunestoneInfo(RunestoneType.IronDiscipline, "Naudr", "All warriors +15% defense", RunestoneCategory.Combat) },
        { RunestoneType.WeaponMaintenance, new RunestoneInfo(RunestoneType.WeaponMaintenance, "Thurs", "All warriors +10% damage", RunestoneCategory.Combat) },
        { RunestoneType.ResilientBlood, new RunestoneInfo(RunestoneType.ResilientBlood, "Yr", "All villagers +10% max HP", RunestoneCategory.Combat) },
        { RunestoneType.FertileLands, new RunestoneInfo(RunestoneType.FertileLands, "Ur", "Villager birth rate +20%", RunestoneCategory.Survival) },
        { RunestoneType.RunekeepersGift, new RunestoneInfo(RunestoneType.RunekeepersGift, "Bjarkan", "Protection Runes cost 1 less stone brick", RunestoneCategory.Utility) },
        { RunestoneType.RaidingShips, new RunestoneInfo(RunestoneType.RaidingShips, "Logr", "Raids yield +10% loot", RunestoneCategory.Utility) },
    };

    public static RunestoneInfo GetInfo(RunestoneType type)
    {
        return runestones[type];
    }

    public static List<RunestoneInfo> GetByCategory(RunestoneCategory category)
    {
        return runestones.Values.Where(r => r.category == category).ToList();
    }

    public static List<RunestoneInfo> GetAllExcluding(IEnumerable<RunestoneType> excluded)
    {
        var excludeSet = new HashSet<RunestoneType>(excluded);
        return runestones.Values.Where(r => !excludeSet.Contains(r.type)).ToList();
    }
}
