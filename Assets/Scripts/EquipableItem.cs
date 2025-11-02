using UnityEngine;

public class EquipableItem : MonoBehaviour
{
    public enum ItemType
    {
        Sword,
        Spear,
        Axe,
        Shield,
        Armor,
        Accessory
    }

    [Header("Item Info")]
    public ItemType itemType;
    public string itemName;
    public int damage;
}
