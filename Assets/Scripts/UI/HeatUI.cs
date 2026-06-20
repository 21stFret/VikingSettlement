using TMPro;
using UnityEngine;

public class HeatUI : MonoBehaviour
{
    public TMP_Text heatText;
    public GameObject fireAnimation;
    public Color warm, cold;

    public TMP_Text requiredWoodText;
    public TMP_Text totalRequiredWoodText;

    public void Init()
    {
        UpdateHeatUI(true);
        SeasonManager.Instance.OnWarmthChanged += isWarm => UpdateHeatUI(isWarm);
    }

    public void UpdateHeatUI(bool isWarm)
    {
        if (isWarm)
        {
            heatText.color = warm;
            heatText.text = "Warm";
            fireAnimation.SetActive(true);
        }
        else
        {
            heatText.color = cold;
            heatText.text = "Cold";
            fireAnimation.SetActive(false);
        }
        UpdateWoodUI();
    }

    public void UpdateWoodUI()
    {
        requiredWoodText.text = "x" + SeasonManager.Instance.woodPerVillagerPerDay.ToString();
        totalRequiredWoodText.text = "(" + SeasonManager.Instance.GetWoodNeededPerDay().ToString() + ")";
    }
}
