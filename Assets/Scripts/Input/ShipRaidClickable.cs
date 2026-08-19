using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Clickable ship that opens the raid UI when clicked.
/// Attach this to your ship GameObject along with a Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShipRaidClickable : WorldInteractable
{
    [Header("UI Reference")]
    [SerializeField] private RaidResultsUI raidUIPanel; // Assign your raid UI panel here

    public override void Interact(WorldInteractionZone zone = null)
    {
        OpenRaidUI();
        PlayerController.Instance?.SetInputEnabled(false);
        base.Interact(zone);
    }

    public override void Deselect(WorldInteractionZone zone = null)
    {
        CloseRaidUI();
    }
    public void CloseRaidUI()
    {
        if (raidUIPanel != null)
        {
            PlayerController.Instance?.SetInputEnabled(true);
            base.Deselect();
        }
    }

    public override void FocusPanel()
    {
        UIFocus.Set(raidUIPanel.continueRaidButton.gameObject);
    }

    public void OpenRaidUI()
    {
        if (raidUIPanel != null)
        {
            raidUIPanel.ToggleLeavePopup(true);
            raidUIPanel.shipRaidClickable = this;
        }
    }

}
