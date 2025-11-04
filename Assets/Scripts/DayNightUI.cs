using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current day and time information
/// </summary>
public class DayNightUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to display day and time")]
    public TextMeshProUGUI dayTimeText;

    [Tooltip("Text component to display next meal time")]
    public TextMeshProUGUI nextMealText;

    [Header("Display Settings")]
    [Tooltip("Show the UI panel")]
    public bool showUI = true;

    private void Update()
    {
        if (!showUI || DayNightManager.Instance == null) return;

        UpdateDayTimeDisplay();
    }

    private void UpdateDayTimeDisplay()
    {
        if (dayTimeText != null)
        {
            string timeStr = DayNightManager.Instance.GetFormattedTime();
            int day = DayNightManager.Instance.GetCurrentDay();
            dayTimeText.text = $"Day {day}\n{timeStr}";
        }

        if (nextMealText != null)
        {
            float currentTime = DayNightManager.Instance.GetTimeOfDay();
            float mealTime = 0.5f; // This should match DayNightManager.mealTime

            if (currentTime < mealTime)
            {
                nextMealText.text = "Next Meal: Noon";
            }
            else
            {
                nextMealText.text = "Next Meal: Tomorrow";
            }
        }
    }

    private void OnGUI()
    {
        if (!showUI || DayNightManager.Instance == null) return;

        // Simple fallback UI if TextMeshPro components aren't assigned
        if (dayTimeText == null)
        {
            GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 100));
            GUILayout.Box("Day/Night Cycle", GUILayout.Width(180));

            string timeStr = DayNightManager.Instance.GetFormattedTime();
            int day = DayNightManager.Instance.GetCurrentDay();

            GUILayout.Label($"Day: {day}");
            GUILayout.Label($"Time: {timeStr}");

            float timeOfDay = DayNightManager.Instance.GetTimeOfDay();
            GUILayout.Label($"Progress: {(timeOfDay * 100f):F0}%");

            GUILayout.EndArea();
        }
    }
}
