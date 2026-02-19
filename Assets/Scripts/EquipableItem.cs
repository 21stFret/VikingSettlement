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
        Accessory,
        Torch
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

    [Header("Durability")]
    [Tooltip("Max durability. 0 = no durability tracking (unbreakable).")]
    public int maxDurability = 0;
    [SerializeField] private int currentDurability;

    public bool IsShield => itemType == ItemType.Shield;
    public bool IsBroken => maxDurability > 0 && currentDurability <= 0;
    public int CurrentDurability => currentDurability;

    public event System.Action OnBroken;

    private void Awake()
    {
        if (maxDurability > 0)
            currentDurability = maxDurability;
    }

    /// <summary>
    /// Reduce durability by amount. Fires OnBroken when it hits 0.
    /// </summary>
    public void TakeDurabilityDamage(int amount)
    {
        if (maxDurability <= 0 || IsBroken) return;
        currentDurability = Mathf.Max(0, currentDurability - amount);
        if (currentDurability <= 0)
        {
            Debug.Log($"{itemName} has shattered!");
            OnBroken?.Invoke();
        }
    }

    public void RestoreDurability(int amount)
    {
        if (maxDurability <= 0) return;
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
    }
}
