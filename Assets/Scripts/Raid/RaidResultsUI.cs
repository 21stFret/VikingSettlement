using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Raid scene results popup. Assign panel references in the Inspector.
/// Subscribe to RaidManager.OnRaidEnded automatically — just place this in the raid scene.
/// Clicking the return button calls RaidManager.LoadSettlementScene().
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

    [Header("Button")]
    public Button returnButton;

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturnClicked);

        if (RaidManager.Instance != null)
            RaidManager.Instance.OnRaidEnded += ShowResults;
        else
            Debug.LogWarning("RaidResultsUI: RaidManager not found — results panel won't show.");
    }

    private void OnDestroy()
    {
        if (RaidManager.Instance != null)
            RaidManager.Instance.OnRaidEnded -= ShowResults;
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
        {
            if (report.loot == null || report.loot.Count == 0)
            {
                lootText.text = "No loot collected.";
            }
            else
            {
                var combined = RaidSceneController.Instance != null
                    ? RaidSceneController.Instance.GetCombinedLoot()
                    : report.loot;

                var lines = new System.Text.StringBuilder();
                foreach (var item in combined)
                    lines.AppendLine($"+ {item.amount:F0} {item.resourceType}");
                lootText.text = lines.ToString().TrimEnd();
            }
        }

        if (casualtiesText != null)
        {
            if (report.casualties == null || report.casualties.Count == 0)
            {
                casualtiesText.text = "No casualties.";
            }
            else
            {
                var lines = new System.Text.StringBuilder();
                foreach (var v in report.casualties)
                    if (v != null) lines.AppendLine(v.villagerName);
                casualtiesText.text = lines.ToString().TrimEnd();
            }
        }

        UIFocus.Set(returnButton.gameObject);
    }

    private void OnReturnClicked()
    {
        if (RaidManager.Instance != null)
            RaidManager.Instance.LoadSettlementScene();
        else
            Debug.LogError("RaidResultsUI: RaidManager gone — cannot load settlement.");
    }
}
