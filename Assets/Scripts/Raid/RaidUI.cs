using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaidUI : MonoBehaviour
{
    private RaidManager raidManager;
    public List<RaidOptionItemUI> raidOptionItems;
    public GameObject raidUIPanel;
    public TMP_Text raidAmountText;
    public Button closeButton;

    [Header("Party Selection")]
    public RaidPartyUI raidPartyUI;

    private int selectedRaidIndex = -1;

    private void Start()
    {
        
        raidUIPanel.SetActive(false);
        closeButton.onClick.AddListener(() => CloseRaidUI());

        // Find party UI if not assigned
        if (raidPartyUI == null)
        {
            raidPartyUI = GetComponentInChildren<RaidPartyUI>(true);
            if (raidPartyUI == null)
            {
                raidPartyUI = FindAnyObjectByType<RaidPartyUI>();
            }
        }
    }

    public void OpenRaidUI()
    {
        raidManager = RaidManager.Instance;
        raidUIPanel.SetActive(true);
        PopulateRaidOptions();
        raidAmountText.text = "Available Raids: " + raidManager.GetAvailableRaids().Count.ToString();
    }

    private void PopulateRaidOptions()
    {
        List<RaidDestination> availableRaids = raidManager.GetAvailableRaids();

        for (int i = 0; i < raidOptionItems.Count; i++)
        {
            if (i < availableRaids.Count)
            {
                RaidDestination raidOption = availableRaids[i];
                string rewards = "";
                for (int j = 0; j < raidOption.potentialLoot.Count; j++)
                {
                    rewards += raidOption.potentialLoot[j].resourceType.ToString() + " x" + raidOption.potentialLoot[j].amount.ToString();
                    if (raidOption.potentialLoot.Count > 1 && j < raidOption.potentialLoot.Count - 1)
                    {
                        rewards += "\n";
                    }
                }

                raidOptionItems[i].Setup(
                    raidOption.destinationName,
                    raidOption.enemyCount.ToString(),
                    raidOption.timeDilationMultiplier.ToString("F1") + "x",
                    raidOption.realTimeLimit + " mins",
                    rewards,
                    i
                );
                raidOptionItems[i].gameObject.SetActive(true);
            }
            else
            {
                raidOptionItems[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when a raid option is selected - opens the party selector
    /// </summary>
    public void SelectRaidOption(int raidIndex)
    {
        List<RaidDestination> availableRaids = raidManager.GetAvailableRaids();

        if (raidIndex < 0 || raidIndex >= availableRaids.Count)
        {
            Debug.LogWarning($"Invalid raid index: {raidIndex}");
            return;
        }

        selectedRaidIndex = raidIndex;
        RaidDestination selectedRaid = availableRaids[raidIndex];

        Debug.Log($"Selected raid: {selectedRaid.destinationName}");

        // Open party selection panel
        if (raidPartyUI != null)
        {
            raidPartyUI.OpenForRaid(selectedRaid);
        }
        else
        {
            Debug.LogError("RaidPartyUI not found! Cannot select party for raid.");
        }
    }

    /// <summary>
    /// Get the currently selected raid destination
    /// </summary>
    public RaidDestination GetSelectedRaid()
    {
        if (selectedRaidIndex < 0) return null;

        List<RaidDestination> availableRaids = raidManager.GetAvailableRaids();
        if (selectedRaidIndex < availableRaids.Count)
        {
            return availableRaids[selectedRaidIndex];
        }
        return null;
    }

    public void CloseRaidUI()
    {
        // Close party selection if open
        if (raidPartyUI != null && raidPartyUI.IsOpen())
        {
            raidPartyUI.Close();
        }

        raidUIPanel.SetActive(false);
        selectedRaidIndex = -1;
    }
}
