using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Displays detailed information about a selected building
/// </summary>
public class BuildingInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;

    [Header("Building Info")]
    [SerializeField] private BuildingSelector buildingSelector;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI productionInfoText;
    [SerializeField] private TextMeshProUGUI productionAmountText;
    [SerializeField] private TextMeshProUGUI productionAmountText2;
    [SerializeField] private TextMeshProUGUI consumeAmountText1;
    [SerializeField] private TextMeshProUGUI consumeAmountText2;
    [SerializeField] private Image resourceGeneratedIcon;
    [SerializeField] private Image resourceGeneratedIcon2;
    [SerializeField] private Image resourceConsumedIcon1;
    [SerializeField] private Image resourceConsumedIcon2;
    [SerializeField] private Button closeBuildingPanelButton;
    
    [Header("Production Progress")]

    [SerializeField] private GameObject productionSection;
    [SerializeField] private Image productionProgressBar;
    [SerializeField] private TextMeshProUGUI productionProgressText;
    [SerializeField] private TextMeshProUGUI estimatedTimeText;
    
    [Header("Workers")]
    [SerializeField] private GameObject workersSection;
    [SerializeField] private TextMeshProUGUI workerCountText;
    [SerializeField] private Transform workerListContainer;
    [SerializeField] private Button assignWorkerButton;
    
    [Header("Assign Worker Panel")]
    [SerializeField] private GameObject assignWorkerSection;
    [SerializeField] private Transform availableVillagersContainer;
    [SerializeField] private Button closeAssignPanelButton;
    
    [Header("Repair")]
    [SerializeField] private GameObject repairSection;
    [SerializeField] private Button repairButton;
    [SerializeField] private Transform repairCostContainer;
    [SerializeField] private Button closeRepairPanelButton;

    [Header("Level")]
    [SerializeField] private GameObject upgradeSection;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Transform upgradeCostContainer;

    [Header("Longhouse")]
    [SerializeField] private GameObject longhouseSection;
    [SerializeField] private TextMeshProUGUI populationText;
    [SerializeField] private TextMeshProUGUI combatText;
    [SerializeField] private TextMeshProUGUI MoraleText;

    [Header("Healers Hut")]
    [SerializeField] private GameObject healersSection;
    [SerializeField] private TextMeshProUGUI currentHealingBonus;
    [SerializeField] private Button removeWoundButton;

    [Header("Blacksmith Crafting Queue")]
    [SerializeField] private GameObject craftingQueueSection;
    [SerializeField] private Transform craftOptionsContainer; // holds pooled BlacksmithCraftOptionItem rows (the menu)
    [SerializeField] private Transform craftQueueContainer;   // holds pooled BlacksmithQueueItem rows (the current queue)
    [SerializeField] private TextMeshProUGUI craftSelectedNameText; // shared across all menu options - shows whichever icon is selected
    [SerializeField] private Transform craftSelectedCostContainer; // holds pooled SingleResourceDisplay rows, same as repair/upgrade costs
    [SerializeField] private Button craftQueueButton; // shared "Queue" button - queues the currently selected option
    [SerializeField] private Image craftingProgressBar;
    [SerializeField] private TextMeshProUGUI craftingProgressText;
    [SerializeField] private TextMeshProUGUI craftingTimeText;


    [Header("Colors")]
    [SerializeField] private Color progressBarColor = new Color(0.3f, 0.8f, 0.3f);
    
    public static BuildingInfoPanel Instance { get; private set; }

    private Building currentBuilding;
    private List<VillagerWorkerItem> workerListItems = new List<VillagerWorkerItem>();
    private List<VillagerWorkerItem> availableVillagerItems = new List<VillagerWorkerItem>();
    private List<SingleResourceDisplay> repairCostItems = new List<SingleResourceDisplay>();
    private List<SingleResourceDisplay> upgradeCostItems = new List<SingleResourceDisplay>();
    private List<BlacksmithCraftOptionItem> craftOptionItems = new List<BlacksmithCraftOptionItem>();
    private List<BlacksmithQueueItem> craftQueueItems = new List<BlacksmithQueueItem>();
    private List<SingleResourceDisplay> craftSelectedCostItems = new List<SingleResourceDisplay>();
    private EquipmentRecipe selectedCraftRecipe;
    private static readonly List<ResourceCost> EmptyCosts = new List<ResourceCost>();

    
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (productionProgressBar != null)
            productionProgressBar.color = progressBarColor;
        
        if (assignWorkerButton != null)
            assignWorkerButton.onClick.AddListener(OnAssignWorkerButtonClicked);

        if (repairButton != null)
            repairButton.onClick.AddListener(OnRepairButtonClicked);

        if (repairSection != null)
            repairSection.SetActive(false);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

        if (craftQueueButton != null)
            craftQueueButton.onClick.AddListener(OnCraftQueueButtonClicked);

        if (closeAssignPanelButton != null)
            closeAssignPanelButton.onClick.AddListener(CloseAssignPanel);
            
        if (closeBuildingPanelButton != null)
            closeBuildingPanelButton.onClick.AddListener(Hide);

        if (assignWorkerSection != null)
            assignWorkerSection.SetActive(false);

        if(closeRepairPanelButton != null)
            closeRepairPanelButton.onClick.AddListener(Hide);

        if (mainPanel !=null)
            mainPanel.SetActive(false);

        upgradeCostItems.Clear();
        upgradeCostItems.AddRange(upgradeCostContainer.GetComponentsInChildren<SingleResourceDisplay>(true));

        repairCostItems.Clear();
        repairCostItems.AddRange(repairCostContainer.GetComponentsInChildren<SingleResourceDisplay>(true));

        workerListItems.Clear();
        workerListItems.AddRange(workerListContainer.GetComponentsInChildren<VillagerWorkerItem>(true));

        availableVillagerItems.Clear();
        availableVillagerItems.AddRange(availableVillagersContainer.GetComponentsInChildren<VillagerWorkerItem>(true));

        craftOptionItems.Clear();
        if (craftOptionsContainer != null)
            craftOptionItems.AddRange(craftOptionsContainer.GetComponentsInChildren<BlacksmithCraftOptionItem>(true));

        craftQueueItems.Clear();
        if (craftQueueContainer != null)
            craftQueueItems.AddRange(craftQueueContainer.GetComponentsInChildren<BlacksmithQueueItem>(true));

        craftSelectedCostItems.Clear();
        if (craftSelectedCostContainer != null)
            craftSelectedCostItems.AddRange(craftSelectedCostContainer.GetComponentsInChildren<SingleResourceDisplay>(true));

        if (craftingQueueSection != null)
            craftingQueueSection.SetActive(false);
    }
    
    /// <summary>
    /// Show information for a specific building
    /// </summary>
    public void ShowBuilding(Building building, BuildingSelector selector = null)
    {
        if (building == null) return;

        currentBuilding = building;
        buildingSelector = selector;
        selectedCraftRecipe = null; // menu options belong to the previous building - don't carry a selection over
        mainPanel.SetActive(true);
        //GameTickManager.Instance?.PushUIPause();
        UpdateDisplay();

        if (CompendiumManager.Instance != null)
        {
            string buildingName = building.data.buildingName.ToString().ToLower().Replace(" ", "_");
            CompendiumManager.Instance.Discover("building_" + buildingName);
        }
    }
    
    /// <summary>
    /// Called by BuildingInteractionZone after opening the panel via controller.
    /// Sets EventSystem focus to the close button so gamepad can navigate immediately.
    /// </summary>
    public void FocusForController()
    {
        if (currentBuilding != null && currentBuilding.needsRepair)
            UIFocus.Set(repairButton != null ? repairButton.gameObject : closeBuildingPanelButton?.gameObject);
        else
            UIFocus.Set(assignWorkerButton != null ? assignWorkerButton.gameObject : closeBuildingPanelButton?.gameObject);
    }

    /// <summary>
    /// Hide the building info panel
    /// </summary>
    public void Hide()
    {
        mainPanel.SetActive(false);
        currentBuilding = null;
        GameTickManager.Instance?.PopUIPause();

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetInputEnabled(true);

        CloseAssignPanel();
        if (buildingSelector != null)
        {
            buildingSelector.Deselect();
        }
    }
    
    /// <summary>
    /// Update all displayed information
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentBuilding == null) return;

        // Building name and type
        if (buildingNameText != null)
            buildingNameText.text = currentBuilding.data.buildingName;

        if (levelText != null)
            levelText.text = $"Level {currentBuilding.level}";

        // Repair state overrides normal UI
        if (currentBuilding.needsRepair)
        {
            if (repairSection != null)
                repairSection.SetActive(true);

            RefreshRepairCosts();

            if (repairButton != null)
                repairButton.interactable = currentBuilding.CanRepair();

            if (productionSection != null) productionSection.SetActive(false);
            if (assignWorkerButton != null) assignWorkerButton.gameObject.SetActive(false);
            if (upgradeSection != null) upgradeSection.SetActive(false);
            if(workersSection  != null) workersSection.SetActive(false);
            if(longhouseSection != null) longhouseSection.SetActive(false);
            if (craftingQueueSection != null) craftingQueueSection.SetActive(false);
            return;
        }

        // Normal display
        if (repairSection != null) repairSection.SetActive(false);
        if (assignWorkerButton != null) assignWorkerButton.gameObject.SetActive(true);
        if (workersSection != null) workersSection.SetActive(true);
        if(longhouseSection != null) longhouseSection.SetActive(false);
        if(upgradeSection != null) upgradeSection.SetActive(true);
        if (craftingQueueSection != null) craftingQueueSection.SetActive(false);

        UpdateUpgradeDisplay();

        if (currentBuilding.data.buildingType == BuildingType.Longhouse)
        {
            if (productionSection != null) productionSection.SetActive(false);
            if (assignWorkerButton != null) assignWorkerButton.gameObject.SetActive(false);
            if (workersSection != null) workersSection.SetActive(false);
            if (craftingQueueSection != null) craftingQueueSection.SetActive(false);
            UpdateLonghouseDisplay();
            return;
        }

        if (currentBuilding.data.buildingType == BuildingType.HealersHut)
        {
            if (productionSection != null) productionSection.SetActive(false);
            if (assignWorkerButton != null) assignWorkerButton.gameObject.SetActive(true);
            if (workersSection != null) workersSection.SetActive(true);
            if (craftingQueueSection != null) craftingQueueSection.SetActive(false);
            UpdateHealersDisplay();
        }

        if (currentBuilding.data.buildingType == BuildingType.Blacksmith)
        {
            if (productionSection != null) productionSection.SetActive(false);
            if (assignWorkerButton != null) assignWorkerButton.gameObject.SetActive(true);
            if (workersSection != null) workersSection.SetActive(true);
            // Equipment crafting queue (Blacksmith-style)
            if (craftingQueueSection != null)
                craftingQueueSection.SetActive(currentBuilding.HasEquipmentQueue);

            if (currentBuilding.HasEquipmentQueue)
                RefreshCraftingQueueSection();
        }

        // Production info
        bool producesResources = currentBuilding.data.resourceOutputs.Count > 0 || currentBuilding.data.craftingRecipe.inputResources.Count > 0;

        if (productionSection != null)
            productionSection.SetActive(producesResources);

        if (currentBuilding != null && !currentBuilding.needsRepair)
        {
            if (producesResources)
            {
                UpdateProductionDisplay();
            }
        }
        else
        {
            if (productionSection != null)
                productionSection.SetActive(false);
        }

        // Worker info
        UpdateWorkerDisplay();
    }

    private void RefreshRepairCosts()
    {
        RefreshCostList(currentBuilding.repairCosts, repairCostItems);
    }

    private void OnRepairButtonClicked()
    {
        if (currentBuilding == null) return;
        currentBuilding.Repair();
        UpdateDisplay();
        UIFocus.Set(assignWorkerButton.gameObject);
    }

    private void UpdateLonghouseDisplay()
    {
        longhouseSection.SetActive(true);
        SettlementManager settlement = SettlementManager.Instance;
        combatText.text = $"Overall combat level: {settlement.GetOverrallCombat().ToString("F1")}";
        populationText.text = $"Population: {settlement.GetPopulation()} / {settlement.GetMaxPopulation()}";
        MoraleText.text = $"Morale: {settlement.GetOverrallMorale().ToString("F1")}";
    }

    private void UpdateHealersDisplay()
    {

    }

    /// <summary>
    /// Update the level/upgrade section (current level, upgrade cost, upgrade button state)
    /// </summary>
    private void UpdateUpgradeDisplay()
    {
        if (currentBuilding == null || upgradeSection == null) return;

        bool canLevelUp = currentBuilding.level < currentBuilding.data.maxLevel;
        upgradeSection.SetActive(true);

        if (!canLevelUp) return;

        RefreshUpgradeCosts();

        if (upgradeButton != null)
            upgradeButton.interactable = currentBuilding.CanUpgrade();
    }

    private void RefreshUpgradeCosts()
    {
        var nextLevel = currentBuilding.data.GetLevelData(currentBuilding.level + 1);
        RefreshCostList(nextLevel.upgradeCost, upgradeCostItems);
    }

    /// <summary>
    /// Populate a cost-row list from a pool, reusing existing rows and only
    /// instantiating new ones when the pool needs to grow. Unused rows are
    /// deactivated rather than destroyed so they can be reused next refresh.
    /// Expects itemPrefab to carry a SingleResourceDisplay component.
    /// </summary>
    private void RefreshCostList(IList<ResourceCost> costs, List<SingleResourceDisplay> pool)
    {
        // Deactivate leftover pooled rows instead of destroying them
        for (int i = costs.Count; i < pool.Count; i++)
        {
            pool[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < costs.Count; i++)
        {
            SingleResourceDisplay item = pool[i];
    
            item.gameObject.SetActive(true);

            var cost = costs[i];
            var display = item.GetComponent<SingleResourceDisplay>();
            if (display == null) continue;

            display.resourceType = cost.resourceType;

            if (display.icon != null && IconManager.Instance != null)
                display.icon.sprite = IconManager.Instance.GetIconForResource(cost.resourceType);

            if (display.amountText != null)
                display.amountText.text = cost.amount.ToString();
        }


    }

    private void OnUpgradeButtonClicked()
    {
        if (currentBuilding == null) return;
        currentBuilding.Upgrade();
        UpdateDisplay();
    }

    /// <summary>
    /// Populate the crafting menu (from data.craftableEquipment, via BlacksmithCraftOptionItem) and the
    /// current queue list (via BlacksmithQueueItem) for an equipment-queue building, e.g. Blacksmith.
    /// </summary>
    private void RefreshCraftingQueueSection()
    {
        if (currentBuilding == null) return;

        var options = currentBuilding.GetCraftableEquipmentOptions();

        // Keep the current selection if it's still a valid option for this building; otherwise default
        // to the first option (or nothing, if the menu is empty).
        if (selectedCraftRecipe == null || options == null || !options.Contains(selectedCraftRecipe))
            selectedCraftRecipe = (options != null && options.Count > 0) ? options[0] : null;

        for (int i = 0; i < craftOptionItems.Count; i++)
        {
            if (options == null || i >= options.Count)
            {
                craftOptionItems[i].gameObject.SetActive(false);
                continue;
            }
            craftOptionItems[i].Setup(options[i], this);
            craftOptionItems[i].SetSelected(options[i] == selectedCraftRecipe);
            craftOptionItems[i].gameObject.SetActive(true);
        }

        UpdateSelectedCraftDisplay();

        var queue = currentBuilding.GetQueuedEquipment();
        for (int i = 0; i < craftQueueItems.Count; i++)
        {
            if (queue == null || i >= queue.Count)
            {
                craftQueueItems[i].gameObject.SetActive(false);
                continue;
            }
            craftQueueItems[i].Setup(queue[i], i, this);
            craftQueueItems[i].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Update the shared name/cost text and Queue button to reflect selectedCraftRecipe.
    /// </summary>
    private void UpdateSelectedCraftDisplay()
    {
        if (selectedCraftRecipe == null)
        {
            if (craftSelectedNameText != null) craftSelectedNameText.text = "";
            RefreshCostList(EmptyCosts, craftSelectedCostItems);
            if (craftQueueButton != null) craftQueueButton.interactable = false;
            return;
        }

        if (craftSelectedNameText != null)
            craftSelectedNameText.text =  selectedCraftRecipe.itemName.Replace("_", " ");

        // Same icon+amount row display used for repair/upgrade costs - handles the common case of an
        // item needing two (or more, up to the pool size) different resources.
        RefreshCostList(selectedCraftRecipe.inputResources, craftSelectedCostItems);

        if (craftQueueButton != null)
            craftQueueButton.interactable = true;
    }

    /// <summary>
    /// Select a menu option so the shared name/cost text and Queue button reflect it.
    /// Called by BlacksmithCraftOptionItem.
    /// </summary>
    public void SelectCraftOption(EquipmentRecipe recipe)
    {
        selectedCraftRecipe = recipe;
        UpdateSelectedCraftDisplay();

        for (int i = 0; i < craftOptionItems.Count; i++)
            craftOptionItems[i].SetSelected(craftOptionItems[i].Recipe == recipe);
    }

    /// <summary>
    /// Called when the shared Queue button is clicked - queues whatever's currently selected.
    /// </summary>
    private void OnCraftQueueButtonClicked()
    {
        if (currentBuilding == null || selectedCraftRecipe == null) return;
        currentBuilding.QueueEquipmentItem(selectedCraftRecipe.itemName);
        RefreshCraftingQueueSection();
    }

    /// <summary>
    /// Cancel a queued item at the current building. Called by BlacksmithQueueItem.
    /// </summary>
    public void CancelQueuedCraftItem(int queueIndex)
    {
        if (currentBuilding == null) return;
        currentBuilding.CancelQueuedEquipment(queueIndex);
        RefreshCraftingQueueSection();
    }


    /// <summary>
    /// Update production progress display
    /// </summary>
    private void UpdateProductionDisplay()
    {
        if (currentBuilding == null) return;
        
        // Production info text
        if (productionInfoText != null)
        {
            resourceConsumedIcon1.gameObject.SetActive(false);
            resourceConsumedIcon2.gameObject.SetActive(false);
            resourceGeneratedIcon2.gameObject.SetActive(false);

            if (currentBuilding.data.productionType == ProductionType.ResourceGathering)
            {
                var outputs = currentBuilding.adjustedProductionAmounts;
                string outputList = string.Join(", ", outputs.Select(o => $"{o.amount} {o.resourceType}"));
                productionInfoText.text = $"Produces: {outputList}";

                // Update resource icon (shows the first output resource)
                if (resourceGeneratedIcon != null && outputs.Count > 0)
                {

                    for (int i = 0; i < outputs.Count; i++)
                    {
                        Sprite icon = IconManager.Instance.GetIconForResource(outputs[i].resourceType);
                        if(i==0)
                        {
                            resourceGeneratedIcon.sprite = icon;
                            if (productionAmountText != null)
                            {
                                productionAmountText.text = "+ " + outputs[i].amount.ToString();
                            }
                        }
                        if(i==1)
                        {
                            resourceGeneratedIcon2.gameObject.SetActive(true);
                            resourceGeneratedIcon2.sprite = icon;
                            productionAmountText2.text = "+ " + outputs[i].amount.ToString();
                        }

                    }
                }
            }
            else if (currentBuilding.data.productionType == ProductionType.Crafting &&
                     currentBuilding.data.craftingRecipe != null)
            {
                CraftingRecipe recipe = currentBuilding.data.craftingRecipe;
                float effectiveOutputAmount = recipe.outputAmount * currentBuilding.EffectiveProductionMultiplier;
                string inputs = "";
                foreach (var cost in recipe.inputResources)
                {
                    inputs += $"{cost.amount} {cost.resourceType}, ";
                }
                inputs = inputs.TrimEnd(',', ' ');

                string info = $"Crafts: {effectiveOutputAmount} {recipe.outputResource} from {inputs}";
                productionInfoText.text = info;

                if (productionAmountText != null)
                {
                    productionAmountText.text = $"+ {effectiveOutputAmount}";
                }

                // Update resource icon
                if (resourceGeneratedIcon != null)
                {
                    Sprite icon = IconManager.Instance.GetIconForResource(currentBuilding.data.craftingRecipe.outputResource);
                    resourceGeneratedIcon.sprite = icon;
                }

                // Update consume amount texts and icons for crafting buildings
                if (recipe.inputResources.Count > 0)
                {
                    
                    for (int i = 0; i < recipe.inputResources.Count; i++)
                    {
                        var input = recipe.inputResources[i];
                        if (i == 0)
                        {
                            if (consumeAmountText1 != null)
                            {
                                consumeAmountText1.text = $"- {input.amount}";
                            }
                            if (resourceConsumedIcon1 != null)
                            {
                                Sprite icon1 = IconManager.Instance.GetIconForResource(input.resourceType);
                                resourceConsumedIcon1.sprite = icon1;
                                resourceConsumedIcon1.gameObject.SetActive(true);
                            }
                        }
                        else if (i == 1)
                        {
                            if (consumeAmountText2 != null)
                            {
                                consumeAmountText2.text = $"- {input.amount}";
                            }
                            if (resourceConsumedIcon2 != null)
                            {
                                Sprite icon2 = IconManager.Instance.GetIconForResource(input.resourceType);
                                resourceConsumedIcon2.sprite = icon2;
                                resourceConsumedIcon2.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
            else
            {
                productionInfoText.text = "No production";
            }


        }
        
        // Progress bar
        if (productionProgressBar != null)
        {
            float progress = currentBuilding.GetProductionProgressPercent();
            productionProgressBar.fillAmount = progress;
        }
        
        // Progress text
        if (productionProgressText != null)
        {
            float progress = currentBuilding.productionProgress;
            productionProgressText.text = $"{progress:F1}%";
        }
        
        // Estimated time
        if (estimatedTimeText != null)
        {
            float timeToComplete = currentBuilding.GetEstimatedTimeToCompletion();
            
            if (currentBuilding.assignedWorkers.Count == 0)
            {
                estimatedTimeText.text = "No workers";
            }
            else if (float.IsInfinity(timeToComplete))
            {
                estimatedTimeText.text = "Calculating...";
            }
            else
            {
                estimatedTimeText.text = $"Time: {FormatTime(timeToComplete)}";
            }
        }
    }

    private void UpdateCraftingDisplay()
    { 
        // Progress bar
        if (craftingProgressBar != null)
        {
            float progress = currentBuilding.GetProductionProgressPercent();
            craftingProgressBar.fillAmount = progress;
        }

        // Progress text
        if (craftingProgressText != null)
        {
            float progress = currentBuilding.productionProgress;
            craftingProgressText.text = $"{progress:F1}%";
        }

        // Estimated time
        if (craftingTimeText != null)
        {
            float timeToComplete = currentBuilding.GetEstimatedTimeToCompletion();

            if (currentBuilding.assignedWorkers.Count == 0)
            {
                craftingTimeText.text = "No workers";
            }
            else
            {
                craftingTimeText.text = $"Time: {FormatTime(timeToComplete)}";
            }
        }

        if(craftingProgressBar.fillAmount ==0)
        {
            RefreshCraftingQueueSection();
        }
    }
    
    /// <summary>
    /// Update worker list display
    /// </summary>
    private void UpdateWorkerDisplay()
    {
        if (currentBuilding == null) return;
        
        // Worker count
        if (workerCountText != null)
        {
            int current = currentBuilding.assignedWorkers.Count;
            int max = currentBuilding.EffectiveMaxWorkers;
            workerCountText.text = $"Workers: {current}/{max}";
        }
        
        // Enable/disable assign button
        if (assignWorkerButton != null)
        {
            assignWorkerButton.interactable = currentBuilding.CanAssignWorker();
        }
        
        // Update worker list
        RefreshWorkerList();
    }
    
    /// <summary>
    /// Refresh the list of assigned workers
    /// </summary>
    private void RefreshWorkerList()
    {
        if (workerListContainer == null || workerListItems == null) return;

        for (int i = 0; i < workerListItems.Count; i++)
        {
            VillagerWorkerItem itemComponent = workerListItems[i].GetComponent<VillagerWorkerItem>();
            if (i >= currentBuilding.assignedWorkers.Count)
            {
                if (itemComponent != null) itemComponent.gameObject.SetActive(false);
                continue;
            }
            if (itemComponent != null)
            {
                itemComponent.Setup(currentBuilding.assignedWorkers[i], this, false);
                itemComponent.gameObject.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Called when assign worker button is clicked
    /// </summary>
    private void OnAssignWorkerButtonClicked()
    {
        if (assignWorkerSection != null)
        {
            assignWorkerSection.SetActive(true);
            RefreshAvailableVillagers();
        }
    }
    
    /// <summary>
    /// Refresh the list of available villagers
    /// </summary>
    private void RefreshAvailableVillagers()
    {
        // Get unemployed villagers
        List<Villager> unemployed = SettlementManager.Instance.GetUnemployedVillagers();

        for(int i = 0; i < availableVillagerItems.Count; i++)
        {
            // Setup the item
            VillagerWorkerItem itemComponent = availableVillagerItems[i];
            if(i>=unemployed.Count)
            {
                itemComponent.gameObject.SetActive(false);
                continue;
            }
            if (itemComponent != null)
            {
                itemComponent.Setup(unemployed[i], this, true);
                itemComponent.gameObject.SetActive(true);
            }
        }
        if(unemployed.Count > 0)
        {
            UIFocus.Set(availableVillagerItems[0].gameObject);
        }
        else
        {
            UIFocus.Set(closeAssignPanelButton != null ? closeAssignPanelButton.gameObject : assignWorkerButton.gameObject);
        }
    }
    
    /// <summary>
    /// Assign a villager to the current building
    /// </summary>
    public void AssignVillager(Villager villager)
    {
        if (currentBuilding != null && villager != null)
        {
            currentBuilding.AssignWorker(villager);
            UpdateDisplay();
            RefreshAvailableVillagers();
        }
    }
    
    /// <summary>
    /// Remove a worker from the current building
    /// </summary>
    public void RemoveWorker(Villager villager)
    {
        if (currentBuilding != null && villager != null)
        {
            currentBuilding.RemoveWorker(villager);
            UpdateDisplay();
            assignWorkerSection.SetActive(true);
            RefreshAvailableVillagers();
        }
    }
    
    /// <summary>
    /// Close the assign worker panel
    /// </summary>
    private void CloseAssignPanel()
    {
        if (assignWorkerSection != null)
            assignWorkerSection.SetActive(false);
        
    }
    
    /// <summary>
    /// Format seconds into readable time string
    /// </summary>
    private string FormatTime(float seconds)
    {
        if (seconds < 60)
            return $"{seconds:F0}s";
        
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes}m {secs}s";
    }

    /// <summary>
    /// Get the currently displayed building
    /// </summary>
    public Building GetCurrentBuilding()
    {
        return currentBuilding;
    }

    public void Update()
    {
        if (currentBuilding == null) return;

        bool producesResources = currentBuilding.data.resourceOutputs.Count > 0 || currentBuilding.data.craftingRecipe.inputResources.Count > 0;

        if (producesResources && !currentBuilding.needsRepair)
        {
            UpdateProductionDisplay();
        }

        if(currentBuilding.HasEquipmentQueue && currentBuilding.equipmentQueue.Count>0)
        {
            UpdateCraftingDisplay();
        }
   
    }
}
