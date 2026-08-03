using UnityEngine;

public class ItemAttachment : MonoBehaviour
{
    [Header("Attachment Points")]
    public Transform leftHandAttachment;
    public Transform rightHandAttachment;
    public Transform backAttachment;

    [Header("Equipped Items")]
    [SerializeField] public GameObject shield;
    [SerializeField] public GameObject weapon;
    [SerializeField] public GameObject torch;

    [Header("Settings")]
    [SerializeField] private AttachmentPoint shieldAttachPoint = AttachmentPoint.LeftHand;
    [SerializeField] private AttachmentPoint weaponAttachPoint = AttachmentPoint.RightHand;
    [SerializeField] private AttachmentPoint torchAttachPoint = AttachmentPoint.Back;

    private WeaponDatabase WD;

    public enum AttachmentPoint
    {
        LeftHand,
        RightHand,
        Back
    }

    private void Awake()
    {
        WD = WeaponDatabase.Instance;
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
        WD.RemoveItemFromVillageArmory(item.GetComponent<EquipableItem>().itemID);
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
            CC.shield.OnBroken += ShieldDestroyed;
        }
        Villager V = GetComponent<Villager>();
        if(V != null)
        {
            if(V.isJarl)
            {
                AttackCooldownUI.Instance.shieldUi.Init();
            }
        }

    }

    /// <summary>
    /// Detach the shield to the world so it can be picked up. Does nothing if shield is broken.
    /// Called on character death for both allies and enemies.
    /// </summary>
    public void DropShield()
    {
        if (shield == null) return;

        var item = shield.GetComponent<EquipableItem>();
        if (item != null && item.IsBroken) return;

        CharacterBase cc = GetComponent<CharacterBase>();
        if (cc != null && cc.shield != null)
        {
            cc.shield.OnBroken -= ShieldDestroyed;
            cc.isBlocking = false;
            cc.isParrying = false;
            cc.shield.isEquipped = false;
            cc.shield = null;
        }

        shield.transform.SetParent(null);
        shield.tag = "Shield";

        if (shield.GetComponent<Collider2D>() == null)
        {
            var col = shield.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;
        }

        shield = null;
        item?.NotifyUnequipped();
    }

    /// <summary>
    /// Swap the currently equipped shield for one found on the ground: the old shield (if any)
    /// is dropped where the new one was lying, and the new one is equipped in its place.
    /// </summary>
    public void SwapShield(GameObject newShieldGO)
    {
        if (newShieldGO == null) return;

        Vector3 groundPosition = newShieldGO.transform.position;
        GameObject oldShield = shield;

        if (oldShield != null)
        {
            DropShield();
            oldShield.transform.position = groundPosition;
        }

        EquipShield(newShieldGO);
    }

    /// <summary>
    /// Remove and destroy the equipped shield, clearing all related state.
    /// Called automatically when shield durability reaches zero.
    /// </summary>
    public void ShieldDestroyed()
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
        var equipable = newWeapon.GetComponent<EquipableItem>();
        if (equipable != null)
        {
            equipable.isEquipped = true;
        }
        CharacterBase CC = GetComponent<CharacterBase>();
        if (CC != null)
        {
            CC.weapon = equipable;
        }
    }

    public void UnequipWeapon()
    {
        //weapon.SetActive(false);

        var item = weapon.GetComponent<EquipableItem>();
        if (item != null)
        {
            item.isEquipped = false;
        }

        WD.AddItemToVillageArmory(item);
        WD.villageArmoryManager.SpawnArmory();

        CharacterBase CC = GetComponent<CharacterBase>();
        if (CC != null)
        {
            CC.weapon = null;
        }

        if (weapon != null)
        {
            weapon = null;
        }


    }

    /// <summary>
    /// Detach the currently equipped weapon to the world so it can be picked up later.
    /// Mirrors DropShield. Used when swapping for a weapon found on the ground.
    /// </summary>
    public void DropWeapon()
    {
        if (weapon == null) return;

        var item = weapon.GetComponent<EquipableItem>();

        CharacterBase CC = GetComponent<CharacterBase>();
        if (CC != null)
        {
            CC.weapon = null;
        }

        GameObject dropped = weapon;
        weapon = null;

        dropped.transform.SetParent(null);
        dropped.tag = "Weapon";

        if (dropped.GetComponent<Collider2D>() == null)
        {
            var col = dropped.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;
        }

        if (item != null)
        {
            item.isEquipped = false;
            item.NotifyUnequipped();
        }
    }

    /// <summary>
    /// Swap the currently equipped weapon for one found on the ground: the old weapon (if any)
    /// is dropped where the new one was lying, and the new one is equipped in its place.
    /// </summary>
    public void SwapWeapon(GameObject newWeaponGO)
    {
        if (newWeaponGO == null) return;

        Vector3 groundPosition = newWeaponGO.transform.position;
        GameObject oldWeapon = weapon;

        if (oldWeapon != null)
        {
            DropWeapon();
            oldWeapon.transform.position = groundPosition;
        }

        EquipWeapon(newWeaponGO);
    }

    public void TakeWeaponFromArmory()
    {
        EquipableItem armoryWeapon = WD.GetFirstWeaponFromVillageArmory();
        if (armoryWeapon != null)
        {
            string armoryItemID = armoryWeapon.itemID;
            EquipWeapon(WD.villageArmoryManager.GetSpawnedItem(armoryItemID, true));
        }
        else
        {
            print("No weapon found in village armory.");
        }
    }

    public void GiveWeaponByName(string weaponName)
    {
        EquipableItem randomWeapon = WeaponDatabase.Instance.GetWeaponByName(weaponName);
        if (randomWeapon != null)
        {
            GameObject weaponInstance = Instantiate(randomWeapon.gameObject);
            EquipWeapon(weaponInstance);
        }
    }

    public void TakeShieldFromArmory()
    {
        EquipableItem armoryShield = WD.GetFirstShieldFromVillageArmory();
        if (armoryShield != null)
        {
            string armoryItemID = armoryShield.itemID;
            EquipShield(WD.villageArmoryManager.GetSpawnedItem(armoryItemID, true));
        }
        else
        {
            print("No shield found in village armory.");
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

    public void SetDurability(Vector2 values)
    {
        if(weapon != null)
        {
            weapon.GetComponent<EquipableItem>().SetDurability(values.x);
        }
        if(shield != null)
        {
            shield.GetComponent<EquipableItem>().SetDurability(values.y);
        }
    }
}