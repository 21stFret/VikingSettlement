using UnityEngine;

/// <summary>
/// World pickup for a dropped/placed weapon or shield. When the corresponding slot is empty,
/// Villager.OnTriggerEnter2D auto-equips on contact and this never becomes interactable. When
/// the slot is already occupied, this offers a "Pick up X" prompt via WorldInteractionZone —
/// pressing Interact (E) swaps the held item for this one.
/// </summary>
[RequireComponent(typeof(EquipableItem))]
public class EquipablePickup : WorldInteractable
{
    private EquipableItem item;

    private void Awake()
    {
        item = GetComponent<EquipableItem>();
    }

    public override bool IsInteractable => item != null && !item.isEquipped;

    public override string PromptLabel => item != null ? $"Press E to /n Pick up {item.itemName}" : base.PromptLabel;

    public override void Interact(WorldInteractionZone zone = null)
    {
        if (item == null || item.isEquipped) return;

        Villager player = PlayerController.Instance != null ? PlayerController.Instance.GetControlTarget() : null;
        ItemAttachment attachment = player != null ? player.itemAttachment : null;
        if (attachment == null) return;

        if (item.IsShield)
            attachment.SwapShield(gameObject);
        else if (item.IsWeapon)
            attachment.SwapWeapon(gameObject);

        base.Interact(zone);
        Deselect(zone);
    }

    public override void Deselect(WorldInteractionZone zone = null) {base.Deselect(zone);}
}
