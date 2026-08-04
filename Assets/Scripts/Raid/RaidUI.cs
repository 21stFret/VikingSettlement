using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaidUI : MonoBehaviour, IRaidOptionSelector
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
        GameTickManager.Instance?.PushUIPause();
        PlayerController.Instance?.SetInputEnabled(false);
        PopulateRaidOptions();
        raidAmountText.text = "Available Raids: " + raidManager.GetAvailableRaids().Count.ToString();
    }

    private void PopulateRaidOptions()
    {
        List<RaidDestinationData> availableRaids = raidManager.GetAvailableRaids();

        for (int i = 0; i < raidOptionItems.Count; i++)
        {
            if (i < availableRaids.Count)
            {
                RaidDestinationData raidOption = availableRaids[i];
                string rewards = "";
                List<ResourceLoot> potentialLoot = raidOption.GetPotentialLoot();
                for (int j = 0; j < potentialLoot.Count; j++)
                {
                    rewards += potentialLoot[j].resourceType.ToString() + " x" + potentialLoot[j].amount.ToString();
                    if (potentialLoot.Count > 1 && j < potentialLoot.Count - 1)
                    {
                        rewards += "\n";
                    }
                }

                string fightLimit = raidOption.realTimeLimit > 0f
                    ? Mathf.RoundToInt(raidOption.realTimeLimit / 60f) + " min fight"
                    : "No time limit";
                raidOptionItems[i].Setup(
                    raidOption.destinationName,
                    raidOption.enemyCount.ToString(),
                    raidOption.GetGameDaysPassed().ToString("F1") + " days away",
                    fightLimit,
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
        List<RaidDestinationData> availableRaids = raidManager.GetAvailableRaids();

        if (raidIndex < 0 || raidIndex >= availableRaids.Count)
        {
            Debug.LogWarning($"Invalid raid index: {raidIndex}");
            return;
        }

        selectedRaidIndex = raidIndex;
        RaidDestinationData selectedRaid = availableRaids[raidIndex];

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
    public RaidDestinationData GetSelectedRaid()
    {
        if (selectedRaidIndex < 0) return null;

        List<RaidDestinationData> availableRaids = raidManager.GetAvailableRaids();
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
        GameTickManager.Instance?.PopUIPause();
        PlayerController.Instance?.SetInputEnabled(true);
    }
}
