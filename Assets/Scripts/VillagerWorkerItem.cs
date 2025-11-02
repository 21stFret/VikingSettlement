using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents an available villager that can be assigned to a building
/// </summary>
public class VillagerWorkerItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI villagerNameText;
    [SerializeField] private TextMeshProUGUI skillInfoText;
    [SerializeField] private Button assignButton;
    [SerializeField] private Image skillIconImage;
    
    private Villager villager;
    private BuildingInfoPanel buildingPanel;

    public bool isAssignedItem = false; // True if this item represents an assigned worker
       
    /// <summary>
    /// Setup this available villager item
    /// </summary>
    public void Setup(Villager villager, BuildingInfoPanel panel, bool isAssignedItem)
    {
        this.villager = villager;
        this.buildingPanel = panel;
        this.isAssignedItem = isAssignedItem;

        UpdateDisplay();

        if(isAssignedItem)
        {
            assignButton.onClick.AddListener(OnAssignClicked);
        }
        else
        {
            assignButton.onClick.AddListener(OnRemoveClicked);
        }
    }
    
    /// <summary>
    /// Update the displayed information
    /// </summary>
    private void UpdateDisplay()
    {
        if (villager == null) return;
        
        if (villagerNameText != null)
            villagerNameText.text = villager.villagerName;
        
        if (skillInfoText != null)
        {
            // Show relevant skill for this building's job
            Building building = buildingPanel.GetComponent<BuildingInfoPanel>().GetCurrentBuilding();
            if (building != null)
            {
                JobType job = building.data.assignedJobType;
                float skill = villager.skills.GetSkillForJob(job);
                skillInfoText.text = $"{skill:F1}";
                SetSkillIcon(job);
            }
            else
            {
                skillInfoText.text = "Unemployed";
            }
        }
    }

    /// <summary>
    /// Called when assign button is clicked
    /// </summary>
    private void OnAssignClicked()
    {
        if (buildingPanel != null && villager != null)
        {
            buildingPanel.AssignVillager(villager);
        }
    }

        /// <summary>
    /// Called when remove button is clicked
    /// </summary>
    private void OnRemoveClicked()
    {
        if (buildingPanel != null && villager != null)
        {
            buildingPanel.RemoveWorker(villager);
        }
    }
    
    /// <summary>
    /// Set the skill icon based on job type
    /// </summary>
    private void SetSkillIcon(JobType job)
    {
        if (skillIconImage == null) return;
        Sprite icon = IconManager.Instance.GetIconForJob(job);
        skillIconImage.sprite = icon;
    }
}
