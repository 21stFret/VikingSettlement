using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject defining a raid destination.
/// Settlement time lost = travelTimeHours * 2 (there and back), converted to game days.
/// The fight scene runs in real time against realTimeLimit; that duration does NOT affect
/// how many settlement days pass — only travelTimeHours does.
/// </summary>
[CreateAssetMenu(fileName = "RaidDestination", menuName = "Viking Settlement/Raid Destination")]
public class RaidDestinationData : ScriptableObject
{
    [Header("Identity")]
    public string destinationName = "Unknown Land";
    public string sceneName = "RaidScene";
    [TextArea(2, 4)]
    public string description = "A dangerous place to raid.";

    [Header("Travel Time")]
    [Tooltip("One-way travel time in in-game hours. Settlement loses travelTimeHours × 2 hours while the party is away.")]
    public float travelTimeHours = 48f;

    [Header("Fight Scene")]
    [Tooltip("Real-time fight limit in seconds (0 = no limit). Does not affect settlement time lost.")]
    public float realTimeLimit = 300f;
    public int recommendedPartySize = 3;
    public int enemyCount = 5;
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("If true, spawns one of each prefab (ignores enemyCount). If false, picks randomly enemyCount times.")]
    public bool spawnAll = false;

    [Header("Rewards")]
    [Tooltip("Chest loot at this camp — known in advance, rolled once on Victory. " +
             "Enemy \"pocket\" loot (Enemy.lootTable) is separate and not previewed.")]
    public List<LootTableEntry> lootTable = new List<LootTableEntry>();

    /// <summary>
    /// Display-only preview of this destination's chest loot ("what they have at the camp"),
    /// derived from lootTable so the UI can never drift out of sync with the actual roll.
    /// Shows each possible resource at its maximum roll amount, ignoring drop chance.
    /// </summary>
    public List<ResourceLoot> GetPotentialLoot()
    {
        var result = new List<ResourceLoot>();
        foreach (var entry in lootTable)
        {
            result.Add(new ResourceLoot { resourceType = entry.resourceType, amount = entry.maxAmount });
        }
        return result;
    }

    /// <summary>
    /// Total game days the settlement loses for this raid (travel there + travel back).
    /// </summary>
    public float GetGameDaysPassed() => (travelTimeHours * 2f) / 24f;

    /// <summary>
    /// One-way game days for this destination — used as D1 (first leg) and D_current
    /// (final "go home" leg) in the raid chain formula.
    /// </summary>
    public float GetOneWayGameDays() => travelTimeHours / 24f;
}
