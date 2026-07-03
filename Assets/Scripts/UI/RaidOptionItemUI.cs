using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Implemented by any panel that lists RaidOptionItemUI rows and needs to know which
/// index was clicked — lets RaidChainPickerUI reuse this row prefab without depending
/// on the concrete RaidUI type.
/// </summary>
public interface IRaidOptionSelector
{
    void SelectRaidOption(int index);
}

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
        IRaidOptionSelector selector = GetComponentInParent<IRaidOptionSelector>();
        if (selector != null && raidIndex >= 0)
        {
            selector.SelectRaidOption(raidIndex);
        }
    }
}
