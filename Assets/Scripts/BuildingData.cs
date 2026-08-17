using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Building", menuName = "Viking Settlement/Building")]
public class BuildingData : ScriptableObject, ISerializationCallbackReceiver
{
    public string buildingName;
    public BuildingType buildingType;
    public Sprite buildingSprite;
    
    [Header("Construction Requirements")]
    public int woodCost;
    public int stoneCost;
    public int ironCost;
    public float constructionTime;

    [Header("Job Settings")]
    public JobType assignedJobType;
    [HideInInspector]
    public int maxWorkers = 1;

    [Header("Production Type")]
    public ProductionType productionType = ProductionType.ResourceGathering;

    [Header("Resource Gathering (if applicable)")]
    public List<ResourceOutput> resourceOutputs = new List<ResourceOutput>(); // All resources produced each cycle (e.g. Hunter's Lodge -> Meat + Pelts)
    public float productionRate; // Speed of progress bar fill (units per second) - shared cycle for all outputs

    // Legacy single-resource fields. Kept (hidden) only so existing BuildingData assets migrate
    // automatically into resourceOutputs on first load - see OnAfterDeserialize. Do not read these
    // directly; use resourceOutputs instead.
    [SerializeField, HideInInspector] private ResourceType producedResource;
    [SerializeField, HideInInspector] private float productionAmount = 1;

    [Header("Crafting (if applicable)")]
    public CraftingRecipe craftingRecipe;

    [Header("Equipment Crafting Menu (Blacksmith-style, optional)")]
    // If populated, this building crafts from a player-chosen queue of these recipes instead of
    // the single fixed craftingRecipe above (craftingRecipe.craftingRate is still used for speed).
    // See Building.HasEquipmentQueue / QueueEquipmentItem.
    public List<EquipmentRecipe> craftableEquipment = new List<EquipmentRecipe>();

    public const int GlobalMaxLevel = 5;

    [Header("Levels")]
    [Range(1, GlobalMaxLevel)] public int maxLevel = GlobalMaxLevel; // per-building cap, can be set lower than GlobalMaxLevel
    public List<BuildingLevelData> levels = new List<BuildingLevelData>();

    /// <summary>
    /// Get the level data for a given level, falling back to a synthesized
    /// level-1 entry (from the legacy flat fields) if not authored yet.
    /// </summary>
    public BuildingLevelData GetLevelData(int level)
    {
        var found = levels.Find(l => l.level == level);
        if (found != null) return found;
        return new BuildingLevelData { level = 1, maxWorkers = maxWorkers, productionMultiplier = 1f };
    }

    private void OnValidate()
    {
        maxLevel = Mathf.Clamp(maxLevel, 1, GlobalMaxLevel);

        for (int lvl = 1; lvl <= maxLevel; lvl++)
        {
            if (levels.Exists(l => l.level == lvl)) continue;
            levels.Add(new BuildingLevelData { level = lvl, maxWorkers = maxWorkers, productionMultiplier = 1f });
        }
    }

    /// <summary>
    /// One-time migration: assets saved before resourceOutputs existed still carry their single
    /// producedResource/productionAmount in the legacy hidden fields. Fold that into resourceOutputs
    /// the first time the asset deserializes, so nothing needs manual re-authoring.
    /// </summary>
    public void OnAfterDeserialize()
    {
        if (resourceOutputs == null) resourceOutputs = new List<ResourceOutput>();
        if (resourceOutputs.Count == 0 && productionType == ProductionType.ResourceGathering && producedResource != ResourceType.None)
        {
            resourceOutputs.Add(new ResourceOutput { resourceType = producedResource, amount = productionAmount });
        }
    }

    public void OnBeforeSerialize() { }
}

[System.Serializable]
public class BuildingLevelData
{
    [Min(1)] public int level = 1;
    public List<ResourceCost> upgradeCost = new List<ResourceCost>(); // cost to reach this level from the previous one; empty for level 1
    public int maxWorkers = 1;
    public float productionMultiplier = 1f; // scales base productionAmount/productionRate, or craftingRecipe outputAmount/craftingRate
    public int populationCapBonus = 0; // only meaningful on the Longhouse; SettlementManager sums this from Longhouse buildings only
}

[System.Serializable]
public class CraftingRecipe
{
    [Header("Input Resources (What's needed)")]
    public List<ResourceCost> inputResources = new List<ResourceCost>();
    
    [Header("Output (What's produced)")]
    public ResourceType outputResource;
    public float outputAmount = 1;
    
    [Header("Crafting Speed")]
    public float craftingRate = 1f; // Speed of progress bar fill (units per second)
    
    /// <summary>
    /// Check if we have enough resources to craft
    /// </summary>
    public bool CanCraft() => ResourceCostUtility.CanAfford(inputResources);

    /// <summary>
    /// Consume the input resources
    /// </summary>
    public void ConsumeResources() => ResourceCostUtility.Consume(inputResources);
}

/// <summary>
/// A single item the player can queue up at an equipment-crafting building (e.g. Blacksmith).
/// itemName must match the itemName of an EquipableItem template registered in WeaponDatabase.
/// </summary>
[System.Serializable]
public class EquipmentRecipe
{
    public string itemName;
    public List<ResourceCost> inputResources = new List<ResourceCost>();

    public bool CanCraft() => ResourceCostUtility.CanAfford(inputResources);
    public void ConsumeResources() => ResourceCostUtility.Consume(inputResources);
}

[System.Serializable]
public class ResourceCost
{
    public ResourceType resourceType;
    public float amount;
}

/// <summary>
/// Shared helpers for checking/consuming a List&lt;ResourceCost&gt; against ResourceManager. Used by both
/// CraftingRecipe (single fixed recipe) and EquipmentRecipe (player-queued items).
/// </summary>
public static class ResourceCostUtility
{
    public static bool CanAfford(List<ResourceCost> costs)
    {
        if (ResourceManager.Instance == null) return false;

        foreach (var cost in costs)
        {
            if (ResourceManager.Instance.GetResource(cost.resourceType) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }

    public static void Consume(List<ResourceCost> costs)
    {
        if (ResourceManager.Instance == null) return;

        foreach (var cost in costs)
        {
            ResourceManager.Instance.SpendResource(cost.resourceType, cost.amount);
        }
    }
}

[System.Serializable]
public class ResourceOutput
{
    public ResourceType resourceType;
    public float amount = 1;
}

public enum ProductionType
{
    ResourceGathering, // Produces resources from nothing (Farm, Mine, etc.)
    Crafting          // Consumes resources to produce other resources (Blacksmith, etc.)
}

public enum BuildingType
{
    Longhouse,
    Farm,
    FishermansHut,
    LumberCamp,
    Sawmill,
    Quarry,
    Mine,
    Blacksmith,
    CarpenterWorkshop,
    Tannery,
    Barracks,
    ArcheryRange,
    Shipyard,
    TradingPost,
    HealersHut,
    GodisHut,
    MeadHall,
    HuntingLodge
}

public enum ResourceType
{
    None, // For buildings that don't produce resources
    Wheat,
    Fish,
    Wood,
    Stone,
    Iron,
    Honey,
    Weapons,
    Tools,
    Armor,
    Shields,
    Sails,
    Leather,
    Mead,
    Planks,
    Gold,
    Meat,
    Pelts,
    Bread,
    Heal
}