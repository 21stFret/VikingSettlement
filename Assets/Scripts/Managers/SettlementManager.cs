using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;

[System.Serializable]
public class DeathRecord
{
    public string name;
    public string uniqueId;
    public float ageAtDeath;
    public DeathCause cause;
    public bool wasJarl;
    public float timeOfDeath;
}

public class SettlementManager : MonoBehaviour, ISaveable
{
    public static SettlementManager Instance { get; private set; }

    [Header("Buildings")]
    private List<Building> allBuildings = new List<Building>();

    [Header("Villagers")]
    private List<Villager> allVillagers = new List<Villager>();
    private List<DeathRecord> _deathLog = new List<DeathRecord>();

    [Header("Population Statistics")]
    [SerializeField] private int youngCount = 0;
    [SerializeField] private int matureCount = 0;
    [SerializeField] private int maleCount = 0;
    [SerializeField] private int femaleCount = 0;
    [SerializeField] private int totalBirths = 0;
    [SerializeField] private int totalDeaths = 0;
    [SerializeField] private float averageAge = 0f;
    public float timeSinceLastBirth;

    [Header("Population Display")]
    [SerializeField] private bool showPopulationUI = true;

    public float ageingInterval = 1f; // How often villagers age (in seconds)
    public float ageingAmount = 0.1f; // How much villagers age each interval
    public float reproductionCooldown = 5f; // Minimum time between having children
    public float reproductionInterval = 0f; // Timer to track reproduction intervals
    private float ageTimer = 0f;
    public float age;
    private float countingAge;

    [Header("Food Consumption")]
    public float foodPerVillagerPerDay = 1f;
    public float totalFoodNeeded = 0f;
    [SerializeField] private HungerDistributionMode hungerMode = HungerDistributionMode.Prioritized;
    [Tooltip("Minimum health lost per starvation tick regardless of current health. Lower this for testing.")]
    [SerializeField] public float minStarvationDamage = 5f;
    public event Action<bool> OnFoodConsumed;

    [Header("Warmth System (Winter)")]
    [Tooltip("Wood consumed per villager per day in winter")]
    public float woodPerVillagerPerDay = 0.5f;
    [Tooltip("Morale penalty per day when settlement is cold")]
    public float coldMoralePenalty = 10f;
    [Tooltip("Morale bonus per day when settlement is warm")]
    public float warmthMoraleBonus = 5f;
    [Tooltip("Health damage per day when settlement is cold (0 to disable)")]
    public float coldHealthDamage = 0f;
    [Tooltip("Current warmth status")]
    [SerializeField] private bool isSettlementWarm = true;
    [Tooltip("Wood consumed today")]
    [SerializeField] private float woodConsumedToday = 0f;
    [Tooltip("Wood needed today")]
    [SerializeField] private float woodNeededToday = 0f;
    public event Action<bool> OnWarmthChanged; // true = warm, false = cold

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

    public void Initialize()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnMealTime += HandleMealTime;
            DayNightManager.Instance.OnDayEnd += HandleNewDay;
        }
        else
            Debug.LogWarning("SettlementManager: DayNightManager not found during Initialize!");

        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnGameTick += OnTick;
            GameTickManager.Instance.OnFastUpdate += FastUpdate;
        }
        else
        {
            Debug.LogWarning("SettlementManager: GameTickManager not found during Initialize!");
        }

        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied += HandleJarlDeath;
            JarlManager.Instance.OnJarlChanged += HandleJarlChanged;
        }
        else
        {
            Debug.LogWarning("SettlementManager: JarlManager not found during Initialize!");
        }
        
        foreach(Building building in allBuildings)
        {
            building.Init();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameTickManager.Exists)
        {
            GameTickManager.Instance.OnGameTick -= OnTick;
            GameTickManager.Instance.OnFastUpdate -= FastUpdate;
        }
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnMealTime -= HandleMealTime;
            DayNightManager.Instance.OnDayEnd -= HandleNewDay;
        }
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied -= HandleJarlDeath;
            JarlManager.Instance.OnJarlChanged -= HandleJarlChanged;
        }
    }

    #region Jarl Events

    /// <summary>
    /// Handle the death of the Jarl - apply morale penalty
    /// </summary>
    private void HandleJarlDeath(Villager deadJarl)
    {
        Debug.Log($"The settlement mourns the death of Jarl {deadJarl.villagerName}...");

        // Apply morale penalty to all villagers
        foreach (var villager in allVillagers)
        {
            if (villager != deadJarl && !villager.IsDead())
            {
                villager.ChangeMorale(-20f);
            }
        }
    }

    /// <summary>
    /// Handle new Jarl selection - apply morale bonus
    /// </summary>
    private void HandleJarlChanged(Villager newJarl)
    {
        Debug.Log($"The settlement celebrates {newJarl.villagerName} as the new Jarl!");

        AttackCooldownUI.Instance.Init();
        DodgeCooldownUI.Instance?.Init();

        // Apply morale bonus for new leader
        foreach (var villager in allVillagers)
        {
            if (!villager.IsDead())
            {
                villager.ChangeMorale(10f);
            }
        }
    }

    #endregion
    
    private void OnTick(float deltaTime)
    {        
        // Update all villagers working
        UpdateVillagers(deltaTime);

        // Update population statistics
        UpdatePopulationStats();

        ageTimer += deltaTime;
        if (ageTimer >= ageingInterval) // Age once per real-time second (adjustable)
        {
            ageTimer = 0f;
            countingAge += ageingAmount;
            age = Mathf.FloorToInt(countingAge);
        }

        timeSinceLastBirth += deltaTime;
    }

    private void FastUpdate()
    {
        float scaledDeltaTime = Time.deltaTime * GameTickManager.Instance.TimeScale;
        UpdateBuildingProduction(scaledDeltaTime);
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
        for (int i = allVillagers.Count - 1; i >= 0; i--)
        {
            allVillagers[i].UpdateLife(deltaTime);
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
    
    #region Wood Consumption

    // Food and wood both consumed here — population resource drain is SettlementManager's responsibility
    private void HandleNewDay()
    {
        ConsumeFirewood();
    }

    private void ConsumeFirewood()
    {
        woodNeededToday = GetTodayWoodCost();
        float woodAvailable = ResourceManager.Instance.GetResource(ResourceType.Wood);
        if (woodNeededToday > woodAvailable)
        {
            ResourceManager.Instance.SpendResource(ResourceType.Wood, woodAvailable);
            woodConsumedToday = woodAvailable;
            ApplyColdEffects();
            SetWarmthStatus(false);
        }
        else
        {
            ResourceManager.Instance.SpendResource(ResourceType.Wood, woodNeededToday);
            woodConsumedToday = woodNeededToday;
            ApplyWarmEffects();
            SetWarmthStatus(true);
        }
    }

    public float GetTodayWoodCost()
    {
        int population = GetPopulation();

        SeasonManager.Season season = SeasonManager.Instance.GetCurrentSeason();

        ColdDayType coldDay = StormScheduler.Instance != null && DayNightManager.Instance != null
            ? StormScheduler.Instance.GetColdDayType(DayNightManager.Instance.CurrentAbsoluteDay)
            : ColdDayType.Chilly;

        // Cold day type and storm are independent — they stack rather than override each other.
        float stormMultiplier = StormScheduler.Instance != null ? StormScheduler.Instance.GetCurrentDayWoodMultiplier() : 1.0f;
        float runestoneMultiplier = RunestoneManager.Instance != null ? RunestoneManager.Instance.GetWoodConsumptionMultiplier() : 1.0f;
        return SettlementFormulas.GetWoodCost(population, woodPerVillagerPerDay, season, coldDay, stormMultiplier, runestoneMultiplier);
    }

    private void ApplyWarmEffects()
    {
        foreach (var villager in GetAllVillagers())
        {
            if (villager == null || villager.IsDead()) continue;
            villager.isCold = false;
            villager.ChangeMorale(warmthMoraleBonus);
        }

        Debug.Log("Settlement is warm. No negative effects applied.");
    }

    private void ApplyColdEffects()
    {
        var villagers = SettlementManager.Instance.GetAllVillagers();

        int villagersAffected = SettlementFormulas.GetColdAffectedCount(villagers.Count, woodNeededToday, woodConsumedToday);

        for (int i = 0; i < villagersAffected; i++)
        {
            var villager = villagers[i];
            if (villager == null || villager.IsDead()) continue;

            if (coldMoralePenalty > 0)
                villager.ChangeMorale(-coldMoralePenalty);

            if (coldHealthDamage > 0)
                villager.TakeDamage(coldHealthDamage, null, true);

            villager.personalUI.ShowSpeech("I'm freezing!", 2.0f);
            villager.isCold = true;
            villager.personalUI.UpdateStatusEffectIcon(VillagerStatusEffect.Cold);
        }

        Debug.Log($"Cold effects applied: -{coldMoralePenalty} morale{(coldHealthDamage > 0 ? $", -{coldHealthDamage} health" : "")} to {villagersAffected} villagers");
    }

    private void SetWarmthStatus(bool isWarm)
    {
        if (isSettlementWarm != isWarm)
        {
            isSettlementWarm = isWarm;
            OnWarmthChanged?.Invoke(isWarm);
        }
    }
    public bool IsSettlementWarm() => isSettlementWarm;

    #endregion

    #region Food Consumption

    /// <summary>
    /// Handles meal time event from DayNightManager
    /// </summary>
    private void HandleMealTime()
    {
        if (DayNightManager.Instance == null || ResourceManager.Instance == null)
            return;

        int villagerCount = allVillagers.Count;
        float rationingModifier = RunestoneManager.Instance != null ? RunestoneManager.Instance.GetFoodConsumptionModifier() : 0f;
        float effectiveFoodPerVillager = SettlementFormulas.GetEffectiveFoodPerVillager(foodPerVillagerPerDay, rationingModifier);

        totalFoodNeeded = SettlementFormulas.GetTotalFoodNeeded(villagerCount, effectiveFoodPerVillager);

        if (totalFoodNeeded <= 0)
        {
            Debug.Log("No villagers to feed.");
            return;
        }

        int availableFish = (int)ResourceManager.Instance.GetResource(ResourceType.Fish);
        int availableMeat = (int)ResourceManager.Instance.GetResource(ResourceType.Meat);
        int availableBread = (int)ResourceManager.Instance.GetResource(ResourceType.Bread);
        int totalFoodAvailable = availableFish + availableMeat + availableBread;

        if (totalFoodAvailable >= totalFoodNeeded)
        {
            // Enough food - consume it
            EqualyReduceFoodSupplies(availableFish, availableMeat, availableBread, (int)totalFoodNeeded);
            Debug.Log($"Fed {villagerCount} villagers ({totalFoodNeeded} food consumed). \n Remaining fish: {ResourceManager.Instance.GetResource(ResourceType.Fish)}. \n Remaining meat: {ResourceManager.Instance.GetResource(ResourceType.Meat)}. \n Remaining bread: {ResourceManager.Instance.GetResource(ResourceType.Bread)}");
            // All villagers are fed
            foreach (var villager in allVillagers)
            {
                villager.HandleHunger(false);
            }
        }
        else
        {
            print($"Not enough fish! Need {totalFoodNeeded} but only have {totalFoodAvailable}. Villagers are hungry!");
            ResourceManager.Instance.SpendResource(ResourceType.Fish, availableFish);
            ResourceManager.Instance.SpendResource(ResourceType.Meat, availableMeat);
            ResourceManager.Instance.SpendResource(ResourceType.Bread, availableBread);

            if (hungerMode == HungerDistributionMode.Shared)
                ApplySharedHunger(totalFoodAvailable, effectiveFoodPerVillager);
            else
                ApplyPrioritizedHunger(totalFoodAvailable, effectiveFoodPerVillager);
        }

        OnFoodConsumed?.Invoke(totalFoodAvailable >= totalFoodNeeded);
    }

    private void EqualyReduceFoodSupplies(int fish, int meat, int bread, int totalFoodRequired)
    {
        // Keep resource amounts in an array so we can cycle through them generically
        ResourceType[] types = { ResourceType.Fish, ResourceType.Meat, ResourceType.Bread };
        int[] amounts = { fish, meat, bread };

        int startType = 0;

        for (int i = 0; i < totalFoodRequired; i++)
        {
            bool foodConsumed = false;

            // Try up to 3 resource types starting from the current cycle position
            for (int attempt = 0; attempt < types.Length; attempt++)
            {
                int type = (startType + attempt) % types.Length;

                if (amounts[type] > 0)
                {
                    ResourceManager.Instance.SpendResource(types[type], 1);
                    amounts[type]--;
                    foodConsumed = true;
                    break;
                }
            }

            if (!foodConsumed)
            {
                // All resources are exhausted, nothing left to spend
                break;
            }

            startType = (startType + 1) % types.Length; // advance cycle for next unit of food
        }
    }

    /// <summary>
    /// Prioritized: randomly chosen villagers eat fully; the rest get nothing and starve.
    /// </summary>
    private void ApplyPrioritizedHunger(float availableFish, float fishPerVillager)
    {
        int fedCount = SettlementFormulas.GetFedCount(availableFish, fishPerVillager, allVillagers.Count);
        print($"Prioritized hunger: {fedCount}/{allVillagers.Count} villagers fed.");

        List<Villager> shuffled = allVillagers.OrderBy(v => UnityEngine.Random.value).ToList();
        for (int i = 0; i < shuffled.Count; i++)
            shuffled[i].HandleHunger(i >= fedCount);
    }

    /// <summary>
    /// Shared: food divided evenly across all villagers; everyone takes proportional hunger damage.
    /// 0 fish = 100% damage to all. Missing 1 of 3 fish = 33% damage to all.
    /// </summary>
    private void ApplySharedHunger(float availableFish, float fishPerVillager)
    {
        if (fishPerVillager <= 0f || allVillagers.Count == 0) return;

        float hungerFraction = SettlementFormulas.GetSharedHungerFraction(availableFish, fishPerVillager, allVillagers.Count);
        float foodPerVillager = availableFish / allVillagers.Count;

        Debug.LogWarning($"Shared hunger: {foodPerVillager:F2}/{fishPerVillager:F2} food each ({hungerFraction * 100f:F0}% shortage).");

        foreach (var villager in allVillagers)
            villager.HandleSharedHunger(hungerFraction);
    }

    public void SetHungerMode(HungerDistributionMode mode) => hungerMode = mode;
    public HungerDistributionMode GetHungerMode() => hungerMode;

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

    public Building GetBuildingById(string uniqueId)
    {
        foreach (var b in allBuildings)
        {
            if (b != null && b.uniqueId == uniqueId) return b;
        }
        return null;
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

            _deathLog.Add(new DeathRecord
            {
                name       = villager.villagerName,
                uniqueId   = villager.uniqueId,
                ageAtDeath = villager.age,
                cause      = villager.deathCause,
                wasJarl    = villager.isJarl,
                timeOfDeath = Time.time
            });

            Debug.Log($"Unregistered villager: {villager.villagerName} (Age: {villager.age:F1}, Cause: {villager.deathCause}). Total population: {allVillagers.Count}");
        }
    }

    public List<DeathRecord> GetDeathLog() => new List<DeathRecord>(_deathLog);
    
    /// <summary>
    /// Get all villagers
    /// </summary>
    public List<Villager> GetAllVillagers()
    {
        return new List<Villager>(allVillagers);
    }

    public Villager GetCurrentJarl()
    {
        return allVillagers.FirstOrDefault(v => v.isJarl);
    }

    public Villager GetVillagerById(string uniqueId)
    {
        foreach (var v in allVillagers)
        {
            if (v != null && v.uniqueId == uniqueId) return v;
        }
        return null;
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

    public Villager GetMostWoundedVillager()
    {
        var sortedList = allVillagers.OrderBy(villager => villager.currentHealth).ToList();
        if (sortedList.Count == 0) return null;
        if (sortedList[0].currentHealth == sortedList[0].maxHealth) return null;
        return sortedList[0];
    }
    
    /// <summary>
    /// Get total population count
    /// </summary>
    public int GetPopulation()
    {
        return allVillagers.Count;
    }

    /// <summary>
    /// Max village population, driven entirely by constructed Longhouse(s)' current level.
    /// </summary>
    public int GetMaxPopulation()
    {
        int cap = 0;
        foreach (var b in allBuildings)
        {
            if (b == null || !b.isConstructed || b.data == null) continue;
            if (b.data.buildingType != BuildingType.Longhouse) continue;
            cap += b.CurrentLevelData.populationCapBonus;
        }
        return cap;
    }

    public bool HasPopulationCapacity()
    {
        return GetPopulation() < GetMaxPopulation();
    }

    public int GetAverageChildCount()
    {
        int averagechildCount = 0;
        foreach (Villager v in allVillagers)
        {
            averagechildCount += v.childrenCount;
        }
        averagechildCount /= allVillagers.Count;
        return averagechildCount;
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

    public float GetOverrallMorale()
    {
        if (allVillagers.Count == 0) return 0f;
        float totalMorale = 0f;
        foreach (var villager in allVillagers)
        {
            totalMorale += villager.morale;
        }
        return totalMorale / allVillagers.Count;
    }

    public float GetOverrallCombat()
    {
        if (allVillagers.Count == 0) return 0f;
        float totalCombat = 0f;
        foreach (var villager in allVillagers)
        {
            totalCombat += villager.skills.combat;
        }
        return totalCombat / allVillagers.Count;
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

    #region ISaveable

    /// <summary>
    /// Builds a serializable snapshot of a single villager's full state. Pure function of the
    /// passed-in Villager — no dependency on SettlementManager.Instance — so it's safe to call
    /// from any scene (e.g. RaidManager capturing a raid party's state between scene loads).
    /// </summary>
    public static VillagerSave BuildVillagerSave(Villager v)
    {
        CharacterBase cb = v.GetComponent<CharacterBase>();

        return new VillagerSave
        {
            weaponName = cb?.weapon?.itemName,
            weaponDurability = cb?.weapon ? cb.weapon.CurrentDurability : 0,
            shieldName = cb?.shield?.itemName,
            shieldDurability = cb?.shield ? cb.shield.CurrentDurability : 0,
            torchName = cb?.torch?.itemName,
            id = v.uniqueId,
            villagerName = v.villagerName,
            clanName = v.clanName,
            gender = (int)v.gender,
            age = v.age,
            lifeExpectancy = v.lifeExpectancy,
            currentLifeStage = (int)v.currentLifeStage,
            health = v.currentHealth,
            maxHealth = v.maxHealth,
            morale = v.morale,
            maxMorale = v.maxMorale,
            isHungry = v.isHungry,
            isCold = v.isCold,
            currentJob = (int)v.currentJob,
            assignedBuildingId = v.assignedBuilding != null && v.assignedBuilding.data != null ? v.assignedBuilding.data.name : "",
            skills = new VillagerSkillsSave
            {
                farming = v.skills.farming,
                fishing = v.skills.hunting,
                mining = v.skills.mining,
                woodcutting = v.skills.woodcutting,
                crafting = v.skills.crafting,
                combat = v.skills.combat,
                sailing = v.skills.sailing,
                intelligence = v.skills.intelligence,
                learningRate = v.skills.learningRate
            },
            combatStats = new CombatStatsSave
            {
                strength = v.combatStats.strength,
                defense = v.combatStats.defense
            },
            parent1Id = v.parent1 != null ? v.parent1.uniqueId : "",
            parent2Id = v.parent2 != null ? v.parent2.uniqueId : "",
            partnerId = v.partner != null ? v.partner.uniqueId : "",
            childrenCount = v.childrenCount,
            timeSinceLastChild = v.timeSinceLastChild,
            isJarl = v.isJarl,
            isOfJarlLineage = v.isOfJarlLineage,
            generationsFromJarl = v.generationsFromJarl,
            posX = v.transform.position.x,
            posY = v.transform.position.y,
            posZ = v.transform.position.z,
            spriteVariant = v.spriteVariant,
            activeWounds = v.activeWounds != null
                ? v.activeWounds.Select(w => (int)w).ToArray()
                : new int[0]
        };
    }

    public void PopulateSaveData(SaveData data)
    {
        // Save villagers
        var villagerSaves = new List<VillagerSave>();
        foreach (var v in allVillagers.ToList())
        {
            if (v == null || v.IsDead()) continue;
            villagerSaves.Add(BuildVillagerSave(v));
        }
        data.villagers = villagerSaves.ToArray();

        // Save buildings
        var buildingSaves = new List<BuildingSave>();
        foreach (var b in allBuildings.ToList())
        {
            if (b == null) continue;

            var bs = new BuildingSave
            {
                id = b.uniqueId,
                buildingDataName = b.data != null ? b.data.name : "",
                gridPositionX = b.gridPosition.x,
                gridPositionY = b.gridPosition.y,
                isConstructed = b.isConstructed,
                productionProgress = b.productionProgress,
                needsRepair = b.needsRepair,
                level = b.level,
                assignedWorkerIds = new string[b.assignedWorkers.Count]
            };

            for (int i = 0; i < b.assignedWorkers.Count; i++)
            {
                bs.assignedWorkerIds[i] = b.assignedWorkers[i] != null ? b.assignedWorkers[i].uniqueId : "";
            }

            buildingSaves.Add(bs);
        }
        data.buildings = buildingSaves.ToArray();

        // Save settlement stats
        data.stats = new SettlementStatsSave
        {
            totalBirths = totalBirths,
            totalDeaths = totalDeaths
        };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.villagers == null || data.villagers.Length == 0)
        {
            Debug.LogError("SettlementManager: No villagers in save data.");
            return;
        }

        if (VillagerSpawner.Instance == null)
        {
            Debug.LogError("SettlementManager: VillagerSpawner not found — cannot restore villagers.");
            return;
        }

        // Destroy any villagers that exist before this load.
        foreach (var v in allVillagers.ToArray())
        {
            if (v != null) Destroy(v.gameObject);
        }
        allVillagers.Clear();

        // VillagerSpawner owns all instantiation: restore each villager, which also
        // registers it with SettlementManager via Villager.Init().
        var villagerLookup = new Dictionary<string, Villager>();

        foreach (var vs in data.villagers)
        {
            Villager v = VillagerSpawner.Instance.RestoreVillagerFromSave(vs);
            if (v != null)
                villagerLookup[vs.id] = v;
        }

        // Build building lookup by data name (BuildingData SO name)
        // If multiple buildings share a type they are stored in a list.
        var buildingsByName = new Dictionary<string, List<Building>>();
        foreach (var b in allBuildings)
        {
            if (b != null && b.data != null)
            {
                string buildingName = b.data.name;
                if (!buildingsByName.ContainsKey(buildingName))
                {
                    buildingsByName[buildingName] = new List<Building>();
                }
                buildingsByName[buildingName].Add(b);
            }
        }

        // Resolve villager references (parent, partner)
        foreach (var vs in data.villagers)
        {
            if (!villagerLookup.TryGetValue(vs.id, out Villager v)) continue;

            v.parent1 = !string.IsNullOrEmpty(vs.parent1Id) && villagerLookup.ContainsKey(vs.parent1Id) ? villagerLookup[vs.parent1Id] : null;
            v.parent2 = !string.IsNullOrEmpty(vs.parent2Id) && villagerLookup.ContainsKey(vs.parent2Id) ? villagerLookup[vs.parent2Id] : null;
            v.partner = !string.IsNullOrEmpty(vs.partnerId) && villagerLookup.ContainsKey(vs.partnerId) ? villagerLookup[vs.partnerId] : null;

            // Resolve assigned building by BuildingData name
            if (!string.IsNullOrEmpty(vs.assignedBuildingId) && buildingsByName.TryGetValue(vs.assignedBuildingId, out List<Building> matchingBuildings))
            {
                // Find first building of this type that has capacity or already has this worker
                Building assignedBuilding = null;
                foreach (var b in matchingBuildings)
                {
                    if (b.CanAssignWorker() || b.assignedWorkers.Contains(v))
                    {
                        assignedBuilding = b;
                        break;
                    }
                }
                // Fallback to first building of this type if none have capacity
                if (assignedBuilding == null && matchingBuildings.Count > 0)
                {
                    assignedBuilding = matchingBuildings[0];
                }

                if (assignedBuilding != null)
                {
                    v.assignedBuilding = assignedBuilding;
                }
            }
        }

        // Restore building workers from saved data - match by BuildingData name
        if (data.buildings != null)
        {
            // Track which buildings we've already processed (for handling duplicates)
            var processedBuildingIndices = new Dictionary<string, int>();

            foreach (var bs in data.buildings)
            {
                if (string.IsNullOrEmpty(bs.buildingDataName)) continue;
                if (!buildingsByName.TryGetValue(bs.buildingDataName, out List<Building> matchingBuildings)) continue;

                // Get the next unprocessed building of this type
                if (!processedBuildingIndices.ContainsKey(bs.buildingDataName))
                {
                    processedBuildingIndices[bs.buildingDataName] = 0;
                }
                int buildingIndex = processedBuildingIndices[bs.buildingDataName];
                if (buildingIndex >= matchingBuildings.Count) continue;

                Building b = matchingBuildings[buildingIndex];
                processedBuildingIndices[bs.buildingDataName]++;

                // Restore building state
                b.isConstructed = bs.isConstructed;
                b.productionProgress = bs.productionProgress;
                b.SetNeedsRepair(bs.needsRepair);
                b.level = bs.level > 0 ? bs.level : 1;

                // Restore assigned workers
                b.assignedWorkers.Clear();
                if (bs.assignedWorkerIds != null && bs.assignedWorkerIds.Length > 0)
                {
                    foreach (var workerId in bs.assignedWorkerIds)
                    {
                        if (!string.IsNullOrEmpty(workerId) && villagerLookup.ContainsKey(workerId))
                        {
                            Villager worker = villagerLookup[workerId];
                            b.assignedWorkers.Add(worker);
                            worker.assignedBuilding = b; // Ensure bi-directional link
                        }
                    }
                }

                Debug.Log($"Restored building '{bs.buildingDataName}' with {b.assignedWorkers.Count} workers");
            }
        }

        // Restore stats
        if (data.stats != null)
        {
            totalBirths = data.stats.totalBirths;
            totalDeaths = data.stats.totalDeaths;
        }

        totalFoodNeeded = allVillagers.Count * foodPerVillagerPerDay;

        // Refresh shadows to pick up newly spawned villagers.
        if (ShadowMaster.Instance != null)
            ShadowMaster.Instance.RefreshShadows();

        Debug.Log($"SettlementManager: Loaded {villagerLookup.Count} villagers from save.");
    }

    #endregion
}

public enum HungerDistributionMode
{
    Prioritized, // Some villagers eat fully; the rest starve completely
    Shared       // Food split evenly; all take proportional hunger damage
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