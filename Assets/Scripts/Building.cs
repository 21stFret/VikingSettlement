using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class Building : MonoBehaviour
{
    public string uniqueId;
    public BuildingData data;
    public Vector2Int gridPosition;
    public bool isConstructed = false;

    [Header("Production")]
    public float productionProgress = 0f; // 0 to 100
    public List<ResourceOutput> adjustedProductionAmounts = new List<ResourceOutput>(); // Production amounts after seasonal modifiers, one per output resource (for UI)
    public GameObject worldUI;
    public Image liveProgressBar;

    [Header("Repair")]
    [SerializeField] private bool startsDamaged = false;
    public bool needsRepair = false;
    public List<ResourceCost> repairCosts = new List<ResourceCost>();
    public Sprite damagedSprite;
    [SerializeField] private SpriteRenderer buildingSprite;
    [SerializeField] private ParticleSystem[] damageVFX;
    private Sprite _normalSprite;

    public event System.Action OnRepaired;
    public static event System.Action<Building> OnAnyBuildingRepaired;
    public static event System.Action<Building> OnAnyWorkerAssigned;

    [Header("Levels")]
    public int level = 1;
    public event System.Action<int> OnLevelChanged;
    public static event System.Action<Building> OnAnyBuildingUpgraded;

    [Header("Crafting Status")]
    public bool waitingForResources = false; // True if crafting building lacks input materials
    public bool startedCrafting = false; // True if crafting has started at least once

    [Header("Equipment Crafting Queue (Blacksmith-style, optional)")]
    public List<string> equipmentQueue = new List<string>(); // Item names, FIFO order; index 0 is current/next
    public event System.Action OnEquipmentQueueChanged;

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
            gridPosition = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
        }

        if (buildingSprite == null)
            buildingSprite = GetComponent<SpriteRenderer>();
        if (buildingSprite != null)
            _normalSprite = buildingSprite.sprite;

        if (startsDamaged)
            SetNeedsRepair(true);

        // Register with settlement manager
        if (SettlementManager.Instance != null)
        {
            SettlementManager.Instance.RegisterBuilding(this);
        }

        worldUI.SetActive(false);

    }

    public void Init()
    {
        SetAdjustedProduction();
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        if (SettlementManager.Instance != null)
        {
            SettlementManager.Instance.UnregisterBuilding(this);
        }
    }
    
    public BuildingLevelData CurrentLevelData => data.GetLevelData(level);
    public int EffectiveMaxWorkers => CurrentLevelData.maxWorkers;
    public float EffectiveProductionMultiplier => CurrentLevelData.productionMultiplier;

    public bool CanAssignWorker()
    {
        return !needsRepair && assignedWorkers.Count < EffectiveMaxWorkers;
    }

    public bool CanUpgrade()
    {
        if (level >= data.maxLevel) return false;
        if (ResourceManager.Instance == null) return false;

        var next = data.GetLevelData(level + 1);
        foreach (var cost in next.upgradeCost)
        {
            if (ResourceManager.Instance.GetResource(cost.resourceType) < cost.amount)
                return false;
        }
        return true;
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;

        var next = data.GetLevelData(level + 1);
        foreach (var cost in next.upgradeCost)
            ResourceManager.Instance.SpendResource(cost.resourceType, cost.amount);

        level++;
        OnLevelChanged?.Invoke(level);
        OnAnyBuildingUpgraded?.Invoke(this);
        SetAdjustedProduction();
    }

    public void SetNeedsRepair(bool value)
    {
        needsRepair = value;

        if (buildingSprite != null)
            buildingSprite.sprite = needsRepair && damagedSprite != null ? damagedSprite : _normalSprite;

        foreach (var vfx in damageVFX)
        {
            if (vfx == null) continue;
            if (needsRepair) vfx.Play();
            else vfx.Stop();
        }
    }

    public bool CanRepair()
    {
        if (ResourceManager.Instance == null) return false;
        foreach (var cost in repairCosts)
        {
            if (ResourceManager.Instance.GetResource(cost.resourceType) < cost.amount)
                return false;
        }
        return true;
    }

    public void Repair()
    {
        if (!needsRepair || !CanRepair()) return;
        foreach (var cost in repairCosts)
            ResourceManager.Instance.SpendResource(cost.resourceType, cost.amount);
        SetNeedsRepair(false);
        OnRepaired?.Invoke();
        OnAnyBuildingRepaired?.Invoke(this);
    }
    
    public void AssignWorker(Villager villager)
    {
        if (CanAssignWorker())
        {
            assignedWorkers.Add(villager);
            villager.AssignJob(data.assignedJobType, this);
            OnAnyWorkerAssigned?.Invoke(this);
            worldUI.SetActive(true); // Show UI when a worker is assigned
            if(equipmentQueue.Count<=0 && data.buildingType == BuildingType.Blacksmith)
            {
                worldUI.SetActive(false);
            }
        }
    }
    
    public void RemoveWorker(Villager villager)
    {
        assignedWorkers.Remove(villager);
        villager.UnassignJob();
        if(assignedWorkers.Count == 0)
        {
            worldUI.SetActive(false); // Hide UI when no workers are assigned
        }
    }

    public void SetAdjustedProduction()
    {
        // Update adjusted production amounts for UI display
        float seasonalMultiplier = GetSeasonalMultiplier();
        adjustedProductionAmounts.Clear();
        foreach (var output in data.resourceOutputs)
        {
            float amount = Mathf.Max(seasonalMultiplier > 0 ? 1 : 0, Mathf.Round(output.amount * EffectiveProductionMultiplier * seasonalMultiplier));
            adjustedProductionAmounts.Add(new ResourceOutput { resourceType = output.resourceType, amount = amount });
        }
    }
    
    /// <summary>
    /// Update production progress. Call this every frame or on ticks.
    /// </summary>
    public void UpdateProduction(float deltaTime)
    {
        if (!isConstructed || needsRepair || assignedWorkers.Count == 0) return;
        
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
        if (data.resourceOutputs == null || data.resourceOutputs.Count == 0) return; // Building doesn't produce anything

        // Calculate total production speed based on workers and their skills
        float productionSpeed = GetProductionSpeed(data.productionRate);

        // Increase progress bar
        productionProgress += productionSpeed * deltaTime;

        liveProgressBar.fillAmount = productionProgress / 100;

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
        if (HasEquipmentQueue)
        {
            UpdateEquipmentCrafting(deltaTime);
            return;
        }

        if (data.craftingRecipe == null) return;

        if(!startedCrafting)
        {
            // Check if we have the required resources
            if (!data.craftingRecipe.CanCraft())
            {
                waitingForResources = true;
                // Don't progress if we lack materials
                return;
            }

            waitingForResources = false;

            // Consume input resources
            data.craftingRecipe.ConsumeResources();
            startedCrafting = true;
            Debug.Log($"{data.buildingName} started crafting {data.craftingRecipe.outputResource}");
        }


        // Calculate crafting speed based on workers and their skills
        float craftingSpeed = GetProductionSpeed(data.craftingRecipe.craftingRate);
        
        // Increase progress bar
        productionProgress += craftingSpeed * deltaTime;

        liveProgressBar.fillAmount = productionProgress / 100;

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
        // Use the pre-calculated adjusted amounts (already set in UpdateResourceGathering)
        bool gefjonActive = DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive;
        float gefjonFoodPct = gefjonActive ? DeathTypeBuff.Instance.GetFoodProductionPercent() : 0f;

        var producedAmounts = new List<int>();
        foreach (var output in adjustedProductionAmounts)
        {
            float amount = SettlementFormulas.ApplyGefjonFoodBonus(output.resourceType, output.amount, gefjonActive, gefjonFoodPct);
            int finalAmount = Mathf.RoundToInt(amount);
            ResourceManager.Instance.AddResource(output.resourceType, finalAmount);
            producedAmounts.Add(finalAmount);
        }

        // Reset progress (keep overflow for next cycle)
        productionProgress -= 100f;

        // Improve worker skills slightly on each completion
        foreach (var worker in assignedWorkers)
        {
            worker.skills.ImproveJob(data.assignedJobType);
        }

        string producedText = string.Join(", ", adjustedProductionAmounts.Select((output, i) => $"{producedAmounts[i]} {output.resourceType}"));
        float seasonalMultiplier = GetSeasonalMultiplier();
        if (seasonalMultiplier < 1.0f)
        {
            Debug.Log($"{data.buildingName} produced {producedText} (reduced by {SeasonManager.Instance?.GetCurrentSeason()})");
        }
        else if (seasonalMultiplier > 1.0f)
        {
            Debug.Log($"{data.buildingName} produced {producedText} (bonus from {SeasonManager.Instance?.GetCurrentSeason()})");
        }
        else
        {
            Debug.Log($"{data.buildingName} produced {producedText}");
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
        // Produce output resource
        float outputAmount = data.craftingRecipe.outputAmount;
        var outputItem = data.craftingRecipe.outputResource;
        if (isForArmory(outputItem))
        {
            WeaponDatabase wd = WeaponDatabase.Instance;
            wd.AddItemToVillageArmory(wd.GetItemByName(outputItem.ToString()));
        }
        else
        {
            if(outputItem == ResourceType.Heal)
            {
                // Heal the most injured villager for building amount (1)
                Villager hurt = SettlementManager.Instance.GetMostWoundedVillager();
                if(hurt!=null)
                {
                    hurt.Heal(outputAmount);
                }
            }
            else
            {
                ResourceManager.Instance.AddResource(data.craftingRecipe.outputResource, outputAmount);
            }

        }


        // Reset progress (keep overflow for next cycle)
        productionProgress -= 100f;
        startedCrafting = false;

        // Improve worker skills on each completion
        foreach (var worker in assignedWorkers)
        {
            worker.skills.ImproveJob(data.assignedJobType);
        }

        Debug.Log($"{data.buildingName} crafted {outputAmount} {data.craftingRecipe.outputResource}");
    }

    private bool isForArmory(ResourceType type)
    {
        if(type == ResourceType.Weapons || type == ResourceType.Armor || type == ResourceType.Shields)
        {
            return true;
        }
        return false;
    }

    #region Equipment Crafting Queue

    /// <summary>
    /// True if this building crafts from a player-chosen queue of equipable items (e.g. Blacksmith)
    /// instead of the single fixed craftingRecipe.
    /// </summary>
    public bool HasEquipmentQueue => data.craftableEquipment != null && data.craftableEquipment.Count > 0;

    /// <summary>
    /// The menu of items this building can craft (name + resource cost), for UI display.
    /// </summary>
    public IReadOnlyList<EquipmentRecipe> GetCraftableEquipmentOptions() => data.craftableEquipment;

    /// <summary>
    /// The current queue of item names, in crafting order (index 0 is current/next).
    /// </summary>
    public IReadOnlyList<string> GetQueuedEquipment() => equipmentQueue;

    /// <summary>
    /// Add an item to the back of the crafting queue. Returns false if itemName isn't a valid
    /// option for this building. Resources aren't checked/consumed until the item reaches the
    /// front of the queue and starts crafting.
    /// </summary>
    public bool QueueEquipmentItem(string itemName)
    {
        if (!HasEquipmentQueue) return false;
        if (!data.craftableEquipment.Exists(r => r.itemName == itemName)) return false;

        equipmentQueue.Add(itemName);
        OnEquipmentQueueChanged?.Invoke();
        worldUI.SetActive(true);
        return true;
    }

    /// <summary>
    /// Remove a queued item by its position in the queue. Resources are only ever consumed once an
    /// item reaches the front and actually starts crafting (see UpdateEquipmentCrafting), so cancelling
    /// anything still waiting in line is free. Cancelling the item currently being crafted (index 0,
    /// resources already spent) forfeits those materials - no refund - and resets progress.
    /// </summary>
    public bool CancelQueuedEquipment(int queueIndex)
    {
        if (queueIndex < 0 || queueIndex >= equipmentQueue.Count) return false;

        if (queueIndex == 0 && startedCrafting)
        {
            productionProgress = 0f;
            startedCrafting = false;
            waitingForResources = false;
            worldUI.SetActive(false);
        }

        equipmentQueue.RemoveAt(queueIndex);
        OnEquipmentQueueChanged?.Invoke();
        return true;
    }

    private EquipmentRecipe FindEquipmentRecipe(string itemName)
    {
        return data.craftableEquipment?.Find(r => r.itemName == itemName);
    }

    /// <summary>
    /// Update equipment-queue crafting buildings (Blacksmith). Mirrors UpdateCrafting's flow -
    /// same worker/skill/runestone-adjusted speed via GetProductionSpeed - but crafts whatever
    /// item is at the front of equipmentQueue instead of a single fixed recipe.
    /// </summary>
    private void UpdateEquipmentCrafting(float deltaTime)
    {
        if (equipmentQueue.Count == 0)
        {
            waitingForResources = false;
            return;
        }

        EquipmentRecipe recipe = FindEquipmentRecipe(equipmentQueue[0]);
        if (recipe == null)
        {
            // Stale entry (e.g. removed from the design data) - drop it and move on.
            equipmentQueue.RemoveAt(0);
            OnEquipmentQueueChanged?.Invoke();
            return;
        }

        if (!startedCrafting)
        {
            if (!recipe.CanCraft())
            {
                waitingForResources = true;
                return;
            }

            waitingForResources = false;
            recipe.ConsumeResources();
            startedCrafting = true;
            Debug.Log($"{data.buildingName} started crafting {recipe.itemName}");
        }

        float craftingSpeed = GetProductionSpeed(data.craftingRecipe != null ? data.craftingRecipe.craftingRate : 1f);

        productionProgress += craftingSpeed * deltaTime;
        liveProgressBar.fillAmount = productionProgress / 100;

        if (productionProgress >= 100f)
        {
            CompleteEquipmentCrafting(recipe);
        }
    }

    /// <summary>
    /// Called when the item at the front of the equipment queue finishes crafting.
    /// </summary>
    private void CompleteEquipmentCrafting(EquipmentRecipe recipe)
    {
        WeaponDatabase wd = WeaponDatabase.Instance;
        EquipableItem template = wd != null ? wd.GetItemByName(recipe.itemName) : null;
        if (template != null)
        {
            wd.AddItemToVillageArmory(template);
        }
        else
        {
            Debug.LogWarning($"{data.buildingName} finished crafting '{recipe.itemName}' but no matching item template was found in WeaponDatabase.");
        }

        equipmentQueue.RemoveAt(0);
        productionProgress = 0;
        startedCrafting = false;
        liveProgressBar.fillAmount = productionProgress / 100;

        foreach (var worker in assignedWorkers)
        {
            worker.skills.ImproveJob(data.assignedJobType);
        }
        worldUI.SetActive(false);
        Debug.Log($"{data.buildingName} crafted {recipe.itemName}");
        OnEquipmentQueueChanged?.Invoke();
    }

    #endregion

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
        if (HasEquipmentQueue && equipmentQueue.Count == 0) return float.MaxValue;

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
        if (data.productionType != ProductionType.Crafting)
            return "";

        List<ResourceCost> inputResources;
        if (HasEquipmentQueue)
        {
            if (equipmentQueue.Count == 0) return "";
            EquipmentRecipe recipe = FindEquipmentRecipe(equipmentQueue[0]);
            if (recipe == null) return "";
            inputResources = recipe.inputResources;
        }
        else
        {
            if (data.craftingRecipe == null) return "";
            inputResources = data.craftingRecipe.inputResources;
        }

        string text = "Needs: ";
        for (int i = 0; i < inputResources.Count; i++)
        {
            var input = inputResources[i];
            text += $"{input.amount} {input.resourceType}";
            if (i < inputResources.Count - 1)
                text += ", ";
        }
        return text;
    }
}