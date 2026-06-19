using UnityEngine;

/// <summary>
/// Base class for any world object the player can interact with via mouse click or
/// gamepad (WorldInteractionZone). Extend this on BuildingSelector, RaidShip, QuestGiver, etc.
///
/// Minimal contract:
///   Interact()   — open your panel / trigger your behaviour
///   Deselect()   — close/clean up (called on B press or walking away)
///   FocusPanel() — (optional) set UIFocus after the panel opens so gamepad can navigate
///   IsInteractable — gate that prevents interaction when not ready (e.g. ship under construction)
/// </summary>
public abstract class WorldInteractable : MonoBehaviour
{
    public virtual bool IsInteractable => true;

    public abstract void Interact();
    public abstract void Deselect();

    /// <summary>
    /// Called one frame after Interact() by WorldInteractionZone so the same button press
    /// that opens the panel doesn't immediately click the first focused button.
    /// Override and call UIFocus.Set/Push on your panel's first button.
    /// </summary>
    public virtual void FocusPanel() { }
}
