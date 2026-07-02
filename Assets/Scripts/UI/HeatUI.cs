using TMPro;
using UnityEngine;

public class HeatUI : MonoBehaviour
{
    public static HeatUI Instance { get; private set; }

    public TMP_Text heatText;
    public GameObject fireAnimation;
    public Color warm, cold;

    // Base cost label: population × woodPerVillagerPerDay — neutral number, always shown.
    public TMP_Text woodPerVillager;
    // Delta label: difference between actual cost (season + storm) and base cost.
    // Hidden when delta is zero (normal winter with no storm).
    public TMP_Text totalRequiredWoodText;
    private SettlementManager settlementManager;

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        UpdateHeatUI(true);
        settlementManager = SettlementManager.Instance;
        settlementManager.OnWarmthChanged += isWarm => UpdateHeatUI(isWarm);
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
        if (settlementManager == null) return;

        int   population = settlementManager.GetPopulation();
        float baseCost   = population * settlementManager.woodPerVillagerPerDay;
        float actualCost = settlementManager.GetTodayWoodCost();
        float delta      = Mathf.Round((actualCost - baseCost) * 10f) / 10f;

        woodPerVillager.text = "x " + settlementManager.woodPerVillagerPerDay.ToString("F0");

        if (delta > 0f)
        {
            totalRequiredWoodText.color = Color.red;
        }
        else
        {
            totalRequiredWoodText.color = Color.green;
        }
        if(delta==0)
        {
            totalRequiredWoodText.color = Color.white;
        }

        totalRequiredWoodText.text = "(" +actualCost.ToString("F0") + ")";
    }
}
