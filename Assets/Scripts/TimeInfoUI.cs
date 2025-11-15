using TMPro;
using UnityEngine;

public class TimeInfoUI : MonoBehaviour
{
    public TMP_Text solarYearText;
    public TMP_Text dayText;
    public TMP_Text seasonText;
    public TMP_Text yearText;

    private void Update()
    {
        solarYearText.text = "Solar Year: " + SeasonManager.Instance.GetCurrentSolarYear().ToString();
        dayText.text = "Day: " + DayNightManager.Instance.GetCurrentDay().ToString();
        seasonText.text = "Season: " + SeasonManager.Instance.GetCurrentSeason().ToString();
        yearText.text = "Age: " + SettlementManager.Instance.age.ToString();
    }
}
