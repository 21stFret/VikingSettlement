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
public class InfoPopupUI : MonoBehaviour
{
    public static InfoPopupUI Instance { get; private set; }

    [Header("Notification Button")]
    [SerializeField] private Button notificationButton;
    [SerializeField] private GameObject badgeObject;
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

    private PlayerInputActions inputActions;

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
        badgeObject.SetActive(false);

        Enqueue(new NotificationEntry("Welcome to Jarlborn", "Lead your settlement, feed your vikings with fish and gather wood survive the harsh winters ahead. Good luck, Jarl.", null));
        Enqueue(new NotificationEntry("Raiding", "If you don't want to stay here and cut trees go raiding from your longship, bring back some loot. Good luck, Jarl.", null));
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

    /// <summary>Queues a notification. Badge will appear if the popup is not already open.</summary>
    public static void Push(string title, string message, Sprite icon = null)
    {
        if (Instance == null) return;
        Instance.Enqueue(new NotificationEntry(title, message, icon));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Enqueue(NotificationEntry entry)
    {
        _queue.Enqueue(entry);
        RefreshBadge();

        // If already open, just update the badge count — player will hit Next
        // If closed, the badge appearing is the prompt to open
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
        badgeObject.SetActive(hasUnread);

        if (badgeCountText != null)
            badgeCountText.text = _queue.Count.ToString();
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
