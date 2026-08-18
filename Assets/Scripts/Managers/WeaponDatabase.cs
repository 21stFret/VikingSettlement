using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

public class WeaponDatabase : MonoBehaviour, ISaveable
{
    public static WeaponDatabase Instance { get; private set; }

    [Header("Weapons")]
    [SerializeField] private EquipableItem[] availableWeapons;

    [Header("Shields")]
    [SerializeField] private EquipableItem[] availableShields;

    [Header("Torches")]
    [SerializeField] private EquipableItem[] availableTorches;

    [Header("Village Armory")]
    public List<ArmoryItemRecord> villageArmory = new List<ArmoryItemRecord>();

    public VillageArmoryManager villageArmoryManager;

    public int startingWeaponsAmount;
    public int startingShieldsAmount;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Init()
    {
        bool willLoadSave = GameManager.Instance != null && GameManager.Instance.ShouldLoadSave;
        if (!willLoadSave)
            GenerateInitalArmory();
    }

    /// <summary>
    /// Get a random weapon from the database
    /// </summary>
    /// <returns>A random EquipableItem weapon</returns>
    public EquipableItem GetRandomWeapon()
    {
        if (availableWeapons.Length == 0)
            return null;
        // not the last 2 as they are dragur weapons
        int index = Random.Range(0, availableWeapons.Length -2);
        return availableWeapons[index];
    }

    /// <summary>
    /// Get a random shield from the database
    /// </summary>
    /// <returns>A random EquipableItem shield</returns>
    public EquipableItem GetRandomShield(int i  = -1)
    {
        if (availableShields.Length == 0)
            return null;

        int index = Random.Range(0, availableShields.Length);
        if (i != -1)
        {
            return availableShields[i];
        }
        return availableShields[index];
    }

    /// <summary>
    /// Get a weapon by name
    /// </summary>
    public EquipableItem GetWeaponByName(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return null;

        foreach (var weapon in availableWeapons)
        {
            if (weapon != null && weapon.itemName == weaponName)
            {
                return weapon;
            }
        }
        return null;
    }

    /// <summary>
    /// Get a shield by name
    /// </summary>
    public EquipableItem GetShieldByName(string shieldName)
    {
        if (string.IsNullOrEmpty(shieldName)) return null;

        foreach (var shield in availableShields)
        {
            if (shield != null && shield.itemName == shieldName)
            {
                return shield;
            }
        }
        return null;
    }

    /// <summary>
    /// Get a random torch from the database
    /// </summary>
    public EquipableItem GetRandomTorch()
    {
        if (availableTorches == null || availableTorches.Length == 0) return null;
        return availableTorches[Random.Range(0, availableTorches.Length)];
    }

    /// <summary>
    /// Get a torch by name
    /// </summary>
    public EquipableItem GetTorchByName(string torchName)
    {
        if (string.IsNullOrEmpty(torchName) || availableTorches == null) return null;

        foreach (var torch in availableTorches)
        {
            if (torch != null && torch.itemName == torchName)
            {
                return torch;
            }
        }
        return null;
    }

    public EquipableItem GetItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        foreach (var weapon in availableWeapons)
        {
            if (weapon != null && weapon.itemName == itemName)
            {
                return weapon;
            }
        }

        foreach (var shield in availableShields)
        {
            if (shield != null && shield.itemName == itemName)
            {
                return shield;
            }
        }

        foreach (var torch in availableTorches)
        {
            if (torch != null && torch.itemName == itemName)
            {
                return torch;
            }
        }

        return null;
    }

    #region Village Armory Management

    /// <summary>
    /// Village armory entries must be independent records, never the shared references stored
    /// in availableWeapons/availableShields/availableTorches — otherwise two armory items of
    /// the same type would alias the same template and stomp each other's itemID/durability.
    /// This only builds the data record; VillageArmoryManager instantiates the actual visible
    /// floor prop from the template separately (see SpawnArmory).
    /// </summary>
    private ArmoryItemRecord CreateArmoryRecord(EquipableItem template)
    {
        return new ArmoryItemRecord
        {
            itemID = System.Guid.NewGuid().ToString(),
            itemName = template.itemName,
            itemType = template.itemType,
            durability = template.maxDurability
        };
    }

    public void GenerateInitalArmory()
    {
        for (int i = 0; i < startingWeaponsAmount; i++)
        {
            //var item = GetRandomWeapon();
            var item = GetWeaponByName("Iron_Sword");
            if (item != null) { AddItemToVillageArmory(item, Random.Range(6, 8)); }
        }

        for (int i = 0; i < startingShieldsAmount; i++)
        {
            var item = GetRandomShield(i);
            if (item != null) { AddItemToVillageArmory(item); }
        }
    }

    /// <summary>
    /// Adds a new item to the armory. Pass durabilityOverride to seed a starting durability
    /// other than the template's max (e.g. a worn starting weapon) — it must be applied before
    /// SpawnArmory() runs below, since SpawnArmory only sets durability on props it's creating
    /// for the first time and won't revisit this record once it's already spawned.
    /// </summary>
    public ArmoryItemRecord AddItemToVillageArmory(EquipableItem item, float? durabilityOverride = null)
    {
        if (item == null) return null;
        ArmoryItemRecord newItem = CreateArmoryRecord(item);
        if (durabilityOverride.HasValue)
            newItem.durability = durabilityOverride.Value;
        villageArmory.Add(newItem);
        print($"Added {newItem.itemName} to the village armory.");
        if(item.IsShield)
        {
           ResourceManager.Instance.AddResource(ResourceType.Shields, 1f);
        }
        if (item.IsWeapon)
        {
            ResourceManager.Instance.AddResource(ResourceType.Weapons, 1f);
        }
        if (villageArmoryManager != null)
            villageArmoryManager.SpawnArmory();
        return newItem;
    }

    public void AddItemsToVillageArmory(EquipableItem item, float amount)
    {
        for(int i = 0; i < amount; i++)
        {
            AddItemToVillageArmory(item);
        }
    }

    /// <summary>
    /// Remove an item from the armory list by its itemID. The physical prop in the scene is
    /// left in place — it stays visible and can still be picked up by anyone until the next
    /// time the armory is spawned from a fresh load.
    /// </summary>
    public void RemoveItemFromVillageArmory(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        var match = villageArmory.Find(i => i != null && i.itemID == itemID);
        if (match != null)
        {
            villageArmory.Remove(match);
            print($"Removed {match.itemName} from the village armory.");
        }
        else
        {
            print($"Item with id {itemID} not found in the village armory.");
        }
    }

    public ArmoryItemRecord GetItemFromVillageArmory(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        foreach (var item in villageArmory)
        {
            if (item != null && item.itemName == itemName)
            {
                return item;
            }
        }
        return null;
    }

    public ArmoryItemRecord GetFirstShieldFromVillageArmory()
    {
        foreach (var item in villageArmory)
        {
            if (item != null && item.IsShield)
            {
                return item;
            }
        }
        return null;
    }

    public ArmoryItemRecord GetFirstWeaponFromVillageArmory()
    {
        foreach (var item in villageArmory)
        {
            if (item != null && item.IsWeapon)
            {
                return item;
            }
        }
        return null;
    }

    public ArmoryItemRecord GetItemByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;
        foreach (var item in villageArmory)
        {
            if (item != null && item.itemID == itemID)
            {
                return item;
            }
        }
        return null;
    }

    #endregion

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        var armorySave = new ArmorySave();

        // Save armory items
        armorySave.itemNames = new string[villageArmory.Count];
        armorySave.itemDurabilites = new float[villageArmory.Count];
        armorySave.itemIDs = new string[villageArmory.Count];
        for (int i = 0; i < villageArmory.Count; i++)
        {
            var item = villageArmory[i];
            armorySave.itemNames[i] = item.itemName;
            armorySave.itemDurabilites[i] = item.durability;
            armorySave.itemIDs[i] = item.itemID;
        }

        data.armory = new ArmorySave[] { armorySave };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.armory == null || data.armory.Length == 0)
        {
            // create base armory if none exists
            var baseArmory = new ArmorySave();
            int startingSize = 5;
            int total = startingSize * 2;
            baseArmory.itemNames = new string[total];
            baseArmory.itemDurabilites = new float[total];
            baseArmory.itemIDs = new string[total];
            for (int i = 0; i < startingSize; i++)
            {
                var item = GetRandomWeapon();
                if (item != null)
                {
                    baseArmory.itemNames[i] = item.itemName;
                    baseArmory.itemDurabilites[i] = item.maxDurability;
                    baseArmory.itemIDs[i] = System.Guid.NewGuid().ToString();
                }
            }
            for (int i = startingSize; i < total; i++)
            {
                var item = GetRandomShield();
                if (item != null)
                {
                    baseArmory.itemNames[i] = item.itemName;
                    baseArmory.itemDurabilites[i] = item.maxDurability;
                    baseArmory.itemIDs[i] = System.Guid.NewGuid().ToString();
                }
            }
            data.armory = new ArmorySave[] { baseArmory };
            print("Created new armory save as none existed.");
        }

        var armorySave = data.armory[0];

        // villageArmory entries are plain records (see ArmoryItemRecord) — just replace the list.
        villageArmory.Clear();

        if (armorySave.itemNames != null)
        {
            for (int i = 0; i < armorySave.itemNames.Length; i++)
            {
                var itemName = armorySave.itemNames[i];
                var template = GetItemByName(itemName);
                if (template != null)
                {
                    villageArmory.Add(new ArmoryItemRecord
                    {
                        itemID = armorySave.itemIDs[i],
                        itemName = template.itemName,
                        itemType = template.itemType,
                        durability = armorySave.itemDurabilites[i]
                    });
                }
            }
            print($"Loaded {villageArmory.Count} items to the armory");
        }

        if (villageArmoryManager != null)
        {
            villageArmoryManager.SpawnArmory();
        }
    }

    #endregion
}
