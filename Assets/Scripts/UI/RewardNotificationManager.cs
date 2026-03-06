using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a queue of reward notifications, showing up to maxVisible at once.
/// Entries stack in the entryContainer (use a Vertical Layout Group).
///
/// Setup:
///   1. Add this component to a persistent UI GameObject.
///   2. Assign entryPrefab (a prefab with RewardNotificationEntry) and entryContainer.
///   3. Add a Vertical Layout Group to entryContainer.
///
/// Usage:
///   - Resource notifications fire automatically via ResourceManager.OnResourceAdded.
///   - For item pickups call: RewardNotificationManager.Show(sprite, label, amount);
/// </summary>
public class RewardNotificationManager : MonoBehaviour
{
    public static RewardNotificationManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RewardNotificationEntry entryPrefab;
    [SerializeField] private Transform entryContainer;

    [Header("Settings")]
    [Tooltip("How many notifications can be visible at once.")]
    [SerializeField] private int maxVisible = 3;
    [Tooltip("Minimum resource amount to display a notification for.")]
    [SerializeField] private float minimumAmount = 1f;
    [Tooltip("Whether resource gains automatically trigger notifications.")]
    [SerializeField] private bool autoNotifyResources = true;

    [Header("Pool")]
    [SerializeField] private int poolSize = 6;

    private readonly Queue<NotificationData> _queue = new Queue<NotificationData>();
    private readonly Queue<RewardNotificationEntry> _pool = new Queue<RewardNotificationEntry>();
    private int _activeCount;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Prewarm();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()  => ResourceManager.OnResourceAdded += HandleResourceAdded;
    private void OnDisable() => ResourceManager.OnResourceAdded -= HandleResourceAdded;

    private void Prewarm()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var entry = Instantiate(entryPrefab, entryContainer);
            entry.gameObject.SetActive(false);
            _pool.Enqueue(entry);
        }
    }

    private RewardNotificationEntry Rent()
    {
        if (_pool.Count > 0)
        {
            var entry = _pool.Dequeue();
            entry.gameObject.SetActive(true);
            return entry;
        }
        return Instantiate(entryPrefab, entryContainer);
    }

    private void Return(RewardNotificationEntry entry)
    {
        entry.gameObject.SetActive(false);
        // Move to end of container so next shown entry appears at the bottom
        entry.transform.SetAsLastSibling();
        _pool.Enqueue(entry);
        _activeCount--;
        TryShowNext();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Shows a notification with a custom sprite, label, and amount.</summary>
    public static void Show(Sprite icon, string label, float amount)
    {
        if (Instance == null) return;
        Instance.Enqueue(new NotificationData(icon, label, amount));
    }

    /// <summary>Shows a notification for a specific resource type (icon resolved via IconManager).</summary>
    public static void ShowResource(ResourceType type, float amount)
    {
        if (Instance == null) return;
        Sprite icon = IconManager.Instance != null ? IconManager.Instance.GetIconForResource(type) : null;
        string label = type == ResourceType.None ? string.Empty : type.ToString();
        Instance.Enqueue(new NotificationData(icon, label, amount));
    }

    // ── Internal queue ────────────────────────────────────────────────────────

    private void HandleResourceAdded(ResourceType type, float amount)
    {
        if (!autoNotifyResources) return;
        if (type == ResourceType.None) return;
        if (amount < minimumAmount) return;

        Sprite icon = IconManager.Instance != null ? IconManager.Instance.GetIconForResource(type) : null;
        Enqueue(new NotificationData(icon, type.ToString(), amount));
    }

    private void Enqueue(NotificationData data)
    {
        _queue.Enqueue(data);
        TryShowNext();
    }

    private void TryShowNext()
    {
        while (_activeCount < maxVisible && _queue.Count > 0)
        {
            _activeCount++;
            var data = _queue.Dequeue();
            var entry = Rent();
            entry.Setup(data.icon, data.label, data.amount, () => Return(entry));
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    private readonly struct NotificationData
    {
        public readonly Sprite icon;
        public readonly string label;
        public readonly float amount;

        public NotificationData(Sprite icon, string label, float amount)
        {
            this.icon = icon;
            this.label = label;
            this.amount = amount;
        }
    }
}
