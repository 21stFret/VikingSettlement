using UnityEngine;

public class ItemAttachment : MonoBehaviour
{
    [Header("Attachment Points")]
    public Transform leftHandAttachment;
    public Transform rightHandAttachment;
    public Transform backAttachment;

    [Header("Equipped Items")]
    [SerializeField] private GameObject shield;
    [SerializeField] private GameObject weapon;

    [Header("Settings")]
    [SerializeField] private AttachmentPoint shieldAttachPoint = AttachmentPoint.LeftHand;
    [SerializeField] private AttachmentPoint weaponAttachPoint = AttachmentPoint.RightHand;

    public EquipableItem leftHandAttachedItem;
    public EquipableItem rightHandAttachedItem;
    public EquipableItem backAttachedItem;

    public enum AttachmentPoint
    {
        LeftHand,
        RightHand,
        Back
    }
    
    private void Start()
    {
        // Attach items to their points
        if (shield != null)
            EquipShield(shield);
        
        if (weapon != null)
            EquipWeapon(weapon);
    }

    private void AttachItem(Transform item, AttachmentPoint point)
    {
        Transform attachPoint = GetAttachmentPoint(point);
        if (attachPoint != null)
        {
            item.SetParent(attachPoint);
            item.localPosition = Vector3.zero;
        }
    }

    public void EquipShield(GameObject newShield)
    {
        shield = newShield;
        AttachItem(newShield.transform, shieldAttachPoint);
        CharacterController villager = GetComponent<CharacterController>();
        if (villager != null)
        {
            villager.shield = newShield.GetComponent<EquipableItem>();
        } 
    }

    public void EquipWeapon(GameObject newWeapon)
    {
        weapon = newWeapon;
        AttachItem(newWeapon.transform, weaponAttachPoint);
        CharacterController villager = GetComponent<CharacterController>();
        if (villager != null)
        {
            villager.weapon = newWeapon.GetComponent<EquipableItem>();
        }
    }
    
    private Transform GetAttachmentPoint(AttachmentPoint point)
    {
        switch (point)
        {
            case AttachmentPoint.LeftHand: return leftHandAttachment;
            case AttachmentPoint.RightHand: return rightHandAttachment;
            case AttachmentPoint.Back: return backAttachment;
            default: return null;
        }
    }
    
    /// <summary>
    /// Show or hide an item
    /// </summary>
    public void SetItemVisible(SpriteRenderer item, bool visible)
    {
        if (item != null)
            item.enabled = visible;
    }
}