using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    
    private Dictionary<ResourceType, float> resources = new Dictionary<ResourceType, float>();
    
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
        resources[ResourceType.Wood] = 50f;
        resources[ResourceType.Stone] = 30f;
        resources[ResourceType.Wheat] = 20f;
        resources[ResourceType.Fish] = 1f;
        resources[ResourceType.Iron] = 10f;
    }
    
    public void AddResource(ResourceType type, float amount)
    {
        resources[type] += amount;
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
}
