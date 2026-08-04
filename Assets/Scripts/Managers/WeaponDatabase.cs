using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] public List<EquipableItem> villageArmory;

    public VillageArmoryManager villageArmoryManager;


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

        // Skip when a save is about to be loaded — LoadSaveData will populate the real
        // armory a couple of frames later. Generating (and spawning) a throwaway armory
        // here first just leaves orphaned duplicate props in the scene.
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
    /// Village armory entries must be independent instances, never the shared references
    /// stored in availableWeapons/availableShields/availableTorches — otherwise two armory
    /// items of the same type would alias the same object and stomp each other's
    /// itemID/durability. Clones are kept inactive and parented here since they're pure data
    /// records; VillageArmoryManager instantiates the actual visible floor props separately.
    /// </summary>
    private EquipableItem CloneItem(EquipableItem template, bool fromLoad = false)
    {
        GameObject clone = Instantiate(template.gameObject, transform);
        clone.SetActive(false);
        EquipableItem item = clone.GetComponent<EquipableItem>();
        item.Init(fromLoad);
        return item;
    }

    public void GenerateInitalArmory()
    {
        int startingSize = 7;
        for (int i = 0; i < startingSize; i++)
        {
            var item = GetRandomWeapon();
            if (item != null) { AddItemToVillageArmory(item); }
        }
        for (int i = 0; i < 2; i++)
        {
            var item = GetRandomShield(i);
            if (item != null) { AddItemToVillageArmory(item); }
        }
        print("Created new village armory as none existed.");
        if(villageArmoryManager != null)
        {
            villageArmoryManager.SpawnArmory();
        }
    }

    public void AddItemToVillageArmory(EquipableItem item)
    {
        if (item == null) return;
        EquipableItem newItem = CloneItem(item);
        villageArmory.Add(newItem);
        print($"Added {newItem.itemName} to the village armory.");
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

    public EquipableItem GetItemFromVillageArmory(string itemName)
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

    public EquipableItem GetFirstShieldFromVillageArmory()
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

    public EquipableItem GetFirstWeaponFromVillageArmory()
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

    public EquipableItem GetItemByID(string itemID)
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
            armorySave.itemDurabilites[i] = item.CurrentDurability;
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

        // villageArmory entries are owned clones (see CloneItem) — destroy the previous
        // batch rather than just dropping the list references, or they'd leak as inactive
        // orphans under WeaponDatabase's transform.
        foreach (var old in villageArmory)
        {
            if (old != null) Destroy(old.gameObject);
        }
        villageArmory.Clear();

        if (armorySave.itemNames != null)
        {
            for (int i = 0; i < armorySave.itemNames.Length; i++)
            {
                var itemName = armorySave.itemNames[i];
                var template = GetItemByName(itemName);
                if (template != null)
                {
                    EquipableItem item = CloneItem(template, fromLoad: true);
                    item.SetDurability(armorySave.itemDurabilites[i]);
                    item.itemID = armorySave.itemIDs[i];
                    villageArmory.Add(item);
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
