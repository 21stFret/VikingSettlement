using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Raid scene results popup. Assign panel references in the Inspector.
/// Two modes on the same panel:
///  - Leg resolved (RaidManager.OnLegResolved): chain still open — shows "Keep Sailing"
///    (disabled with "No further targets" once every destination is visited) and "Go Home".
///  - Trip over (RaidManager.OnRaidEnded): shows the aggregated trip totals with a single
///    "Return" button that loads the settlement scene.
/// </summary>
public class RaidResultsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject resultsPanel;

    [Header("Result Header")]
    public TMP_Text resultHeaderText;
    public TMP_Text timeSummaryText;

    [Header("Loot")]
    public TMP_Text lootText;

    [Header("Casualties")]
    public TMP_Text casualtiesText;

    [Header("Chain Choice")]
    public Button keepSailingButton;
    public TMP_Text keepSailingButtonLabel;
    public Button goHomeButton;
    public RaidChainPickerUI chainPickerUI;

    public GameObject shipLeavePopup;
    public Button leaveButton;
    public Button continueRaidButton;
    public ShipRaidClickable shipRaidClickable;

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (keepSailingButton != null)
            keepSailingButton.onClick.AddListener(OnKeepSailingClicked);

        if (goHomeButton != null)
            goHomeButton.onClick.AddListener(OnGoHomeClicked);

        if(shipLeavePopup != null)
            shipLeavePopup.SetActive(false);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);

        if (continueRaidButton != null)
            continueRaidButton.onClick.AddListener(OnContinueClicked);

        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.OnRaidEnded += ShowResults;
            RaidManager.Instance.OnLegResolved += ShowLegChoice;
        }
        else
        {
            Debug.LogWarning("RaidResultsUI: RaidManager not found — results panel won't show.");
        }
    }

    private void OnDestroy()
    {
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.OnRaidEnded -= ShowResults;
            RaidManager.Instance.OnLegResolved -= ShowLegChoice;
        }
    }

    public void ToggleLeavePopup(bool value)
    {
        shipLeavePopup.SetActive(value);
        if (value) { GameTickManager.Instance?.PushUIPause();  } 
        else 
        {
            shipRaidClickable.CloseRaidUI();
            GameTickManager.Instance?.PopUIPause();
        }
        GameTickManager.Instance?.ToggleRealPause(value);
    }

    private void OnLeaveClicked()
    {
        ToggleLeavePopup(false);
        RaidSceneController.Instance.LeaveRaid();
    }
    private void OnContinueClicked()
    {
        ToggleLeavePopup(false);

    }

    private void ShowResults(RaidReport report)
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (resultHeaderText != null)
        {
            resultHeaderText.text = report.result switch
            {
                RaidResult.Victory => "Victory!",
                RaidResult.Defeat  => "Defeat",
                RaidResult.Retreat => "Retreat",
                _                  => "Raid Over"
            };
        }

        if (timeSummaryText != null)
            timeSummaryText.text = $"{report.gameDaysPassed:F1} days passed";

        if (lootText != null)
            lootText.text = FormatLoot(report.loot);

        if (casualtiesText != null)
            casualtiesText.text = FormatBattleReport(report.casualtyNames, report.injuries);

        if (keepSailingButton != null) keepSailingButton.gameObject.SetActive(false);
        if (goHomeButton != null) goHomeButton.gameObject.SetActive(true);

        UIFocus.Set(goHomeButton.gameObject);
    }

    private void ShowLegChoice(LegReport report)
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);

        if (timeSummaryText != null)
            timeSummaryText.text = $"{report.totalTimeAwaySoFar:F1} days away so far";

        if (lootText != null)
            lootText.text = FormatLoot(report.legLoot);

        if (casualtiesText != null)
            casualtiesText.text = FormatBattleReport(report.legCasualtyNames, report.injuries);

        if (goHomeButton != null) goHomeButton.gameObject.SetActive(true);

        if (keepSailingButton != null)
        {
            keepSailingButton.gameObject.SetActive(true);
            keepSailingButton.interactable = report.canContinue;
            if (keepSailingButtonLabel != null)
                keepSailingButtonLabel.text = report.canContinue ? "Keep Sailing" : "No further targets";
        }

        GameObject focusTarget = (report.canContinue && keepSailingButton != null)
            ? keepSailingButton.gameObject
            : goHomeButton.gameObject;
        UIFocus.Set(focusTarget);
    }

    private void OnKeepSailingClicked()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (chainPickerUI != null)
            chainPickerUI.Open();
        else
            Debug.LogError("RaidResultsUI: chainPickerUI not assigned — cannot continue the raid chain.");
    }

    private void OnGoHomeClicked()
    {
        // Synchronously fires RaidManager.OnRaidEnded -> ShowResults re-shows the panel in trip-over mode.
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance?.GoHome();
            RaidManager.Instance.LoadSettlementScene();
        }
        else
            Debug.LogError("RaidResultsUI: RaidManager gone — cannot load settlement.");
    }

    private string FormatLoot(List<ResourceLoot> loot)
    {
        if (loot == null || loot.Count == 0)
            return "No loot collected.";

        var totals = new Dictionary<ResourceType, float>();
        foreach (var item in loot)
        {
            totals.TryGetValue(item.resourceType, out float current);
            totals[item.resourceType] = current + item.amount;
        }

        var lines = new StringBuilder();
        foreach (var kv in totals)
            lines.AppendLine($"+ {kv.Value:F0} {kv.Key}");
        return lines.ToString().TrimEnd();
    }

    /// <summary>
    /// Combines the dead and the wounded into one report so a raid isn't reported as "clean"
    /// just because nobody died. Format:
    ///   Fallen:
    ///     Ragnar
    ///
    ///   Wounded:
    ///     Bjorn - Torn Shoulder (18/40 HP)
    ///     Erik - 22/35 HP
    /// </summary>
    private string FormatBattleReport(List<string> casualtyNames, List<VillagerInjuryReport> injuries)
    {
        bool hasCasualties = casualtyNames != null && casualtyNames.Count > 0;
        bool hasInjuries = injuries != null && injuries.Count > 0;

        if (!hasCasualties && !hasInjuries)
            return "No casualties. Everyone returned unharmed.";

        var lines = new StringBuilder();

        if (hasCasualties)
        {
            lines.AppendLine("Fallen:");
            foreach (var name in casualtyNames)
                lines.AppendLine($"  {name}");
        }

        if (hasInjuries)
        {
            if (hasCasualties) lines.AppendLine();
            lines.AppendLine("Wounded:");
            foreach (var injury in injuries)
            {
                string woundNames = injury.newWounds != null && injury.newWounds.Count > 0
                    ? string.Join(", ", injury.newWounds.ConvertAll(w => WoundDatabase.Get(w).displayName)) + " "
                    : "";
                lines.AppendLine($"  {injury.villagerName} - {woundNames}({injury.currentHealth:F0}/{injury.maxHealth:F0} HP)");
            }
        }

        return lines.ToString().TrimEnd();
    }
}
