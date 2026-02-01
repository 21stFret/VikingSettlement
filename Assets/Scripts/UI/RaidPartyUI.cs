using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel for selecting villagers to join a raid party
/// Uses object pooling - pre-created items are reused rather than instantiated/destroyed
/// </summary>
public class RaidPartyUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject partySelectionPanel;

    [Header("Available Villagers Pool")]
    [SerializeField] private List<RaidPartyMemberItem> availableVillagerPool;

    [Header("Party Members Pool")]
    [SerializeField] private List<RaidPartyMemberItem> partyMemberPool;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI partyCountText;
    [SerializeField] private TextMeshProUGUI totalCombatText;

    [Header("Buttons")]
    [SerializeField] private Button startRaidButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    [Header("Warning")]
    [SerializeField] private TextMeshProUGUI warningText;

    // Internal state
    private RaidDestination selectedRaid;
    private List<Villager> raidParty = new List<Villager>();

    private RaidUI raidUI;

    private void Awake()
    {
        if (partySelectionPanel != null)
        {
            partySelectionPanel.SetActive(false);
        }

        // Hide all pooled items initially
        HideAllPooledItems();
    }

    private void Start()
    {
        raidUI = GetComponentInParent<RaidUI>();
        if (raidUI == null)
        {
            raidUI = FindFirstObjectByType<RaidUI>();
        }

        if (startRaidButton != null)
        {
            startRaidButton.onClick.AddListener(OnStartRaidClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    /// <summary>
    /// Hide all items in both pools and clear their state
    /// </summary>
    private void HideAllPooledItems()
    {
        if (availableVillagerPool != null)
        {
            foreach (var item in availableVillagerPool)
            {
                if (item != null)
                {
                    item.Clear();
                    item.gameObject.SetActive(false);
                }
            }
        }

        if (partyMemberPool != null)
        {
            foreach (var item in partyMemberPool)
            {
                if (item != null)
                {
                    item.Clear();
                    item.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Open the party selection panel for a specific raid
    /// </summary>
    public void OpenForRaid(RaidDestination raid)
    {
        if (raid == null)
        {
            Debug.LogWarning("Cannot open party selection - no raid specified!");
            return;
        }

        selectedRaid = raid;
        raidParty.Clear();

        // Show panel
        if (partySelectionPanel != null)
        {
            partySelectionPanel.SetActive(true);
        }

        // Populate villager lists
        RefreshVillagerLists();

        // Update party stats
        UpdatePartyStats();
    }

    /// <summary>
    /// Close the party selection panel
    /// </summary>
    public void Close()
    {
        // Hide all pooled items
        HideAllPooledItems();

        if (partySelectionPanel != null)
        {
            partySelectionPanel.SetActive(false);
        }

        raidParty.Clear();
        selectedRaid = null;
    }

    /// <summary>
    /// Refresh both villager lists using pooled items
    /// </summary>
    private void RefreshVillagerLists()
    {
        // Hide all items first
        HideAllPooledItems();

        if (SettlementManager.Instance == null) return;

        // Get all villagers
        List<Villager> allVillagers = SettlementManager.Instance.GetAllVillagers();

        // Populate available villagers (those not in party)
        int availableIndex = 0;
        foreach (Villager villager in allVillagers)
        {
            if (!raidParty.Contains(villager))
            {
                if (availableVillagerPool != null && availableIndex < availableVillagerPool.Count)
                {
                    var item = availableVillagerPool[availableIndex];
                    if (item != null)
                    {
                        item.Setup(villager, this, false);
                        item.gameObject.SetActive(true);
                        availableIndex++;
                    }
                }
            }
        }

        // Populate party members
        int partyIndex = 0;
        foreach (Villager villager in raidParty)
        {
            if (partyMemberPool != null && partyIndex < partyMemberPool.Count)
            {
                var item = partyMemberPool[partyIndex];
                if (item != null)
                {
                    item.Setup(villager, this, true);
                    item.gameObject.SetActive(true);
                    partyIndex++;
                }
            }
        }
    }

    /// <summary>
    /// Add a villager to the raid party
    /// </summary>
    public void AddToParty(Villager villager)
    {
        if (villager == null || raidParty.Contains(villager)) return;

        // Check if we have room in the party pool
        if (partyMemberPool != null && raidParty.Count >= partyMemberPool.Count)
        {
            Debug.LogWarning("Party is full - no more pool slots available!");
            return;
        }

        raidParty.Add(villager);
        RefreshVillagerLists();
        UpdatePartyStats();
    }

    /// <summary>
    /// Remove a villager from the raid party
    /// </summary>
    public void RemoveFromParty(Villager villager)
    {
        if (villager == null || !raidParty.Contains(villager)) return;

        raidParty.Remove(villager);
        RefreshVillagerLists();
        UpdatePartyStats();
    }

    /// <summary>
    /// Update the party statistics display
    /// </summary>
    private void UpdatePartyStats()
    {
        // Party count
        if (partyCountText != null)
        {
            int recommended = selectedRaid?.recommendedPartySize ?? 3;
            string countColor = raidParty.Count >= recommended ? "white" : "yellow";
            partyCountText.text = $"Party: <color={countColor}>{raidParty.Count}/{recommended}</color>";
        }

        // Total combat skill
        if (totalCombatText != null)
        {
            float totalCombat = 0f;
            foreach (var villager in raidParty)
            {
                if (villager != null)
                {
                    totalCombat += villager.skills.combat;
                }
            }
            totalCombatText.text = $"Total Combat: {totalCombat:F1}";
        }

        // Warning text
        UpdateWarningText();

        // Start button state
        UpdateStartButton();
    }

    /// <summary>
    /// Update warning text based on party composition
    /// </summary>
    private void UpdateWarningText()
    {
        if (warningText == null) return;

        if (raidParty.Count == 0)
        {
            warningText.text = "Select villagers to join the raid party";
            warningText.color = Color.gray;
            return;
        }

        if (selectedRaid != null && raidParty.Count < selectedRaid.recommendedPartySize)
        {
            warningText.text = $"Warning: Party is smaller than recommended!";
            warningText.color = Color.yellow;
            return;
        }

        // Check for low combat skills
        float avgCombat = 0f;
        foreach (var v in raidParty)
        {
            avgCombat += v.skills.combat;
        }
        avgCombat /= raidParty.Count;

        if (avgCombat < 1f)
        {
            warningText.text = "Warning: Low average combat skill!";
            warningText.color = Color.yellow;
            return;
        }

        warningText.text = "Ready to raid!";
        warningText.color = Color.green;
    }

    /// <summary>
    /// Update the start raid button state
    /// </summary>
    private void UpdateStartButton()
    {
        if (startRaidButton == null) return;

        bool canStart = raidParty.Count > 0 && selectedRaid != null;
        startRaidButton.interactable = canStart;

        if (startButtonText != null)
        {
            startButtonText.text = canStart ? "Start Raid" : "Select Party";
        }
    }

    /// <summary>
    /// Called when start raid button is clicked
    /// </summary>
    private void OnStartRaidClicked()
    {
        if (selectedRaid == null || raidParty.Count == 0)
        {
            Debug.LogWarning("Cannot start raid - no raid selected or party empty!");
            return;
        }

        // Start the raid through RaidManager
        if (RaidManager.Instance != null)
        {
            bool success = RaidManager.Instance.StartRaid(selectedRaid, new List<Villager>(raidParty));
            if (success)
            {
                Close();
                if (raidUI != null)
                {
                    raidUI.CloseRaidUI();
                }
            }
        }
        else
        {
            Debug.LogError("RaidManager instance not found!");
        }
    }

    /// <summary>
    /// Called when cancel button is clicked
    /// </summary>
    private void OnCancelClicked()
    {
        Close();
    }

    /// <summary>
    /// Get the current raid party (for external access)
    /// </summary>
    public List<Villager> GetCurrentParty()
    {
        return new List<Villager>(raidParty);
    }

    /// <summary>
    /// Check if the panel is currently open
    /// </summary>
    public bool IsOpen()
    {
        return partySelectionPanel != null && partySelectionPanel.activeSelf;
    }

    /// <summary>
    /// Get the maximum party size based on pool capacity
    /// </summary>
    public int GetMaxPartySize()
    {
        return partyMemberPool?.Count ?? 0;
    }

    /// <summary>
    /// Get the maximum available villager display count based on pool capacity
    /// </summary>
    public int GetMaxAvailableDisplay()
    {
        return availableVillagerPool?.Count ?? 0;
    }
}
