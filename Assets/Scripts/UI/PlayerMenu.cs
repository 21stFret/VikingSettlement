using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab bar that coordinates the player's overlay panels (calendar, villagers, quests,
/// notifications, skills). Clicking a tab closes whichever panel is currently open and
/// opens the one requested; clicking the active tab again closes it.
///
/// Each panel owns its own show/hide visuals and pause behaviour — this class only calls
/// their public Open()/Close() methods, it doesn't reach into their internals.
/// </summary>
public class PlayerMenu : MonoBehaviour
{
    private enum Tab { None, Calendar, Resources, Villagers, Quests, Notifications, Skills }

    [SerializeField] private GameObject menuPanel;

    [Header("Panels")]
    [SerializeField] private CalendarUI calendarPanel;
    // No dedicated Resources panel yet — ResourceDisplayUI is a passive always-on HUD
    // strip, not a togglable overlay, so there's nothing to Open()/Close() here.
    // See PlayerMenu.OnResourcesTabClick().
    [SerializeField] private VillagerUIManager villagersPanel;
    [SerializeField] private MissionTrackerUI QuestsPanel;
    [SerializeField] private InfoPopupUI notificationsPanel;
    [SerializeField] private SkillTreeUI skillsPanel;

    [Header("Category Tabs")]
    [SerializeField] private Button calendarTabButton;
    [SerializeField] private Button resourcesTabButton;
    [SerializeField] private Button villagersTabButton;
    [SerializeField] private Button QuestsTabButton;
    [SerializeField] private Button notificationsTabButton;
    [SerializeField] private Button skillsTabButton;

    private Tab activeTab = Tab.None;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.TogglePlayerMenu.performed += OnTogglePlayerMenu;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        BindTab(calendarTabButton, Tab.Calendar);
        BindTab(resourcesTabButton, Tab.Resources);
        BindTab(villagersTabButton, Tab.Villagers);
        BindTab(QuestsTabButton, Tab.Quests);
        BindTab(notificationsTabButton, Tab.Notifications);
        BindTab(skillsTabButton, Tab.Skills);

        CloseAllPanels();
        CloseMenu();
    }

    // ── Tab wiring ────────────────────────────────────────────────────────────

    private void BindTab(Button button, Tab tab)
    {
        if (button == null) return;
        button.onClick.AddListener(() => OnTabClicked(tab));
    }

    private void OnTabClicked(Tab tab)
    {
        if (activeTab == tab)
        {
            ClosePanel(activeTab);
            activeTab = Tab.None;
            return;
        }

        if (activeTab != Tab.None)
            ClosePanel(activeTab);

        OpenPanel(tab);
        activeTab = tab;
    }

    private void OpenPanel(Tab tab)
    {
        switch (tab)
        {
            case Tab.Calendar: calendarPanel?.Open(); break;
            case Tab.Resources: OnResourcesTabClick(); break;
            case Tab.Villagers: villagersPanel?.Open(); break;
            case Tab.Quests: QuestsPanel?.Open(); break;
            case Tab.Notifications: notificationsPanel?.Open(); break;
            case Tab.Skills: skillsPanel?.Open(); break;
        }
    }

    private void ClosePanel(Tab tab)
    {
        switch (tab)
        {
            case Tab.Calendar: calendarPanel?.Close(); break;
            case Tab.Resources: break; // nothing to close yet, see OnResourcesTabClick()
            case Tab.Villagers: villagersPanel?.Close(); break;
            case Tab.Quests: QuestsPanel?.Close(); break;
            case Tab.Notifications: notificationsPanel?.Close(); break;
            case Tab.Skills: skillsPanel?.Close(); break;
        }
    }

    private void CloseAllPanels()
    {
        calendarPanel?.Close();
        villagersPanel?.Close();
        QuestsPanel?.Close();
        notificationsPanel?.Close();
        skillsPanel?.Close();
        activeTab = Tab.None;
    }

    private void OnResourcesTabClick()
    {
        // TODO: there's no dedicated Resources panel to open yet — ResourceDisplayUI is
        // just the always-visible top-bar strip. Point this at a real panel once one exists.
        Debug.LogWarning("PlayerMenu: Resources tab has no panel wired up yet.");
    }

    // ── Whole-menu toggle (not yet wired to input — see OnEnable) ───────────────

    public void ToggleMenu()
    {
        if (menuPanel == null) return;
        if (menuPanel.activeSelf) CloseMenu(); else OpenMenu();
    }

    public void OpenMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        CloseAllPanels();
    }

    private void OnTogglePlayerMenu(InputAction.CallbackContext _) => ToggleMenu();
}
