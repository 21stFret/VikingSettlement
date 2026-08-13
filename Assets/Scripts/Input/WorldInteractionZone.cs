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
    public bool interacting;

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
        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponent<WorldInteractable>();
        if (interactable != null)
            nearby.Remove(interactable);
        ShowPrompt();
    }

    public void ShowPrompt()
    {
        if (interacting) return;

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
        if(interacting) return;

        WorldInteractable target = GetNearest();
        if (target == null) return;

        interacting = true;
        promptText.text = "";
        interactionPrompt.SetActive(false);

        active = target;
        target.Interact(this);

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
        active.Deselect(this);
        active = null;
        interacting = false;
        UIFocus.Clear();
        ShowPrompt();
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
