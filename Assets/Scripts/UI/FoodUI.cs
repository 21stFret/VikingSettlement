using TMPro;
using UnityEngine;

public class FoodUI : MonoBehaviour
{
    public TMP_Text starvingText;
    public Color starving, fed;

    public TMP_Text requiredWoodText;
    public TMP_Text totalRequiredWoodText;

    public void Init()
    {
        UpdateFoodUI(true);
        SettlementManager.Instance.OnFoodConsumed += isFed => UpdateFoodUI(isFed);
    }

    public void UpdateFoodUI(bool isFed)
    {
        if (!isFed)
        {
            starvingText.color = starving;
            starvingText.text = "Starving";
        }
        else
        {
            starvingText.color = fed;
            starvingText.text = "Fed";
        }
        UpdateFoodUI();
    }

    public void UpdateFoodUI()
    {
        requiredWoodText.text = "x" + SettlementManager.Instance.fishPerVillagerPerDay.ToString();
        totalRequiredWoodText.text = "(" + SettlementManager.Instance.totalFishNeeded.ToString() + ")";
    }
}
