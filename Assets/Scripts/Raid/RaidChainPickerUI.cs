using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// "Keep Sailing" destination picker — shown from RaidResultsUI after a leg resolves.
/// Near-clone of RaidUI's list rendering (reuses the same RaidOptionItemUI prefab/pool),
/// filtered to destinations not yet visited this trip, and skips party re-selection
/// entirely since the party carries over unchanged between legs.
/// </summary>
public class RaidChainPickerUI : MonoBehaviour, IRaidOptionSelector
{
    [SerializeField] private GameObject pickerPanel;
    [SerializeField] private List<RaidOptionItemUI> raidOptionItems;

    private List<RaidDestinationData> availableDestinations = new List<RaidDestinationData>();

    public void Open()
    {
        if (pickerPanel != null)
            pickerPanel.SetActive(true);

        Populate();
    }

    public void Close()
    {
        if (pickerPanel != null)
            pickerPanel.SetActive(false);
    }

    private void Populate()
    {
        availableDestinations = RaidManager.Instance.GetAvailableChainDestinations();
        float hopCost = RaidManager.Instance.GetHopCostDisplay();

        for (int i = 0; i < raidOptionItems.Count; i++)
        {
            if (i < availableDestinations.Count)
            {
                RaidDestinationData destination = availableDestinations[i];

                string fightLimit = destination.realTimeLimit > 0f
                    ? Mathf.RoundToInt(destination.realTimeLimit / 60f) + " min fight"
                    : "No time limit";

                // Unlike RaidUI's normal listing (which shows this destination's own round-trip
                // days), the chain picker shows the flat hopCost — every hop this trip costs the
                // same placeholder amount regardless of the destination's own travelTimeHours.
                string travelInfo = $"+{hopCost:F1} days to sail there";

                raidOptionItems[i].Setup(
                    destination.destinationName,
                    destination.enemyCount.ToString(),
                    travelInfo,
                    fightLimit,
                    BuildRewardsText(destination),
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

    public void SelectRaidOption(int index)
    {
        if (index < 0 || index >= availableDestinations.Count)
        {
            Debug.LogWarning($"Invalid chain raid index: {index}");
            return;
        }

        RaidDestinationData chosen = availableDestinations[index];
        Close();
        RaidManager.Instance.ContinueRaid(chosen);
    }

    private string BuildRewardsText(RaidDestinationData destination)
    {
        var sb = new StringBuilder();
        List<ResourceLoot> potentialLoot = destination.GetPotentialLoot();
        for (int j = 0; j < potentialLoot.Count; j++)
        {
            sb.Append(potentialLoot[j].resourceType).Append(" x").Append(potentialLoot[j].amount);
            if (j < potentialLoot.Count - 1)
                sb.Append('\n');
        }
        return sb.ToString();
    }
}
