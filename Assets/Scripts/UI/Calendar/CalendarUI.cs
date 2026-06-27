using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Collections.Generic;

/// <summary>
/// Parent controller for the 30-day calendar grid.
/// Subscribes to CalendarManager.OnCalendarUpdated and pushes data to each DayEntryUI cell.
/// Toggle: C on keyboard, D-pad Left on gamepad (mapped to Player.ToggleCalendar action).
/// </summary>
public class CalendarUI : MonoBehaviour
{
    [Header("Day Entries — assign all 30 in order, index 0 = today")]
    [SerializeField] private List<DayEntryUI> dayEntries = new List<DayEntryUI>();

    [Header("Panel")]
    [Tooltip("Root GameObject of the full calendar panel. Toggled by Open/Close.")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject rootParentDayUI;

    [SerializeField] private Button closeBtn;

    // ─────────────────────────────────────────────────────────────────────────

    private PlayerInputActions _inputActions;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        dayEntries.AddRange(rootParentDayUI.GetComponentsInChildren<DayEntryUI>());
    }

    private void OnEnable()
    {
        _inputActions.Enable();
        _inputActions.Player.ToggleCalendar.performed += OnToggleCalendar;

        if (closeBtn != null)
            closeBtn.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        _inputActions.Player.ToggleCalendar.performed -= OnToggleCalendar;
        _inputActions.Disable();

        if (closeBtn != null)
            closeBtn.onClick.RemoveListener(Close);

        // Calendar data subscriptions are NOT unsubscribed here — they must survive
        // panel hide/show cycles (Close sets rootPanel inactive which triggers OnDisable).
        // They are removed only in OnDestroy.
    }

    private void OnDestroy()
    {
        if (CalendarManager.Instance != null)
            CalendarManager.Instance.OnCalendarUpdated -= RefreshAll;
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay -= RefreshAll;
    }

    private void Start()
    {
        if (CalendarManager.Instance != null)
            CalendarManager.Instance.OnCalendarUpdated += RefreshAll;
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay += RefreshAll;

        Close();
        // RefreshAll() is not called here — CalendarManager.Initialize() fires
        // OnCalendarUpdated from inside GSB.Init() (2+ frames after Start), so
        // the subscription above will catch it with valid data.
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnToggleCalendar(InputAction.CallbackContext _) => TogglePanel();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Repopulate all 30 cells from current CalendarManager data.
    /// Called automatically on CalendarManager.OnCalendarUpdated.
    /// </summary>
    public void RefreshAll()
    {
        if (CalendarManager.Instance == null) return;

        CalendarDayData[] days = CalendarManager.Instance.Days;

        for (int i = 0; i < dayEntries.Count; i++)
        {
            if (dayEntries[i] == null) continue;
            if (i >= days.Length)      break;

            dayEntries[i].Populate(days[i], isToday: i == 0);
        }
    }

    public void Open()
    {
        RefreshAll();
        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void Close()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void TogglePanel()
    {
        if (rootPanel == null) return;
        if (rootPanel.activeSelf) Close(); else Open();
    }
}
