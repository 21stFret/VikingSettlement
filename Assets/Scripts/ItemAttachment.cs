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
    [SerializeField] private GameObject torch;

    [Header("Settings")]
    [SerializeField] private AttachmentPoint shieldAttachPoint = AttachmentPoint.LeftHand;
    [SerializeField] private AttachmentPoint weaponAttachPoint = AttachmentPoint.RightHand;
    [SerializeField] private AttachmentPoint torchAttachPoint = AttachmentPoint.Back;

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

        if (torch != null)
            EquipTorch(torch);
    }

    private void AttachItem(Transform item, AttachmentPoint point)
    {
        Transform attachPoint = GetAttachmentPoint(point);
        if (attachPoint != null)
        {
            item.SetParent(attachPoint);
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
        }
    }

    public void EquipShield(GameObject newShield)
    {
        shield = newShield;
        AttachItem(newShield.transform, shieldAttachPoint);
        CharacterController CC = GetComponent<CharacterController>();
        if (CC != null)
        {
            CC.shield = newShield.GetComponent<EquipableItem>();
            CC.shield.isEquipped = true;
        } 
    }

    public void EquipWeapon(GameObject newWeapon)
    {
        weapon = newWeapon;
        AttachItem(newWeapon.transform, weaponAttachPoint);
        CharacterController CC = GetComponent<CharacterController>();
        if (CC != null)
        {
            CC.weapon = newWeapon.GetComponent<EquipableItem>();
        }
    }

    public void GiveRandomWeapon()
    {
        // This is a placeholder implementation. Replace with actual weapon selection logic.
        EquipableItem randomWeapon = WeaponDatabase.Instance.GetRandomWeapon();
        if (randomWeapon != null)
        {
            GameObject weaponInstance = Instantiate(randomWeapon.gameObject);
            EquipWeapon(weaponInstance);
        }
    }

    public void GiveRandomShield()
    {
        // This is a placeholder implementation. Replace with actual shield selection logic.
        EquipableItem randomShield = WeaponDatabase.Instance.GetRandomShield();
        if (randomShield != null)
        {
            GameObject shieldInstance = Instantiate(randomShield.gameObject);
            EquipShield(shieldInstance);
        }
    }

    public void EquipTorch(GameObject newTorch)
    {
        torch = newTorch;
        AttachItem(newTorch.transform, torchAttachPoint);
        backAttachedItem = newTorch.GetComponent<EquipableItem>();
        if (backAttachedItem != null)
            backAttachedItem.isEquipped = true;
    }

    public void GiveRandomTorch()
    {
        if (WeaponDatabase.Instance == null) return;
        EquipableItem randomTorch = WeaponDatabase.Instance.GetRandomTorch();
        if (randomTorch != null)
        {
            GameObject torchInstance = Instantiate(randomTorch.gameObject);
            EquipTorch(torchInstance);
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