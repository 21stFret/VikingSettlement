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
            item.gameObject.SetActive(true);
            item.localPosition = Vector3.zero;
            item.localRotation = Quaternion.identity;
        }
    }

    public void EquipShield(GameObject newShield)
    {
        shield = newShield;
        AttachItem(newShield.transform, shieldAttachPoint);
        CharacterBase CC = GetComponent<CharacterBase>();
        if (CC != null)
        {
            CC.shield = newShield.GetComponent<EquipableItem>();
            CC.shield.isEquipped = true;
            CC.shield.OnBroken += UnequipShield;
        }
    }

    /// <summary>
    /// Remove and destroy the equipped shield, clearing all related state.
    /// Called automatically when shield durability reaches zero.
    /// </summary>
    public void UnequipShield()
    {
        CharacterBase CC = GetComponent<CharacterBase>();
        if (CC != null)
        {
            // Detach and play both break effects before the shield GO is destroyed
            if (CC.shield != null)
            {
                PlayDetachedEffect(CC.shield.sheildSparkEffect);
                PlayDetachedEffect(CC.shield.shatterEffect);
            }

            CC.isBlocking = false;
            CC.isParrying = false;
            CC.shield = null;
        }

        if (shield != null)
        {
            Destroy(shield);
            shield = null;
        }
    }

    public void EquipWeapon(GameObject newWeapon)
    {
        weapon = newWeapon;
        AttachItem(newWeapon.transform, weaponAttachPoint);
        CharacterBase CC = GetComponent<CharacterBase>();
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

    public void GiveWeaponByName(string weaponName)
    {
        // This is a placeholder implementation. Replace with actual weapon selection logic.
        EquipableItem randomWeapon = WeaponDatabase.Instance.GetWeaponByName(weaponName);
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

    public void GiveShieldByName(string weaponName)
    {
        // This is a placeholder implementation. Replace with actual weapon selection logic.
        EquipableItem randomWeapon = WeaponDatabase.Instance.GetShieldByName(weaponName);
        if (randomWeapon != null)
        {
            GameObject weaponInstance = Instantiate(randomWeapon.gameObject);
            EquipShield(weaponInstance);
        }
    }

    public void EquipTorch(GameObject newTorch)
    {
        torch = newTorch;
        AttachItem(newTorch.transform, torchAttachPoint);
        CharacterBase CC = GetComponent<CharacterBase>();
        if (CC != null)
        {
            CC.torch = newTorch.GetComponent<EquipableItem>();
            if (CC.torch != null)
                CC.torch.isEquipped = true;
        }
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
    
    private static void PlayDetachedEffect(ParticleSystem fx)
    {
        if (fx == null) return;
        fx.transform.SetParent(null);
        fx.Play();
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
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