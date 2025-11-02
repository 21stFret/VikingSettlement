using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages the grid view of all villagers
/// </summary>
public class VillagerListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform gridContainer; // Parent object with Grid Layout Group
    [SerializeField] private GameObject villagerGridItemPrefab;
    [SerializeField] private VillagerInfoPanel infoPanel;
    
    [Header("Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 1f; // Refresh grid every second
    
    private List<VillagerGridItem> gridItems = new List<VillagerGridItem>();
    private VillagerGridItem currentlySelected;
    private float refreshTimer = 0f;
    
    private void Start()
    {
        RefreshVillagerList();
    }
    
    private void Update()
    {
        if (autoRefresh)
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                RefreshVillagerList();
            }
        }
    }
    
    /// <summary>
    /// Refresh the entire villager list
    /// </summary>
    public void RefreshVillagerList()
    {
        if (SettlementManager.Instance == null) return;
        
        List<Villager> allVillagers = SettlementManager.Instance.GetAllVillagers();
        
        // Remove grid items for villagers that no longer exist
        for (int i = gridItems.Count - 1; i >= 0; i--)
        {
            if (gridItems[i] == null || gridItems[i].GetVillager() == null || 
                !allVillagers.Contains(gridItems[i].GetVillager()))
            {
                if (gridItems[i] != null)
                {
                    Destroy(gridItems[i].gameObject);
                }
                gridItems.RemoveAt(i);
            }
        }
        
        // Add new villagers that don't have grid items yet
        foreach (Villager villager in allVillagers)
        {
            bool hasGridItem = false;
            foreach (VillagerGridItem item in gridItems)
            {
                if (item.GetVillager() == villager)
                {
                    hasGridItem = true;
                    break;
                }
            }
            
            if (!hasGridItem)
            {
                CreateGridItem(villager);
            }
        }
    }
    
    /// <summary>
    /// Create a new grid item for a villager
    /// </summary>
    private void CreateGridItem(Villager villager)
    {
        if (villagerGridItemPrefab == null || gridContainer == null) return;
        
        GameObject itemObj = Instantiate(villagerGridItemPrefab, gridContainer);
        VillagerGridItem item = itemObj.GetComponent<VillagerGridItem>();
        
        if (item != null)
        {
            item.Setup(villager, this);
            gridItems.Add(item);
        }
    }
    
    /// <summary>
    /// Called when a villager grid item is clicked
    /// </summary>
    public void SelectVillager(VillagerGridItem item)
    {
        // Deselect previous
        if (currentlySelected != null)
        {
            currentlySelected.SetSelected(false);
        }
        
        // Select new
        currentlySelected = item;
        item.SetSelected(true);
        
        // Show in info panel
        if (infoPanel != null)
        {
            infoPanel.ShowVillager(item.GetVillager());
        }
    }
    
    /// <summary>
    /// Deselect the current villager
    /// </summary>
    public void DeselectVillager()
    {
        if (currentlySelected != null)
        {
            currentlySelected.SetSelected(false);
            currentlySelected = null;
        }
        
        if (infoPanel != null)
        {
            infoPanel.Hide();
        }
    }
    
    /// <summary>
    /// Force refresh the list immediately
    /// </summary>
    public void ForceRefresh()
    {
        RefreshVillagerList();
    }
}
