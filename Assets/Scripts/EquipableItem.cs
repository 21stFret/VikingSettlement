using UnityEngine;

public class EquipableItem : MonoBehaviour
{
    public enum ItemType
    {
        Sword,
        Spear,
        Axe,
        Hammer,
        Shield,
        Armor,
        Accessory
    }

    [Header("Item Info")]
    public ItemType itemType;
    public string itemName;
    public int strength;
    public ItemAttachment.AttachmentPoint attachPoint;
    public bool isEquipped = false;

    [Header("Combat")]
    [Tooltip("Time between attacks in seconds. Lower = faster attacks.")]
    public float attackSpeed = 0.5f;
}
