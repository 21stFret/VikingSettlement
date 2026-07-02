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
    [Tooltip("Display-only loot shown in the raid selection UI.")]
    public List<ResourceLoot> potentialLoot = new List<ResourceLoot>();
    [Tooltip("Per-enemy kill loot table used during the raid scene.")]
    public List<LootTableEntry> lootTable = new List<LootTableEntry>();

    /// <summary>
    /// Total game days the settlement loses for this raid (travel there + travel back).
    /// </summary>
    public float GetGameDaysPassed() => (travelTimeHours * 2f) / 24f;
}
