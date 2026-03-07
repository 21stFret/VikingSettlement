using UnityEngine;
using Season = SeasonManager.Season;

/// <summary>
/// Listens to SeasonManager.OnSeasonChanged and pushes a message to InfoPopupUI.
/// Attach to any persistent GameObject in the game scene.
/// </summary>
public class SeasonNotificationHook : MonoBehaviour
{
    private void OnEnable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged += OnSeasonChanged;
    }

    private void OnDisable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnSeasonChanged -= OnSeasonChanged;
    }

    private void OnSeasonChanged(Season season)
    {
        switch (season)
        {
            case Season.Summer:
                InfoPopupUI.Push("Summer", "Summer has arrived. Crops and bees thrive — farming and beekeeping are at their best. Fishing is poor.");
                break;
            case Season.Winter:
                InfoPopupUI.Push("Winter", "Winter is here. Farms and hives lie dormant — move workers to the Fishing Hut.");
                break;
        }
    }
}
