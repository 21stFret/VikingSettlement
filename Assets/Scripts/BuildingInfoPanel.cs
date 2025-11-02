using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays detailed information about a selected building
/// </summary>
public class BuildingInfoPanel : MonoBehaviour
{
    [Header("Building Info")]
    [SerializeField] private BuildingSelector buildingSelector;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI buildingTypeText;
    [SerializeField] private TextMeshProUGUI productionInfoText;
    [SerializeField] private TextMeshProUGUI productionAmountText;
    [SerializeField] private TextMeshProUGUI consumeAmountText1;
    [SerializeField] private TextMeshProUGUI consumeAmountText2;
    [SerializeField] private Image resourceGeneratedIcon;
    [SerializeField] private Image resourceConsumedIcon1;
    [SerializeField] private Image resourceConsumedIcon2;
    [SerializeField] private Button closeBuildingPanelButton;
    
    [Header("Production Progress")]
    [SerializeField] private GameObject productionSection;
    [SerializeField] private Image productionProgressBar;
    [SerializeField] private TextMeshProUGUI productionProgressText;
    [SerializeField] private TextMeshProUGUI estimatedTimeText;
    
    [Header("Workers")]
    [SerializeField] private TextMeshProUGUI workerCountText;
    [SerializeField] private Transform workerListContainer;
    [SerializeField] private GameObject workerListItemPrefab;
    [SerializeField] private Button assignWorkerButton;
    
    [Header("Assign Worker Panel")]
    [SerializeField] private GameObject assignWorkerPanel;
    [SerializeField] private Transform availableVillagersContainer;
    [SerializeField] private GameObject availableVillagerItemPrefab;
    [SerializeField] private Button closeAssignPanelButton;
    
    [Header("Colors")]
    [SerializeField] private Color progressBarColor = new Color(0.3f, 0.8f, 0.3f);
    
    private Building currentBuilding;
    private List<GameObject> workerListItems = new List<GameObject>();
    private List<GameObject> availableVillagerItems = new List<GameObject>();

    
    
    private void Start()
    {
        if (productionProgressBar != null)
            productionProgressBar.color = progressBarColor;
        
        if (assignWorkerButton != null)
            assignWorkerButton.onClick.AddListener(OnAssignWorkerButtonClicked);

        if (closeAssignPanelButton != null)
            closeAssignPanelButton.onClick.AddListener(CloseAssignPanel);
            
        if (closeBuildingPanelButton != null)
            closeBuildingPanelButton.onClick.AddListener(Hide);

        if (assignWorkerPanel != null)
            assignWorkerPanel.SetActive(false);

        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Show information for a specific building
    /// </summary>
    public void ShowBuilding(Building building, BuildingSelector selector = null)
    {
        if (building == null) return;
        
        currentBuilding = building;
        buildingSelector = selector;
        gameObject.SetActive(true);
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Hide the building info panel
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentBuilding = null;
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
        
        if (buildingTypeText != null)
            buildingTypeText.text = currentBuilding.data.buildingType.ToString();
        
        // Production info
        bool producesResources = currentBuilding.data.producedResource != ResourceType.None;
        
        if (productionSection != null)
            productionSection.SetActive(producesResources);
        
        if (producesResources)
        {
            UpdateProductionDisplay();
        }
        
        // Worker info
        UpdateWorkerDisplay();
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
            
            if (currentBuilding.data.productionType == ProductionType.ResourceGathering)
            {
                string info = $"Produces: {currentBuilding.data.productionAmount} {currentBuilding.data.producedResource}";
                productionInfoText.text = info;

                if (productionAmountText != null)
                {
                    productionAmountText.text = $"+ {currentBuilding.data.productionAmount}";
                }

                
                // Update resource icon
                if (resourceGeneratedIcon != null)
                {
                    Sprite icon = IconManager.Instance.GetIconForResource(currentBuilding.data.producedResource);
                    resourceGeneratedIcon.sprite = icon;
                }
            }
            else if (currentBuilding.data.productionType == ProductionType.Crafting &&
                     currentBuilding.data.craftingRecipe != null)
            {
                CraftingRecipe recipe = currentBuilding.data.craftingRecipe;
                string inputs = "";
                foreach (var cost in recipe.inputResources)
                {
                    inputs += $"{cost.amount} {cost.resourceType}, ";
                }
                inputs = inputs.TrimEnd(',', ' ');

                string info = $"Crafts: {recipe.outputAmount} {recipe.outputResource} from {inputs}";
                productionInfoText.text = info;
            
                if (productionAmountText != null)
                {
                    productionAmountText.text = $"+ {recipe.outputAmount}";
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
                estimatedTimeText.text = "No workers assigned";
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
            int max = currentBuilding.data.maxWorkers;
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
        if (workerListContainer == null || workerListItemPrefab == null) return;
        
        // Clear existing items
        foreach (GameObject item in workerListItems)
        {
            if (item != null) Destroy(item);
        }
        workerListItems.Clear();
        
        // Create items for each worker
        foreach (Villager worker in currentBuilding.assignedWorkers)
        {
            GameObject item = Instantiate(workerListItemPrefab, workerListContainer);
            
            // Setup the item
            VillagerWorkerItem itemComponent = item.GetComponent<VillagerWorkerItem>();
            if (itemComponent != null)
            {
                itemComponent.Setup(worker, this, false);
            }
            
            workerListItems.Add(item);
        }
    }
    
    /// <summary>
    /// Called when assign worker button is clicked
    /// </summary>
    private void OnAssignWorkerButtonClicked()
    {
        if (assignWorkerPanel != null)
        {
            assignWorkerPanel.SetActive(true);
            RefreshAvailableVillagers();
        }
    }
    
    /// <summary>
    /// Refresh the list of available villagers
    /// </summary>
    private void RefreshAvailableVillagers()
    {
        if (availableVillagersContainer == null || availableVillagerItemPrefab == null) return;
        
        // Clear existing items
        foreach (GameObject item in availableVillagerItems)
        {
            if (item != null) Destroy(item);
        }
        availableVillagerItems.Clear();
        
        // Get unemployed villagers
        List<Villager> unemployed = SettlementManager.Instance.GetUnemployedVillagers();
        
        // Create items for each villager
        foreach (Villager villager in unemployed)
        {
            GameObject item = Instantiate(availableVillagerItemPrefab, availableVillagersContainer);
            
            // Setup the item
            VillagerWorkerItem itemComponent = item.GetComponent<VillagerWorkerItem>();
            if (itemComponent != null)
            {
                itemComponent.Setup(villager, this, true);
            }
            
            availableVillagerItems.Add(item);
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
            RefreshAvailableVillagers();
        }
    }
    
    /// <summary>
    /// Close the assign worker panel
    /// </summary>
    private void CloseAssignPanel()
    {
        if (assignWorkerPanel != null)
            assignWorkerPanel.SetActive(false);
        
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
        if (currentBuilding != null)
        {
            UpdateProductionDisplay();
        }
    }
}
