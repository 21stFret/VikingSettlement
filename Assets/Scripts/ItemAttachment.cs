using UnityEngine;

public class ItemAttachment : MonoBehaviour
{
    [Header("Attachment Points")]
    public Transform leftHandAttachment;
    public Transform rightHandAttachment;
    public Transform backAttachment;

    [Header("Equipped Items")]
    [SerializeField] private SpriteRenderer shield;
    [SerializeField] private SpriteRenderer weapon;
    
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
            AttachItem(shield.transform, shieldAttachPoint);
        
        if (weapon != null)
            AttachItem(weapon.transform, weaponAttachPoint);
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

    public void EquipShield(SpriteRenderer newShield, AttachmentPoint point)
    {
        shield = newShield;
        shieldAttachPoint = point;
        AttachItem(leftHandAttachment, point);
    }
    
    public void EquipWeapon(SpriteRenderer newWeapon, AttachmentPoint point)
    {
        weapon = newWeapon;
        weaponAttachPoint = point;
        AttachItem(rightHandAttachment, point);
        VillagerController villager = GetComponent<VillagerController>();
        if (villager != null)
        {
            villager.weapon = rightHandAttachedItem;
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