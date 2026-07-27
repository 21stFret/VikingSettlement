using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class VillageArmoryManager : MonoBehaviour
{
    private int currentCount;
    private List<GameObject> spawnedItems = new List<GameObject>();

    public void SpawnArmory()
    {
        currentCount = 0;
        print($"{WeaponDatabase.Instance.villageArmory.Count} items in armory");
        foreach(EquipableItem item in WeaponDatabase.Instance.villageArmory)
        {
            GameObject existing = spawnedItems.Find(i => i != null && i.GetComponent<EquipableItem>().itemID == item.itemID);
            if(existing != null)
            {
                existing.transform.position = transform.position + new Vector3(currentCount * 2.0f, 0, 0);
                currentCount++;
                continue;
            }

            // Instantiate the item in the scene. The armory's data-record clone (item.gameObject)
            // is kept inactive, so the spawned prop must be explicitly reactivated.
            GameObject itemInstance = Instantiate(item.gameObject);
            itemInstance.SetActive(true);
            itemInstance.GetComponent<EquipableItem>().Init(true);
            itemInstance.transform.position = transform.position + new Vector3(currentCount * 2.0f, 0, 0);
            currentCount++;
            spawnedItems.Add(itemInstance);
        }
        print($"spawned {currentCount} weapons from armory");
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
