using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Clickable ship that opens the raid UI when clicked.
/// Attach this to your ship GameObject along with a Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShipClickable : WorldInteractable, IClickable
{
    [Header("Settings")]
    [SerializeField] private int clickPriority = 10; // Higher than buildings
    [SerializeField] private bool canClickDuringRaid = false;

    [Header("UI Reference")]
    [SerializeField] private RaidUI raidUIPanel; // Assign your raid UI panel here

    private Collider2D shipCollider;

    // IClickable implementation
    public Collider2D Collider => shipCollider;
    public int ClickPriority => clickPriority;
    public bool IsClickable => CanBeClicked();

    private void Awake()
    {
        shipCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // Register with MouseInputController (handles timing issues)
        MouseInputController.RegisterClickable(this);
    }

    private void OnEnable()
    {
        // Re-register when enabled
        MouseInputController.RegisterClickable(this);
    }

    private void OnDisable()
    {
        // Unregister when disabled
        MouseInputController.UnregisterClickable(this);
    }

    private void OnDestroy()
    {
        MouseInputController.UnregisterClickable(this);
    }

    public override void Interact(WorldInteractionZone zone = null)
    {
        OpenRaidUI();
        PlayerController.Instance?.SetInputEnabled(false);
    }

    public override void Deselect(WorldInteractionZone zone = null)
    {
        raidUIPanel.CloseRaidUI();
        PlayerController.Instance?.SetInputEnabled(true);
    }

    public override void FocusPanel()
    {
        UIFocus.Set(raidUIPanel.raidPartyUI?.startRaidButton.gameObject);
    }

    private bool CanBeClicked()
    {
        // Don't allow clicking if already on a raid (unless explicitly allowed)
        if (!canClickDuringRaid && RaidManager.Instance != null && RaidManager.Instance.IsOnRaid)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Called by MouseInputController when ship is clicked
    /// </summary>
    public void OnClicked()
    {
        OpenRaidUI();
    }

    /// <summary>
    /// Opens the raid UI panel
    /// </summary>
    public void OpenRaidUI()
    {
        //Debug.Log("Ship clicked - opening raid UI");

        if (raidUIPanel != null)
        {
            raidUIPanel.OpenRaidUI();
        }
        else
        {
            //Debug.LogWarning("No raid UI panel assigned to ShipClickable!");
        }
    }
}
