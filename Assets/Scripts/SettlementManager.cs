using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SettlementManager : MonoBehaviour
{
    public static SettlementManager Instance { get; private set; }
    
    [Header("Buildings")]
    private List<Building> allBuildings = new List<Building>();
    
    [Header("Villagers")]
    private List<Villager> allVillagers = new List<Villager>();
    
    [Header("Population Statistics")]
    [SerializeField] private int youngCount = 0;
    [SerializeField] private int matureCount = 0;
    [SerializeField] private int maleCount = 0;
    [SerializeField] private int femaleCount = 0;
    [SerializeField] private int totalBirths = 0;
    [SerializeField] private int totalDeaths = 0;
    [SerializeField] private float averageAge = 0f;

    [Header("Population Display")]
    [SerializeField] private bool showPopulationUI = true;

    public float ageingInterval = 1f; // How often villagers age (in seconds)
    public float ageingAmount = 0.1f; // How much villagers age each interval
    public float reproductionCooldown = 5f; // Minimum time between having children
    public float reproductionInterval = 0f; // Timer to track reproduction intervals
    public float gameTickInterval = 1f; // General game tick interval
    private float gameTickTimer = 0f;

    [Header("Food Consumption")]
    public float fishPerVillagerPerDay = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to meal time events
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnMealTime += HandleMealTime;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnMealTime -= HandleMealTime;
        }
    }
    
    private void Update()
    {
                // Update all building production (continuous)
        UpdateBuildingProduction(Time.deltaTime);
        gameTickTimer += Time.deltaTime;
        if (gameTickTimer < gameTickInterval) return;


        
        // Update all villagers working
        UpdateVillagers(gameTickTimer);

        // Update population statistics
        UpdatePopulationStats();
        gameTickTimer = 0f; // Reset tick timer
    }
    
    private void UpdateBuildingProduction(float deltaTime)
    {
        foreach (Building building in allBuildings)
        {
            building.UpdateProduction(deltaTime);
        }
    }
    
    private void UpdateVillagers(float deltaTime)
    {
        foreach (Villager villager in allVillagers)
        {
            villager.UpdateLife(deltaTime);
        }
    }
    
    private void UpdatePopulationStats()
    {
        youngCount = 0;
        matureCount = 0;
        maleCount = 0;
        femaleCount = 0;
        float totalAge = 0f;
        
        foreach (var villager in allVillagers)
        {
            // Count life stages
            switch (villager.currentLifeStage)
            {
                case LifeStage.Young:
                    youngCount++;
                    break;
                case LifeStage.Mature:
                    matureCount++;
                    break;
            }
            
            // Count genders
            if (villager.gender == Gender.Male)
                maleCount++;
            else
                femaleCount++;
            
            // Sum ages for average
            totalAge += villager.age;
        }
        
        // Calculate average age
        averageAge = allVillagers.Count > 0 ? totalAge / allVillagers.Count : 0f;
    }
    
    #region Food Consumption

    /// <summary>
    /// Handles meal time event from DayNightManager
    /// </summary>
    private void HandleMealTime()
    {
        if (DayNightManager.Instance == null || ResourceManager.Instance == null)
            return;

        int villagerCount = allVillagers.Count;
        float totalFishNeeded = villagerCount * fishPerVillagerPerDay;

        if (totalFishNeeded <= 0)
        {
            Debug.Log("No villagers to feed.");
            return;
        }

        float availableFish = ResourceManager.Instance.GetResource(ResourceType.Fish);

        if (availableFish >= totalFishNeeded)
        {
            // Enough fish - consume it
            ResourceManager.Instance.SpendResource(ResourceType.Fish, totalFishNeeded);
            Debug.Log($"Fed {villagerCount} villagers ({totalFishNeeded} fish consumed). Remaining fish: {ResourceManager.Instance.GetResource(ResourceType.Fish)}");
            // All villagers are fed
            foreach (var villager in allVillagers)
            {
                villager.HandleHunger(false);
            }
        }
        else
        {
            // Not enough fish - villagers go hungry
            Debug.LogWarning($"Not enough fish! Need {totalFishNeeded} but only have {availableFish}. Villagers are hungry!");

            // Consume what we have
            if (availableFish > 0)
            {
                ResourceManager.Instance.SpendResource(ResourceType.Fish, availableFish);
                int fedVillagers = Mathf.FloorToInt(availableFish / fishPerVillagerPerDay);
                Debug.LogWarning($"Only fed {fedVillagers}/{villagerCount} villagers. {villagerCount - fedVillagers} villagers went hungry!");
            }
            else
            {
                Debug.LogWarning($"No fish available! All {villagerCount} villagers went hungry!");
            }

            // Select random villagers to go hungry
            List<Villager> hungryVillagers = new List<Villager>(allVillagers);
            int villagersToFeed = Mathf.FloorToInt(availableFish / fishPerVillagerPerDay);
            hungryVillagers = hungryVillagers.OrderBy(v => Random.value).ToList(); // Randomize order
            for (int i = 0; i < hungryVillagers.Count; i++)
            {
                if (i >= villagersToFeed)
                {
                    hungryVillagers[i].HandleHunger(true);
                }
                else
                {
                    hungryVillagers[i].HandleHunger(false);
                }
            }
        }
    }

    #endregion

    #region Building Management

    /// <summary>
    /// Register a building with the settlement manager
    /// </summary>
    public void RegisterBuilding(Building building)
    {
        if (!allBuildings.Contains(building))
        {
            allBuildings.Add(building);
        }
    }
    
    /// <summary>
    /// Unregister a building (when destroyed)
    /// </summary>
    public void UnregisterBuilding(Building building)
    {
        allBuildings.Remove(building);
    }
    
    /// <summary>
    /// Get all buildings of a specific type
    /// </summary>
    public List<Building> GetBuildingsOfType(BuildingType type)
    {
        List<Building> buildings = new List<Building>();
        foreach (Building building in allBuildings)
        {
            if (building.data.buildingType == type)
            {
                buildings.Add(building);
            }
        }
        return buildings;
    }
    
    /// <summary>
    /// Get all buildings
    /// </summary>
    public List<Building> GetAllBuildings()
    {
        return new List<Building>(allBuildings);
    }
    
    /// <summary>
    /// Get all building selectors
    /// </summary>
    public List<BuildingSelector> GetAllBuildingSelectors()
    {
        List<BuildingSelector> selectors = new List<BuildingSelector>();
        foreach (Building building in allBuildings)
        {
            BuildingSelector selector = building.GetComponent<BuildingSelector>();
            if (selector != null)
            {
                selectors.Add(selector);
            }
        }
        return selectors;
    }
    
    #endregion
    
    #region Villager Management
    
    /// <summary>
    /// Register a villager with the settlement manager
    /// </summary>
    public void RegisterVillager(Villager villager)
    {
        if (!allVillagers.Contains(villager))
        {
            allVillagers.Add(villager);
            Debug.Log($"Registered villager: {villager.villagerName} (Age: {villager.age:F1}, {villager.gender}). Total population: {allVillagers.Count}");
            
            // Track births (villagers starting at age 0)
            if (villager.age < 0.1f)
            {
                totalBirths++;
            }
        }
    }
    
    /// <summary>
    /// Unregister a villager (when they die)
    /// </summary>
    public void UnregisterVillager(Villager villager)
    {
        if (allVillagers.Contains(villager))
        {
            allVillagers.Remove(villager);
            totalDeaths++;
            Debug.Log($"Unregistered villager: {villager.villagerName} (Age: {villager.age:F1}). Total population: {allVillagers.Count}");
        }
    }
    
    /// <summary>
    /// Get all villagers
    /// </summary>
    public List<Villager> GetAllVillagers()
    {
        return new List<Villager>(allVillagers);
    }
    
    /// <summary>
    /// Get all unemployed villagers (only mature villagers can work)
    /// </summary>
    public List<Villager> GetUnemployedVillagers()
    {
        List<Villager> unemployed = new List<Villager>();
        foreach (Villager villager in allVillagers)
        {
            if (villager.currentJob == JobType.None && villager.currentLifeStage == LifeStage.Mature)
            {
                unemployed.Add(villager);
            }
        }
        return unemployed;
    }
    
    /// <summary>
    /// Get villagers by life stage
    /// </summary>
    public List<Villager> GetVillagersByLifeStage(LifeStage stage)
    {
        List<Villager> result = new List<Villager>();
        foreach (var villager in allVillagers)
        {
            if (villager.currentLifeStage == stage)
            {
                result.Add(villager);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get available workers (mature villagers without jobs)
    /// </summary>
    public List<Villager> GetAvailableWorkers()
    {
        List<Villager> workers = new List<Villager>();
        foreach (var villager in allVillagers)
        {
            if (villager.currentLifeStage == LifeStage.Mature && villager.currentJob == JobType.None)
            {
                workers.Add(villager);
            }
        }
        return workers;
    }
    
    /// <summary>
    /// Get villagers by gender
    /// </summary>
    public List<Villager> GetVillagersByGender(Gender gender)
    {
        List<Villager> result = new List<Villager>();
        foreach (var villager in allVillagers)
        {
            if (villager.gender == gender)
            {
                result.Add(villager);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get total population count
    /// </summary>
    public int GetPopulation()
    {
        return allVillagers.Count;
    }
    
    /// <summary>
    /// Get population statistics
    /// </summary>
    public PopulationStats GetPopulationStats()
    {
        return new PopulationStats
        {
            totalPopulation = allVillagers.Count,
            youngCount = youngCount,
            matureCount = matureCount,
            maleCount = maleCount,
            femaleCount = femaleCount,
            totalBirths = totalBirths,
            totalDeaths = totalDeaths,
            averageAge = averageAge,
            availableWorkers = GetAvailableWorkers().Count
        };
    }
    
    #endregion
    
    #region UI Display
    
    private void OnGUI()
    {
        if (!showPopulationUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 280, 280));
        
        // Title
        GUILayout.Box("Settlement Population", GUILayout.Width(270));
        
        // Population breakdown
        GUILayout.Label($"Total Population: {allVillagers.Count}");
        GUILayout.Label($"  Young: {youngCount}");
        GUILayout.Label($"  Mature: {matureCount}");
        GUILayout.Space(5);
        
        // Gender breakdown
        GUILayout.Label($"Gender:");
        GUILayout.Label($"  Male: {maleCount}");
        GUILayout.Label($"  Female: {femaleCount}");
        GUILayout.Space(5);
        
        // Employment
        GUILayout.Label($"Available Workers: {GetAvailableWorkers().Count}");
        GUILayout.Label($"Employed: {matureCount - GetAvailableWorkers().Count}");
        GUILayout.Space(5);
        
        // Vital statistics
        GUILayout.Label($"Average Age: {averageAge:F1} years");
        GUILayout.Label($"Total Births: {totalBirths}");
        GUILayout.Label($"Total Deaths: {totalDeaths}");
        GUILayout.Space(5);
        
        // Buildings
        GUILayout.Label($"Total Buildings: {allBuildings.Count}");
        
        GUILayout.EndArea();
    }
    
    #endregion
}

/// <summary>
/// Population statistics data structure
/// </summary>
[System.Serializable]
public struct PopulationStats
{
    public int totalPopulation;
    public int youngCount;
    public int matureCount;
    public int maleCount;
    public int femaleCount;
    public int totalBirths;
    public int totalDeaths;
    public float averageAge;
    public int availableWorkers;
}