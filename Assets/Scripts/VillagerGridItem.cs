using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single villager card in the grid view
/// </summary>
public class VillagerGridItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI villagerNameText;
    [SerializeField] private TextMeshProUGUI jobText;
    [SerializeField] private Button selectButton;
    
    [Header("Visual Feedback")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f);
    
    private Villager villager;
    private VillagerListUI listUI;
    private bool isSelected = false;
    
    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnClick);
        }
    }
    
    /// <summary>
    /// Initialize this grid item with a villager
    /// </summary>
    public void Setup(Villager villager, VillagerListUI listUI)
    {
        this.villager = villager;
        this.listUI = listUI;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update the displayed information
    /// </summary>
    public void UpdateDisplay()
    {
        if (villager == null) return;
        
        if (villagerNameText != null)
            villagerNameText.text = villager.villagerName;
        
        if (jobText != null)
        {
            string jobName = villager.currentJob == JobType.None ?
                "Unemployed" : villager.currentJob.ToString();
                if(villager.currentLifeStage == LifeStage.Young)
                {
                    jobName = "Child";
                }
            jobText.text = jobName;
        }
    }
    
    /// <summary>
    /// Called when this villager card is clicked
    /// </summary>
    private void OnClick()
    {
        if (listUI != null)
        {
            listUI.SelectVillager(this);
        }
    }
    
    /// <summary>
    /// Set whether this item is selected
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }
    }
    
    /// <summary>
    /// Get the villager associated with this grid item
    /// </summary>
    public Villager GetVillager()
    {
        return villager;
    }
    
    private void Update()
    {
        // Update display periodically
        if (villager != null)
        {
            UpdateDisplay();
        }
    }
}
