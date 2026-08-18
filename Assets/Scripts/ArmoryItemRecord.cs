/// <summary>
/// Plain bookkeeping record for one item owned by the village armory: which template it was
/// cloned from, its independent itemID, and its current durability. Deliberately not a
/// MonoBehaviour/GameObject — an armory entry doesn't need a Transform, SpriteRenderer, or any
/// of EquipableItem's visual/behavioral machinery until it's actually spawned as a physical prop
/// (see VillageArmoryManager.SpawnArmory) or equipped on a villager. Matches the shape already
/// used for persistence in ArmorySave.
/// </summary>
[System.Serializable]
public class ArmoryItemRecord
{
    public string itemID;
    public string itemName;
    public EquipableItem.ItemType itemType;
    public float durability;

    public bool IsShield => itemType == EquipableItem.ItemType.Shield;
    public bool IsWeapon => itemType == EquipableItem.ItemType.Sword || itemType == EquipableItem.ItemType.Spear ||
                            itemType == EquipableItem.ItemType.Axe || itemType == EquipableItem.ItemType.Hammer ||
                            itemType == EquipableItem.ItemType.Bow;
}
