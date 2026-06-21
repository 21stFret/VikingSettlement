using UnityEngine;

/// <summary>
/// Place on the Holmgang stone / challenge post in the scene.
/// Player clicks it to open the opponent selection UI.
///
/// Required scene references:
///   - holmgangUI   : the HolmgangUI panel manager
///
/// Optional:
///   - interactionCollider : defaults to the first Collider2D on this GameObject
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HolmgangInteractable : WorldInteractable, IClickable
{
    [SerializeField] private HolmgangUI holmgangUI;
    [SerializeField] private Collider2D interactionCollider;
    [SerializeField] private int clickPriority = 5;

    // IClickable
    public Collider2D Collider    => interactionCollider;
    public int        ClickPriority => clickPriority;
    public bool       IsClickable   => IsInteractable;

    public override bool IsInteractable =>
        holmgangUI != null
        && !holmgangUI.IsOpen
        && (HolmgangManager.Instance == null || HolmgangManager.Instance.CanChallenge);

    private void Start()
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider2D>();

        MouseInputController.RegisterClickable(this);
    }

    public void OnClicked()
    {
        PlayerController.Instance?.SetInputEnabled(false);
        holmgangUI?.Open();
    }

    public override void Interact()
    {
        PlayerController.Instance?.SetInputEnabled(false);
        holmgangUI?.Open();
    }

    public override void Deselect()
    {
        holmgangUI?.Close();
    }
}
