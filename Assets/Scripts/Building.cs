using UnityEngine;
using System.Collections.Generic;

public class Building : MonoBehaviour
{
    public string uniqueId;
    public BuildingData data;
    public Vector2Int gridPosition;
    public bool isConstructed = false;
    public float constructionProgress = 0f;
    
    [Header("Production")]
    public float productionProgress = 0f; // 0 to 100
    public float adjustedProductionAmount = 0f; // Production amount after seasonal modifiers (for UI)

    [Header("Crafting Status")]
    public bool waitingForResources = false; // True if crafting building lacks input materials
    
    public List<Villager> assignedWorkers = new List<Villager>();
    
    private void Start()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }

        // Initialize gridPosition from world position if not already set
        if (gridPosition == Vector2Int.zero)
        {
            // Convert world position to grid position (assuming 1 unit = 1 grid cell)
            gridPosition = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
        }

        // Register with settlement manager
        if (SettlementManager.Instance != null)
        {
            SettlementManager.Instance.RegisterBuilding(this);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        if (SettlementManager.Instance != null)
        {
            SettlementManager.Instance.UnregisterBuilding(this);
        }
    }
    
    public bool CanAssignWorker()
    {
        return assignedWorkers.Count < data.maxWorkers;
    }
    
    public void AssignWorker(Villager villager)
    {
        if (CanAssignWorker())
        {
            assignedWorkers.Add(villager);
            villager.AssignJob(data.assignedJobType, this);
        }
    }
    
    public void RemoveWorker(Villager villager)
    {
        assignedWorkers.Remove(villager);
        villager.UnassignJob();
    }
    
    /// <summary>
    /// Update production progress. Call this every frame or on ticks.
    /// </summary>
    public void UpdateProduction(float deltaTime)
    {
        if (!isConstructed || assignedWorkers.Count == 0) return;
        
        // Handle based on production type
        if (data.productionType == ProductionType.ResourceGathering)
        {
            UpdateResourceGathering(deltaTime);
        }
        else if (data.productionType == ProductionType.Crafting)
        {
            UpdateCrafting(deltaTime);
        }
    }
    
    /// <summary>
    /// Update resource gathering buildings (farms, mines, etc.)
    /// </summary>
    private void UpdateResourceGathering(float deltaTime)
    {
        if (data.producedResource == ResourceType.None) return; // Building doesn't produce anything

        // Update adjusted production amount for UI display
        float seasonalMultiplier = GetSeasonalMultiplier();
        adjustedProductionAmount = Mathf.Max(seasonalMultiplier > 0 ? 1 : 0, Mathf.Round(data.productionAmount * seasonalMultiplier));

        // Calculate total production speed based on workers and their skills
        float productionSpeed = GetProductionSpeed(data.productionRate);

        // Increase progress bar
        productionProgress += productionSpeed * deltaTime;

        // Check if production is complete
        if (productionProgress >= 100f)
        {
            CompleteResourceGathering();
        }
    }
    
    /// <summary>
    /// Update crafting buildings (blacksmith, carpenter, etc.)
    /// </summary>
    private void UpdateCrafting(float deltaTime)
    {
        if (data.craftingRecipe == null) return;
        
        // Check if we have the required resources
        if (!data.craftingRecipe.CanCraft())
        {
            waitingForResources = true;
            // Don't progress if we lack materials
            return;
        }
        
        waitingForResources = false;
        
        // Calculate crafting speed based on workers and their skills
        float craftingSpeed = GetProductionSpeed(data.craftingRecipe.craftingRate);
        
        // Increase progress bar
        productionProgress += craftingSpeed * deltaTime;
        
        // Check if crafting is complete
        if (productionProgress >= 100f)
        {
            CompleteCrafting();
        }
    }
    
    /// <summary>
    /// Calculate the production speed based on workers and their skills
    /// </summary>
    private float GetProductionSpeed(float baseRate)
    {
        float totalSpeed = 0f;

        foreach (var worker in assignedWorkers)
        {
            // Each worker contributes their skill multiplier to the speed
            totalSpeed += baseRate * worker.GetSkillMultiplier(data.assignedJobType);
        }

        // Apply Tireless Workers runestone bonus
        if (RunestoneManager.Instance != null)
        {
            totalSpeed *= RunestoneManager.Instance.GetProductionSpeedMultiplier();
        }

        return totalSpeed;
    }
    
    /// <summary>
    /// Called when resource gathering reaches 100%
    /// </summary>
    private void CompleteResourceGathering()
    {
        // Use the pre-calculated adjusted amount (already set in UpdateResourceGathering)
        float amount = adjustedProductionAmount;

        // Apply Gefjon's Blessing food production bonus
        if (data.producedResource == ResourceType.Fish || data.producedResource == ResourceType.Wheat)
        {
            if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive)
            {
                amount *= (1f + DeathTypeBuff.Instance.GetFoodProductionPercent() / 100f);
            }
        }

        int finalAmount = Mathf.RoundToInt(amount);
        ResourceManager.Instance.AddResource(data.producedResource, finalAmount);

        // Reset progress (keep overflow for next cycle)
        productionProgress -= 100f;

        // Improve worker skills slightly on each completion
        foreach (var worker in assignedWorkers)
        {
            worker.skills.ImproveSkill(data.assignedJobType);
        }

        float seasonalMultiplier = GetSeasonalMultiplier();
        if (seasonalMultiplier < 1.0f)
        {
            Debug.Log($"{data.buildingName} produced {finalAmount} {data.producedResource} (reduced by {SeasonManager.Instance?.GetCurrentSeason()})");
        }
        else if (seasonalMultiplier > 1.0f)
        {
            Debug.Log($"{data.buildingName} produced {finalAmount} {data.producedResource} (bonus from {SeasonManager.Instance?.GetCurrentSeason()})");
        }
        else
        {
            Debug.Log($"{data.buildingName} produced {finalAmount} {data.producedResource}");
        }
    }

    /// <summary>
    /// Get the current seasonal production multiplier for this building
    /// </summary>
    public float GetSeasonalMultiplier()
    {
        if (SeasonManager.Instance != null)
        {
            return SeasonManager.Instance.GetProductionMultiplier(data.buildingType);
        }
        return 1.0f;
    }
    
    /// <summary>
    /// Called when crafting reaches 100%
    /// </summary>
    private void CompleteCrafting()
    {
        // Consume input resources
        data.craftingRecipe.ConsumeResources();
        
        // Produce output resource
        ResourceManager.Instance.AddResource(
            data.craftingRecipe.outputResource, 
            data.craftingRecipe.outputAmount
        );
        
        // Reset progress (keep overflow for next cycle)
        productionProgress -= 100f;
        
        // Improve worker skills on each completion
        foreach (var worker in assignedWorkers)
        {
            worker.skills.ImproveSkill(data.assignedJobType);
        }
        
        Debug.Log($"{data.buildingName} crafted {data.craftingRecipe.outputAmount} {data.craftingRecipe.outputResource}");
    }
    
    /// <summary>
    /// Get production progress as a percentage (0-1 for UI)
    /// </summary>
    public float GetProductionProgressPercent()
    {
        return Mathf.Clamp01(productionProgress / 100f);
    }
    
    /// <summary>
    /// Get estimated time until next production completion in seconds
    /// </summary>
    public float GetEstimatedTimeToCompletion()
    {
        float baseRate = data.productionType == ProductionType.ResourceGathering 
            ? data.productionRate 
            : (data.craftingRecipe != null ? data.craftingRecipe.craftingRate : 0f);
            
        float productionSpeed = GetProductionSpeed(baseRate);
        if (productionSpeed <= 0) return float.MaxValue;
        
        float remainingProgress = 100f - productionProgress;
        return remainingProgress / productionSpeed;
    }
    
    /// <summary>
    /// Check if this building is currently waiting for input resources (crafting only)
    /// </summary>
    public bool IsWaitingForResources()
    {
        return waitingForResources;
    }
    
    /// <summary>
    /// Get what resources this building needs (for UI display)
    /// </summary>
    public string GetRequiredResourcesText()
    {
        if (data.productionType != ProductionType.Crafting || data.craftingRecipe == null)
            return "";
        
        string text = "Needs: ";
        for (int i = 0; i < data.craftingRecipe.inputResources.Count; i++)
        {
            var input = data.craftingRecipe.inputResources[i];
            text += $"{input.amount} {input.resourceType}";
            if (i < data.craftingRecipe.inputResources.Count - 1)
                text += ", ";
        }
        return text;
    }
}