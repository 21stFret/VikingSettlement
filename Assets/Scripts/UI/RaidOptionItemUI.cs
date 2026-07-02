using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RaidOptionItemUI : MonoBehaviour
{
    public TMP_Text RaidNameText;
    public TMP_Text RaidDifficultyText;
    [FormerlySerializedAs("RaidDilationText")]
    public TMP_Text RaidTravelText;
    public TMP_Text RaidTimeLimitText;
    public TMP_Text RaidRewardText;
    public Button m_SelectButton;

    private int raidIndex = -1;

    public void Setup(string raidName, string difficulty, string travelInfo, string timeLimit, string reward, int index = -1)
    {
        RaidNameText.text = raidName;
        RaidDifficultyText.text = difficulty;
        RaidTravelText.text = travelInfo;
        RaidTimeLimitText.text = timeLimit;
        RaidRewardText.text = reward;
        raidIndex = index;

        m_SelectButton.onClick.RemoveAllListeners();
        m_SelectButton.onClick.AddListener(() => OnSelectRaid());
    }

    private void OnSelectRaid()
    {
        RaidUI raidUI = GetComponentInParent<RaidUI>();
        if (raidUI != null && raidIndex >= 0)
        {
            raidUI.SelectRaidOption(raidIndex);
        }
    }
}
