using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;

    [Header("Panels")]
    [SerializeField] private CalendarUI calendarPanel;
    //[SerializeField] private Button resourcesTabButton;
    [SerializeField] private VillagerUIManager villagersPanel;
    [SerializeField] private MissionTrackerUI QuestsPanel;
    [SerializeField] private InfoPopupUI notificationsPanel;
    //[SerializeField] private Button skillsTabButton;

    [Header("Category Tabs")]
    [SerializeField] private Button calendarTabButton;
    [SerializeField] private Button resourcesTabButton;
    [SerializeField] private Button villagersTabButton;
    [SerializeField] private Button QuestsTabButton;
    [SerializeField] private Button notificationsTabButton;
    [SerializeField] private Button skillsTabButton;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        //inputActions.Player.TogglePlayerMenu.performed += OnTogglePlayerMenu();
    }

    private void Start()
    {
        if(calendarTabButton!=null)
        {
            calendarTabButton.onClick.AddListener(OnCalendarTabClick);
        }
    }

    private void OnTogglePlayerMenu()
    {

    }

    private void OnCalendarTabClick() 
    {

    }

}
