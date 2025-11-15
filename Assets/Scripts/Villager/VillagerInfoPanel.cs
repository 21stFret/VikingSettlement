using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays detailed information about a selected villager
/// </summary>
public class VillagerInfoPanel : MonoBehaviour
{
    [Header("Villager Info")]
    [SerializeField] private TextMeshProUGUI villagerNameText;
    [SerializeField] private TextMeshProUGUI jobText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI moraleText;
    
    [Header("Health/Morale Bars")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image moraleBar;
    
    [Header("Skill Bars")]
    [SerializeField] private Image farmingBar;
    [SerializeField] private Image fishingBar;
    [SerializeField] private Image miningBar;
    [SerializeField] private Image woodcuttingBar;
    [SerializeField] private Image craftingBar;
    [SerializeField] private Image combatBar;
    
    [Header("Skill Text")]
    [SerializeField] private TextMeshProUGUI farmingText;
    [SerializeField] private TextMeshProUGUI fishingText;
    [SerializeField] private TextMeshProUGUI miningText;
    [SerializeField] private TextMeshProUGUI woodcuttingText;
    [SerializeField] private TextMeshProUGUI craftingText;
    [SerializeField] private TextMeshProUGUI combatText;
    
    [Header("Combat Stats")]
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI defenseText;
    
    [Header("Skill Colors")]
    [SerializeField] private Color farmingColor = new Color(0.4f, 0.8f, 0.4f); // Light green
    [SerializeField] private Color fishingColor = new Color(0.4f, 0.6f, 1f); // Blue
    [SerializeField] private Color miningColor = new Color(0.6f, 0.6f, 0.6f); // Gray
    [SerializeField] private Color woodcuttingColor = new Color(0.6f, 0.4f, 0.2f); // Brown
    [SerializeField] private Color craftingColor = new Color(1f, 0.6f, 0.2f); // Orange
    [SerializeField] private Color combatColor = new Color(1f, 0.3f, 0.3f); // Red
    
    [Header("Bar Colors")]
    [SerializeField] private Color healthColor = Color.green;
    [SerializeField] private Color moraleColor = Color.yellow;
    
    private Villager currentVillager;
    
    private void Start()
    {
        // Apply skill colors
        if (farmingBar != null) farmingBar.color = farmingColor;
        if (fishingBar != null) fishingBar.color = fishingColor;
        if (miningBar != null) miningBar.color = miningColor;
        if (woodcuttingBar != null) woodcuttingBar.color = woodcuttingColor;
        if (craftingBar != null) craftingBar.color = craftingColor;
        if (combatBar != null) combatBar.color = combatColor;
        
        if (healthBar != null) healthBar.color = healthColor;
        if (moraleBar != null) moraleBar.color = moraleColor;
        
        // Hide panel initially
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Display information for a specific villager
    /// </summary>
    public void ShowVillager(Villager villager)
    {
        if (villager == null) return;
        
        currentVillager = villager;
        gameObject.SetActive(true);
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Hide the info panel
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentVillager = null;
    }
    
    /// <summary>
    /// Update all displayed information
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentVillager == null) return;
        
        // Basic info
        if (villagerNameText != null)
            villagerNameText.text = currentVillager.villagerName;
        
        if (jobText != null)
            jobText.text = currentVillager.currentJob == JobType.None ? 
                "Unemployed" : currentVillager.currentJob.ToString();
        
        // Health and Morale
        if (healthText != null)
            healthText.text = $"Health: {currentVillager.currentHealth:F0}/{currentVillager.maxHealth:F0}";
        
        if (moraleText != null)
            moraleText.text = $"Morale: {currentVillager.morale:F0}/{currentVillager.maxMorale:F0}";
        
        if (healthBar != null)
            healthBar.fillAmount = currentVillager.currentHealth / currentVillager.maxHealth;
        
        if (moraleBar != null)
            moraleBar.fillAmount = currentVillager.morale / currentVillager.maxMorale;
        
        // Skills
        UpdateSkillBar(farmingBar, farmingText, currentVillager.skills.farming, "Farming");
        UpdateSkillBar(fishingBar, fishingText, currentVillager.skills.fishing, "Fishing");
        UpdateSkillBar(miningBar, miningText, currentVillager.skills.mining, "Mining");
        UpdateSkillBar(woodcuttingBar, woodcuttingText, currentVillager.skills.woodcutting, "Woodcutting");
        UpdateSkillBar(craftingBar, craftingText, currentVillager.skills.crafting, "Crafting");
        UpdateSkillBar(combatBar, combatText, currentVillager.skills.combat, "Combat");
        
        // Combat stats
        if (strengthText != null)
            strengthText.text = $"Strength: {currentVillager.combatStats.strength:F1}";
        
        if (defenseText != null)
            defenseText.text = $"Defense: {currentVillager.combatStats.defense:F1}";
    }
    
    /// <summary>
    /// Update a skill bar and text
    /// </summary>
    private void UpdateSkillBar(Image bar, TextMeshProUGUI text, float skillValue, string skillName)
    {
        if (bar != null)
        {
            // Normalize skill value (assuming max skill is 10)
            bar.fillAmount = Mathf.Clamp01(skillValue / 10f);
        }
        
        if (text != null)
        {
            text.text = $"{skillName}: {skillValue:F1}";
        }
    }
    
    private void Update()
    {
        // Update display every frame to show real-time changes
        if (currentVillager != null && gameObject.activeSelf)
        {
            UpdateDisplay();
        }
    }
}
