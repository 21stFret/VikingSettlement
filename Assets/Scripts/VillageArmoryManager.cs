using System.Collections.Generic;
using UnityEngine;

public class VillageArmoryManager : MonoBehaviour
{
    private int currentCount;
    private List<GameObject> spawnedItems = new List<GameObject>();

    public void SpawnArmory()
    {
        currentCount = 0;
        //print($"{WeaponDatabase.Instance.villageArmory.Count} items in armory");
        foreach (ArmoryItemRecord record in WeaponDatabase.Instance.villageArmory)
        {
            GameObject existing = spawnedItems.Find(i => i != null && i.GetComponent<EquipableItem>().itemID == record.itemID);
            if(existing != null)
            {
                existing.transform.position = transform.position + new Vector3(currentCount * 1.0f, 0, 0);
                currentCount++;
                continue;
            }

            EquipableItem template = WeaponDatabase.Instance.GetItemByName(record.itemName);
            if (template == null)
            {
                Debug.LogWarning($"VillageArmoryManager: no template found for armory item '{record.itemName}' (id {record.itemID}).");
                continue;
            }

            // Instantiate the physical prop from the template, parented here so it shows up
            // under the armory in the hierarchy. The template asset itself is kept inactive, so
            // the spawned prop must be explicitly reactivated. Init(fromLoad: true) skips the
            // auto-generated itemID/full-durability reset so our explicit values below stick.
            GameObject itemInstance = Instantiate(template.gameObject, transform);
            itemInstance.SetActive(true);
            EquipableItem itemComponent = itemInstance.GetComponent<EquipableItem>();
            itemComponent.Init(true);
            itemComponent.itemID = record.itemID;
            itemComponent.SetDurability(record.durability);
            itemInstance.transform.position = transform.position + new Vector3(currentCount * 1.0f, 0, 0);
            currentCount++;
            spawnedItems.Add(itemInstance);
        }
        //print($"spawned {currentCount} weapons from armory");
    }

    public GameObject GetSpawnedItem(string ID, bool remove = false)
    {
        GameObject GO = spawnedItems.Find(i => i != null && i.GetComponent<EquipableItem>().itemID == ID);
        if(GO !=null)
        {
            if(remove)
            {
                spawnedItems.Remove(GO);
            }
            return GO;
        }
        else
        {
            return null;
        }
    }
}
