using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Village-scene homecoming report. Shown after RaidManager.OnReturnedToSettlement fires
/// (i.e. after the settlement simulation for the time away has been applied). Freezes the
/// game via PauseManager.RaidReportPause until the player clicks Continue.
/// </summary>
public class HomecomingReportUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject reportPanel;

    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Button")]
    public Button continueButton;

    private void Awake()
    {
        if (reportPanel != null)
            reportPanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (RaidManager.Instance != null)
            RaidManager.Instance.OnReturnedToSettlement += ShowReport;
        else
            Debug.LogWarning("HomecomingReportUI: RaidManager not found — report won't show.");
    }

    private void OnDestroy()
    {
        if (RaidManager.Instance != null)
            RaidManager.Instance.OnReturnedToSettlement -= ShowReport;
    }

    private void ShowReport(SettlementReport report)
    {
        PauseManager.Instance?.SetPauseState(PauseManager.PauseState.RaidReportPause);

        if (reportPanel != null)
            reportPanel.SetActive(true);

        if (titleText != null)
            titleText.text = report.events.Count == 0 ? "All Quiet" : "Homecoming Report";

        if (bodyText != null)
            bodyText.text = ComposeBody(report);

        if (continueButton != null)
            UIFocus.Set(continueButton.gameObject);
    }

    private string ComposeBody(SettlementReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{report.exactDaysPassed:F1} days passed while you were away.");

        foreach (var change in report.resourceChanges)
        {
            if(change.Value == 0)
                continue;
            string sign = change.Value >= 0 ? "+" : "";
            sb.AppendLine($"{sign}{change.Value:F0} {change.Key}");
        }

        if (report.events.Count == 0)
            sb.AppendLine("The village was quiet.");
        else
            foreach (var ev in report.events)
                sb.AppendLine(ev.description);

        return sb.ToString().TrimEnd();
    }

    private void OnContinueClicked()
    {
        if (reportPanel != null)
            reportPanel.SetActive(false);

        PauseManager.Instance?.SetPauseState(PauseManager.PauseState.Playing);
    }
}
