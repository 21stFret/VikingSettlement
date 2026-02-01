using TMPro;
using UnityEngine;

public class HeatUI : MonoBehaviour
{
    public TMP_Text heatText;
    public GameObject fireAnimation;
    public Color warm, cold;

    public TMP_Text requiredWoodText;

    public void Start()
    {
        SeasonManager.Instance.OnWarmthChanged += isWarm => UpdateHeatUI(isWarm);
    }

    public void UpdateHeatUI(bool isWarm)
    {
        requiredWoodText.text = "x" + SeasonManager.Instance.GetWoodNeededPerDay().ToString();
        if(isWarm)
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
    }
}
