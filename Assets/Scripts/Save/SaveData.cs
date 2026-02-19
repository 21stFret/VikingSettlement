using System;
using UnityEngine;

/// <summary>
/// Interface for managers that participate in save/load.
/// </summary>
public interface ISaveable
{
    void PopulateSaveData(SaveData data);
    void LoadSaveData(SaveData data);
}

/// <summary>
/// Root container for all serializable save data.
/// </summary>
[Serializable]
public class SaveData
{
    public string saveVersion = "1.0";
    public string saveName;
    public string saveTimestamp;
    public int slotNumber;

    public GameStateSave gameState;
    public ResourceSave[] resources;
    public VillagerSave[] villagers;
    public BuildingSave[] buildings;
    public MissionSaveData missions;
    public string currentJarlId;
    public SettlementStatsSave stats;
    public SkillTreeSaveData skillTreeData;
    public RunestoneSaveData runestoneData;
    public DeathTypeBuffSaveData deathBuffData;
}

[Serializable]
public class GameStateSave
{
    public float currentTimeOfDay;
    public int currentDay;
    public int currentSeason;           // SeasonManager.Season as int
    public int daysUntilSeasonChange;
    public int currentSolarYear;
}

[Serializable]
public class ResourceSave
{
    public int resourceType;            // ResourceType as int
    public float amount;
}

[Serializable]
public class VillagerSave
{
    public string id;
    public string villagerName;
    public int gender;
    public float age;
    public float lifeExpectancy;
    public int currentLifeStage;
    public float health;
    public float maxHealth;
    public float morale;
    public float maxMorale;
    public bool isHungry;
    public bool isCold;
    public int currentJob;
    public string assignedBuildingId;     // BuildingData SO name (not GUID)
    public VillagerSkillsSave skills;
    public CombatStatsSave combatStats;
    public string parent1Id;
    public string parent2Id;
    public string partnerId;
    public int childrenCount;
    public float timeSinceLastChild;
    public bool isJarl;
    public bool isOfJarlLineage;
    public int generationsFromJarl;
    public float posX;
    public float posY;
    public float posZ;
    public string spriteVariant;
    public string weaponName;
    public string shieldName;
    public string torchName;
    public int[] activeWounds; // WoundType cast to int
}

[Serializable]
public class VillagerSkillsSave
{
    public float farming;
    public float fishing;
    public float mining;
    public float woodcutting;
    public float crafting;
    public float combat;
    public float sailing;
    public float intelligence;
    public float learningRate;
}

[Serializable]
public class CombatStatsSave
{
    public float strength;
    public float defense;
}

[Serializable]
public class BuildingSave
{
    public string id;
    public string buildingDataName;     // BuildingData SO name reference
    public int gridPositionX;
    public int gridPositionY;
    public bool isConstructed;
    public float constructionProgress;
    public float productionProgress;
    public string[] assignedWorkerIds;
}

[Serializable]
public class MissionSaveData
{
    public ActiveMissionSave[] activeMissions;
    public string[] completedMissionIds;
}

[Serializable]
public class ActiveMissionSave
{
    public string missionDefinitionId;  // MissionDefinitionSO.missionId
    public float[] objectiveProgress;
    public int status;
}

[Serializable]
public class QuestGiverSave
{
    public string questGiverId;
    public int currentMissionIndex;
    public string activeMissionId;      // MissionDefinitionSO.missionId or empty
}

[Serializable]
public class SettlementStatsSave
{
    public int totalBirths;
    public int totalDeaths;
}

/// <summary>
/// Lightweight info about a save slot for UI listing.
/// </summary>
[Serializable]
public class SaveSlotInfo
{
    public string slotName;
    public string saveName;
    public string saveTimestamp;
    public bool exists;
    public int slotNumber;
}

[Serializable]
public class SkillTreeSaveData
{
    public int currentXP;
    public string[] unlockedSkillIds;
}

[Serializable]
public class RunestoneSaveData
{
    public int[] activeRunestoneTypes;
}

[Serializable]
public class DeathTypeBuffSaveData
{
    public int currentBuffType;
    public float remainingTime;
}
