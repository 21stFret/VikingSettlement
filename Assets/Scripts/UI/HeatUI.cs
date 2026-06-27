using TMPro;
using UnityEngine;

public class HeatUI : MonoBehaviour
{
    public static HeatUI Instance { get; private set; }

    public TMP_Text heatText;
    public GameObject fireAnimation;
    public Color warm, cold;

    // Base cost label: population × woodPerVillagerPerDay — neutral number, always shown.
    public TMP_Text requiredWoodText;
    // Delta label: difference between actual cost (season + storm) and base cost.
    // Hidden when delta is zero (normal winter with no storm).
    public TMP_Text totalRequiredWoodText;

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        UpdateHeatUI(true);
        SeasonManager.Instance.OnWarmthChanged += isWarm => UpdateHeatUI(isWarm);
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay += UpdateWoodUI;
    }

    private void OnDestroy()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay -= UpdateWoodUI;
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
        if (SeasonManager.Instance == null || SettlementManager.Instance == null) return;

        int   population = SettlementManager.Instance.GetPopulation();
        float baseCost   = population * SeasonManager.Instance.woodPerVillagerPerDay;
        float actualCost = SeasonManager.Instance.GetTodayWoodCost();
        float delta      = Mathf.Round((actualCost - baseCost) * 10f) / 10f;

        requiredWoodText.text = baseCost.ToString("F1");

        if (Mathf.Approximately(delta, 0f))
        {
            totalRequiredWoodText.gameObject.SetActive(false);
        }
        else
        {
            totalRequiredWoodText.gameObject.SetActive(true);
            if (delta > 0f)
            {
                totalRequiredWoodText.color = Color.red;
                totalRequiredWoodText.text  = "+" + delta.ToString("F1");
            }
            else
            {
                totalRequiredWoodText.color = Color.green;
                totalRequiredWoodText.text  = delta.ToString("F1");
            }
        }
    }
}
