using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// General-purpose info notification popup.
///
/// A persistent button sits in the corner. A red badge appears when there are
/// unread messages. Clicking opens a popup showing the current message.
/// Multiple messages queue up — the player steps through them one at a time.
///
/// Usage (from anywhere):
///   InfoPopupUI.Push("Season Change", "Winter is here. Move workers to the Fishing Hut.");
///
/// Setup:
///   - notificationButton  : the always-visible corner button
///   - badgeObject         : red dot/circle that shows when there are unread messages
///   - badgeCountText      : (optional) TMP text on the badge showing queue count
///   - popupPanel          : the panel that slides in when the button is clicked
///   - titleText           : TMP title inside the popup
///   - bodyText            : TMP body text inside the popup
///   - iconImage           : (optional) icon inside the popup
///   - nextButton          : "Next" / "OK" button inside the popup
/// </summary>
public class InfoPopupUI : MonoBehaviour, ISaveable
{
    public static InfoPopupUI Instance { get; private set; }

    [Header("Notification Button")]
    [SerializeField] private Button notificationButton;
    [SerializeField] private TextMeshProUGUI badgeCountText;

    [Header("Popup Panel")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonLabel;

    [Header("Animation")]
    [SerializeField] private float popupScaleDuration = 0.2f;
    [SerializeField] private Ease popupEase = Ease.OutBack;

    private readonly Queue<NotificationEntry> _queue = new Queue<NotificationEntry>();
    private bool _isOpen;

    // ── Persisted history ────────────────────────────────────────────────────
    // Every pushed notification (whether or not it carries a dedupe id) is kept
    // here newest-first, saved via ISaveable so the player can browse it later
    // from PlayerMenu's Notifications tab (see NotificationHistoryUI).
    private readonly List<NotificationRecord> _history = new List<NotificationRecord>();
    private readonly HashSet<string> _historyIds = new HashSet<string>();

    /// <summary>Full notification history, newest first. Read-only for UI consumers.</summary>
    public IReadOnlyList<NotificationRecord> History => _history;

    private PlayerInputActions inputActions;

    public const string CyanHex = "#00FFFF";
    public const string GoldHex = "#E6B800";
    public const string PurpleHex = "#B24BF3";


    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        inputActions = new PlayerInputActions();

    }

    private void Start()
    {
        notificationButton.onClick.AddListener(OnNotificationButtonClicked);
        nextButton.onClick.AddListener(OnNextClicked);

        popupPanel.SetActive(false);
        notificationButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Pushes the one-time intro/tutorial messages. Each carries a dedupe id, so once a
    /// save file has seen them they will never queue again on a later load — call this
    /// only after save data has been applied (or confirmed to be a fresh game), see
    /// GameManager.InitializeGameAfterDelay.
    /// </summary>
    public void TryShowIntroSequence()
    {
        Push("Welcome to Jarlborn!", "Repair your settlement, keep your vikings alive and happy, gather resources to improve and get stronger! Prepare for the harsh winters ahead. All Father be with you Jarl!", null, "intro_welcome");
        Push("Meal Time", $"At <color={PurpleHex}>Noon</color> every day you must  <color={CyanHex}>feed your villagers</color>. You can see the food cost in the top right. Food is taken from <color={GoldHex}>Fish, Meat & Bread.</color>", null, "intro_mealtime");
        Push("Fire Time", $"At <color={PurpleHex}>Midnight</color> every night the  <color={CyanHex}>Fire</color> must be restocked with <color={GoldHex}>Wood</color> for the coming day. The <color={CyanHex}>Weather & Population</color> change the amount needed. ", null, "intro_firetime");
        Push("Raiding", $"Once you are better equipped and know your village you should go <color={CyanHex}>Raiding</color>. Here you can earn <color={GoldHex}>Gold</color> to upgrade your village further.", null, "intro_raiding");
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Notifications.performed += GamepadInput;
    }

    private void OnDisable()
    {
        inputActions.Player.Notifications.performed -= GamepadInput;
        inputActions.Disable();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Queues a notification. Badge will appear if the popup is not already open.
    /// Every push is also recorded to the persisted history (see <see cref="History"/>).
    /// Pass <paramref name="id"/> to make this a one-time notification: if a history entry
    /// with the same id already exists (this session or from a loaded save), the call is a
    /// silent no-op — no popup, no duplicate history entry. Leave it null for notifications
    /// that are allowed to repeat (e.g. recurring warnings).
    /// </summary>
    public static void Push(string title, string message, Sprite icon = null, string id = null)
    {
        if (Instance == null) return;
        if (!string.IsNullOrEmpty(id) && Instance._historyIds.Contains(id)) return;

        Instance.Enqueue(new NotificationEntry(title, message, icon));
        Instance.AddToHistory(id, title, message);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Enqueue(NotificationEntry entry)
    {
        _queue.Enqueue(entry);
        RefreshBadge();

        // If already open, just update the badge count — player will hit Next
        // If closed, the badge appearing is the prompt to open
    }

    private void AddToHistory(string id, string title, string message)
    {
        //_history.Insert(0, new NotificationRecord(id, title, message)); // newest-first
        _history.Add(new NotificationRecord(id, title, message));
        if (!string.IsNullOrEmpty(id))
            _historyIds.Add(id);
    }

    public void GamepadInput(InputAction.CallbackContext ctx)
    {
        if(ctx.performed)
        {
            OnNotificationButtonClicked();
        }
    }

    private void OnNotificationButtonClicked()
    {
        if (_isOpen)
        {
            Close();
        }
        else if (_queue.Count > 0)
        {
            Open();
        }
    }

    // ── Public API (for PlayerMenu's Notifications tab) ──────────────────────

    /// <summary>Opens the popup for the current queued message, if any. No-op when the queue is empty.</summary>
    public void Open()
    {
        if (_isOpen || _queue.Count == 0) return;

        _isOpen = true;
        ShowCurrentEntry();

        popupPanel.SetActive(true);
        popupPanel.transform.localScale = Vector3.zero;
        popupPanel.transform.DOScale(Vector3.one, popupScaleDuration).SetEase(popupEase);
        PlayerController.Instance?.SetInputEnabled(false);
    }

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        popupPanel.transform.DOScale(Vector3.zero, popupScaleDuration * 0.8f)
            .SetEase(Ease.InBack)
            .OnComplete(() => popupPanel.SetActive(false));

        UIFocus.Clear();
        PlayerController.Instance?.SetInputEnabled(true);
    }

    private void ShowCurrentEntry()
    {
        if (_queue.Count == 0) return;

        var entry = _queue.Peek();

        titleText.text = entry.title;
        bodyText.text  = entry.message;

        if (iconImage != null)
        {
            iconImage.sprite  = entry.icon;
            iconImage.enabled = entry.icon != null;
        }

        // Label the button based on whether more messages follow
        if (nextButtonLabel != null)
            nextButtonLabel.text = _queue.Count > 1 ? $"Next" : "OK";

        UIFocus.Set(nextButton.gameObject);

        RefreshBadge();
    }

    private void OnNextClicked()
    {
        if (_queue.Count > 0)
            _queue.Dequeue();

        if (_queue.Count > 0)
        {
            // More to show — update in place
            ShowCurrentEntry();
        }
        else
        {
            // All read — close
            Close();
            RefreshBadge();
        }
    }

    private void RefreshBadge()
    {
        bool hasUnread = _queue.Count > 0;
        notificationButton.gameObject.SetActive(hasUnread);

        if (badgeCountText != null)
            badgeCountText.text = _queue.Count.ToString();
    }

    // ── ISaveable ─────────────────────────────────────────────────────────────

    public void PopulateSaveData(SaveData data)
    {
        var entries = new List<NotificationRecordSave>(_history.Count);
        foreach (var record in _history)
            entries.Add(new NotificationRecordSave { id = record.id, title = record.title, message = record.message });

        data.notificationHistoryData = new NotificationHistorySaveData { entries = entries };
    }

    public void LoadSaveData(SaveData data)
    {
        _history.Clear();
        _historyIds.Clear();

        if (data.notificationHistoryData?.entries == null) return;

        foreach (var entry in data.notificationHistoryData.entries)
        {
            _history.Add(new NotificationRecord(entry.id, entry.title, entry.message));
            if (!string.IsNullOrEmpty(entry.id))
                _historyIds.Add(entry.id);
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    private readonly struct NotificationEntry
    {
        public readonly string title;
        public readonly string message;
        public readonly Sprite icon;

        public NotificationEntry(string title, string message, Sprite icon)
        {
            this.title   = title;
            this.message = message;
            this.icon    = icon;
        }
    }
}

/// <summary>A single persisted notification history entry (title + message only, no icon).</summary>
public readonly struct NotificationRecord
{
    public readonly string id;
    public readonly string title;
    public readonly string message;

    public NotificationRecord(string id, string title, string message)
    {
        this.id      = id;
        this.title   = title;
        this.message = message;
    }
}
