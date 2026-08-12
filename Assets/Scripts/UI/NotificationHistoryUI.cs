using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full notification history log, browsable at any time from PlayerMenu's Notifications
/// tab. Unlike InfoPopupUI's popup (a transient, steppable queue of unread messages),
/// this shows every notification ever pushed for the current save, newest first.
///
/// Setup:
///   - historyPanel     : the panel root this Open()/Close() toggles
///   - contentContainer : the ScrollRect's Content RectTransform rows are instantiated into
///   - entryPrefab      : row prefab carrying a NotificationHistoryEntryUI component
/// </summary>
public class NotificationHistoryUI : MonoBehaviour
{
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject entryPrefab;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    public void Open()
    {
        if (historyPanel != null) historyPanel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (historyPanel != null) historyPanel.SetActive(false);
    }

    private void Refresh()
    {
        foreach (var go in _spawned)
            Destroy(go);
        _spawned.Clear();

        if (InfoPopupUI.Instance == null || entryPrefab == null || contentContainer == null) return;

        // InfoPopupUI.History is already newest-first.
        foreach (var record in InfoPopupUI.Instance.History)
        {
            GameObject row = Instantiate(entryPrefab, contentContainer);
            row.GetComponent<NotificationHistoryEntryUI>()?.Setup(record.title, record.message);
            _spawned.Add(row);
        }
    }
}
