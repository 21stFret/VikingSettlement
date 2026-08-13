using TMPro;
using UnityEngine;

/// <summary>
/// Row controller for a single entry in the notification history list. Attach to the
/// root of the row prefab used by <see cref="NotificationHistoryUI"/>.
/// </summary>
public class NotificationHistoryEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;

    public void Setup(string title, string message)
    {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
    }
}
