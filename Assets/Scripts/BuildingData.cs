using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Building", menuName = "Viking Settlement/Building")]
public class BuildingData : ScriptableObject
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
    public int maxWorkers = 1;
    
    [Header("Production Type")]
    public ProductionType productionType = ProductionType.ResourceGathering;
    
    [Header("Resource Gathering (if applicable)")]
    public ResourceType producedResource;
    public float productionRate; // Speed of progress bar fill (units per second)
    public float productionAmount = 1; // Amount of resource produced when progress completes
    
    [Header("Crafting (if applicable)")]
    public CraftingRecipe craftingRecipe;
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
    public bool CanCraft()
    {
        if (ResourceManager.Instance == null) return false;
        
        foreach (var cost in inputResources)
        {
            if (ResourceManager.Instance.GetResource(cost.resourceType) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Consume the input resources
    /// </summary>
    public void ConsumeResources()
    {
        if (ResourceManager.Instance == null) return;
        
        foreach (var cost in inputResources)
        {
            ResourceManager.Instance.SpendResource(cost.resourceType, cost.amount);
        }
    }
}

[System.Serializable]
public class ResourceCost
{
    public ResourceType resourceType;
    public float amount;
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
    WeaversHut,
    Tannery,
    Barracks,
    ArcheryRange,
    Shipyard,
    TradingPost,
    HealersHut,
    ShamansHut,
    MeadHall
}

public enum ResourceType
{
    None, // For buildings that don't produce resources
    Wheat,
    Fish,
    Wood,
    Stone,
    Iron,
    Weapons,
    Tools,
    Armor,
    Clothing,
    Sails,
    Leather,
    Mead,
    Planks
}