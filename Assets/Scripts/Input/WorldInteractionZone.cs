using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the player/Jarl. Any WorldInteractable (building, raid ship, quest giver, …)
/// that overlaps this trigger can be opened with Interact (A / E) and closed with
/// ClosePanel (B / F). Requires a CircleCollider2D set as trigger — set its radius
/// in the Inspector to control interaction range.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class WorldInteractionZone : MonoBehaviour
{
    private readonly List<WorldInteractable> nearby = new List<WorldInteractable>();
    private WorldInteractable active;
    private PlayerInputActions inputActions;

    public GameObject interactionPrompt;

    [Tooltip("Optional — shows the nearest interactable's prompt label (e.g. \"Pick up Rare Sword\"). Leave unassigned to just show the icon.")]
    public TMP_Text promptText;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        GetComponent<CircleCollider2D>().isTrigger = true;
        interactionPrompt.SetActive(false);
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Interact.performed   += OnInteract;
        inputActions.Player.ClosePanel.performed += OnClosePanel;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed   -= OnInteract;
        inputActions.Player.ClosePanel.performed -= OnClosePanel;
        inputActions.Disable();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponent<WorldInteractable>();
        if (interactable != null && !nearby.Contains(interactable))
            nearby.Add(interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponent<WorldInteractable>();
        if (interactable != null)
            nearby.Remove(interactable);
    }

    // Re-evaluated every frame (rather than only on enter/exit) because an item's
    // IsInteractable can flip — e.g. a weapon pickup becomes non-interactable the instant it's
    // auto-equipped, without ever firing a trigger-exit since it stays parented to the hand.
    private void Update()
    {
        WorldInteractable nearest = GetNearest();

        interactionPrompt.SetActive(nearest != null);
        if (nearest != null && promptText != null)
            promptText.text = nearest.PromptLabel;
        else promptText.text = "";
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed) return;
        if (PlayerController.Instance != null && !PlayerController.Instance.IsInputEnabled()) return;

        WorldInteractable target = GetNearest();
        if (target == null) return;

        active = target;
        target.Interact();

        // Delay focus by one frame so the same A press doesn't immediately click
        // the first button that gets focused (EventSystem Submit fires same frame).
        StartCoroutine(FocusNextFrame());
    }

    private IEnumerator FocusNextFrame()
    {
        yield return null;
        active?.FocusPanel();
    }

    private void OnClosePanel(InputAction.CallbackContext ctx)
    {
        if (active == null) return;
        active.Deselect();
        active = null;
        UIFocus.Clear();
    }

    private WorldInteractable GetNearest()
    {
        nearby.RemoveAll(i => i == null);

        WorldInteractable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var i in nearby)
        {
            if (!i.IsInteractable) continue;
            float dist = Vector2.SqrMagnitude((Vector2)transform.position - (Vector2)i.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }
}
