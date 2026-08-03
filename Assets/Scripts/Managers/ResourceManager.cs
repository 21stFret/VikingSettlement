using System;
using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour, ISaveable
{
    public static ResourceManager Instance { get; private set; }

    /// <summary>Fired whenever resources are added. Subscribe for notifications or HUD updates.</summary>
    public static event Action<ResourceType, float> OnResourceAdded;

    private Dictionary<ResourceType, float> resources = new Dictionary<ResourceType, float>();

    public float testWoodAmount = 100f;
    public float testStoneAmount = 50f;
    public float testWheatAmount = 30f;
    public float testFishAmount = 20f;
    public float testIronAmount = 15f;
    public float testGoldAmount = 10f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = 0f;
        }

        // Starting resources
        resources[ResourceType.Wood] = testWoodAmount;
        resources[ResourceType.Stone] = testStoneAmount;
        resources[ResourceType.Wheat] = testWheatAmount;
        resources[ResourceType.Fish] = testFishAmount;
        resources[ResourceType.Iron] = testIronAmount;
        resources[ResourceType.Gold] = testGoldAmount;
    }

    public void AddResource(ResourceType type, float amount)
    {
        resources[type] += amount;
        OnResourceAdded?.Invoke(type, amount);

        if(CompendiumManager.Instance != null)
        {
            CompendiumManager.Instance.Discover("resource_" + type.ToString().ToLower());
        }
    }

    public bool SpendResource(ResourceType type, float amount)
    {
        if (resources[type] >= amount)
        {
            resources[type] -= amount;
            return true;
        }
        return false;
    }

    public float GetResource(ResourceType type)
    {
        return resources[type];
    }

    public bool HasEnoughResources(int wood, int stone, int iron)
    {
        return resources[ResourceType.Wood] >= wood &&
               resources[ResourceType.Stone] >= stone &&
               resources[ResourceType.Iron] >= iron;
    }

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        var resourceTypes = (ResourceType[])System.Enum.GetValues(typeof(ResourceType));
        var saves = new List<ResourceSave>();

        foreach (var type in resourceTypes)
        {
            saves.Add(new ResourceSave
            {
                resourceType = (int)type,
                amount = resources.ContainsKey(type) ? resources[type] : 0f
            });
        }

        data.resources = saves.ToArray();
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.resources == null) return;

        foreach (var saved in data.resources)
        {
            ResourceType type = (ResourceType)saved.resourceType;
            resources[type] = saved.amount;
        }
    }

    #endregion
}
