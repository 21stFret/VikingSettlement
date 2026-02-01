using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a villager item in the raid party selector (either available or in party)
/// </summary>
public class RaidPartyMemberItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI villagerNameText;
    [SerializeField] private TextMeshProUGUI combatSkillText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Image backgroundImage;

    [Header("Visual Settings")]
    [SerializeField] private Color availableColor = new Color(0.2f, 0.3f, 0.2f, 1f);
    [SerializeField] private Color inPartyColor = new Color(0.3f, 0.2f, 0.1f, 1f);
    [SerializeField] private Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private Villager villager;
    private RaidPartyUI partyUI;
    private bool isInParty = false;

    public Villager Villager => villager;

    /// <summary>
    /// Setup this item for a villager (called when pulled from pool)
    /// </summary>
    public void Setup(Villager villager, RaidPartyUI partyUI, bool inParty)
    {
        this.villager = villager;
        this.partyUI = partyUI;
        this.isInParty = inParty;

        // Clear and re-add listener
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Clear item state (called when returning to pool)
    /// </summary>
    public void Clear()
    {
        villager = null;
        partyUI = null;
        isInParty = false;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.interactable = true;
        }
    }

    /// <summary>
    /// Update the displayed information
    /// </summary>
    private void UpdateDisplay()
    {
        if (villager == null) return;

        // Name
        if (villagerNameText != null)
        {
            villagerNameText.text = villager.villagerName;
        }

        // Combat skill
        if (combatSkillText != null)
        {
            float combat = villager.skills.combat;
            combatSkillText.text = $"Combat: {combat:F1}";
        }

        // Status (job, life stage, etc.)
        if (statusText != null)
        {
            string status = "";
            if (villager.currentLifeStage != LifeStage.Mature)
            {
                status = villager.currentLifeStage.ToString();
            }
            else if (villager.isJarl)
            {
                status = "Jarl";
            }
            else if (villager.currentJob != JobType.None)
            {
                status = villager.currentJob.ToString();
            }
            else
            {
                status = "Available";
            }
            statusText.text = status;
        }

        // Button text and color based on party state
        if (isInParty)
        {
            if (actionButtonText != null)
                actionButtonText.text = "Remove";
            if (backgroundImage != null)
                backgroundImage.color = inPartyColor;
            if (actionButton != null)
                actionButton.interactable = true; // Can always remove from party
        }
        else
        {
            bool canJoin = CanJoinRaid();
            if (actionButtonText != null)
                actionButtonText.text = canJoin ? "Add" : "-";
            if (backgroundImage != null)
                backgroundImage.color = canJoin ? availableColor : unavailableColor;
            if (actionButton != null)
                actionButton.interactable = canJoin;
        }
    }

    /// <summary>
    /// Check if this villager can join a raid
    /// </summary>
    private bool CanJoinRaid()
    {
        if (villager == null) return false;

        // Must be mature
        if (villager.currentLifeStage != LifeStage.Mature) return false;

        // Cannot be dead
        if (villager.IsDead()) return false;

        // Cannot already be on a raid
        if (villager.isOnRaid) return false;

        return true;
    }

    /// <summary>
    /// Called when the action button is clicked
    /// </summary>
    private void OnActionButtonClicked()
    {
        if (partyUI == null || villager == null) return;

        if (isInParty)
        {
            partyUI.RemoveFromParty(villager);
        }
        else
        {
            partyUI.AddToParty(villager);
        }
    }
}
