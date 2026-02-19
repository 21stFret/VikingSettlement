using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SettlementManager : MonoBehaviour, ISaveable
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
    private float gameTickTimer = 0f;
    private float ageTimer = 0f;
    public float age;
    private float countingAge;

    [Header("Food Consumption")]
    public float fishPerVillagerPerDay = 1f;
    [SerializeField] private HungerDistributionMode hungerMode = HungerDistributionMode.Prioritized;

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
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnGameTick += OnTick;
            GameTickManager.Instance.OnFastUpdate += FastUpdate;
        }

        // Subscribe to Jarl events for settlement-wide morale effects
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied += HandleJarlDeath;
            JarlManager.Instance.OnJarlChanged += HandleJarlChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnGameTick -= OnTick;
            GameTickManager.Instance.OnFastUpdate -= FastUpdate;
        }
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnMealTime -= HandleMealTime;
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
    
    #region Food Consumption

    /// <summary>
    /// Handles meal time event from DayNightManager
    /// </summary>
    private void HandleMealTime()
    {
        if (DayNightManager.Instance == null || ResourceManager.Instance == null)
            return;

        int villagerCount = allVillagers.Count;
        float effectiveFishPerVillager = fishPerVillagerPerDay;

        // Apply Rationing runestone bonus (-1 food per villager)
        if (RunestoneManager.Instance != null)
        {
            effectiveFishPerVillager += RunestoneManager.Instance.GetFoodConsumptionModifier();
            effectiveFishPerVillager = Mathf.Max(0f, effectiveFishPerVillager);
        }

        float totalFishNeeded = villagerCount * effectiveFishPerVillager;

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
            Debug.LogWarning($"Not enough fish! Need {totalFishNeeded} but only have {availableFish}. Villagers are hungry!");

            if (availableFish > 0)
                ResourceManager.Instance.SpendResource(ResourceType.Fish, availableFish);

            if (hungerMode == HungerDistributionMode.Shared)
                ApplySharedHunger(availableFish, effectiveFishPerVillager);
            else
                ApplyPrioritizedHunger(availableFish, effectiveFishPerVillager);
        }
    }

    /// <summary>
    /// Prioritized: randomly chosen villagers eat fully; the rest get nothing and starve.
    /// </summary>
    private void ApplyPrioritizedHunger(float availableFish, float fishPerVillager)
    {
        int fedCount = fishPerVillager > 0 ? Mathf.FloorToInt(availableFish / fishPerVillager) : allVillagers.Count;
        Debug.LogWarning($"Prioritized hunger: {fedCount}/{allVillagers.Count} villagers fed.");

        List<Villager> shuffled = allVillagers.OrderBy(v => Random.value).ToList();
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

        float foodPerVillager = availableFish / allVillagers.Count;
        float hungerFraction = Mathf.Clamp01((fishPerVillager - foodPerVillager) / fishPerVillager);

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

    #region ISaveable

    /// <summary>
    /// Gets the name of an equipable item for saving.
    /// Falls back to GameObject name (without Clone suffix) if itemName is empty.
    /// </summary>
    private string GetEquipableItemName(EquipableItem item)
    {
        if (item == null) return "";

        // Use itemName if it's set
        if (!string.IsNullOrEmpty(item.itemName))
        {
            return item.itemName;
        }

        // Fall back to GameObject name, stripping "(Clone)" suffix
        string goName = item.gameObject.name;
        if (goName.EndsWith("(Clone)"))
        {
            goName = goName.Substring(0, goName.Length - 7).Trim();
        }
        return goName;
    }

    public void PopulateSaveData(SaveData data)
    {
        // Save villagers
        var villagerSaves = new List<VillagerSave>();
        foreach (var v in allVillagers)
        {
            if (v == null || v.IsDead()) continue;

            var vs = new VillagerSave
            {
                id = v.uniqueId,
                villagerName = v.villagerName,
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
                    fishing = v.skills.fishing,
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
                weaponName = GetEquipableItemName(v.GetComponent<CharacterController>()?.weapon),
                shieldName = GetEquipableItemName(v.GetComponent<CharacterController>()?.shield),
                torchName = GetEquipableItemName(v.GetComponent<ItemAttachment>()?.backAttachedItem),
                activeWounds = v.activeWounds != null
                    ? v.activeWounds.Select(w => (int)w).ToArray()
                    : new int[0]
            };

            // Debug: Log weapon/shield being saved
            if (!string.IsNullOrEmpty(vs.weaponName) || !string.IsNullOrEmpty(vs.shieldName))
            {
                Debug.Log($"Saving {v.villagerName}: weapon='{vs.weaponName}', shield='{vs.shieldName}'");
            }

            villagerSaves.Add(vs);
        }
        data.villagers = villagerSaves.ToArray();

        // Save buildings
        var buildingSaves = new List<BuildingSave>();
        foreach (var b in allBuildings)
        {
            if (b == null) continue;

            var bs = new BuildingSave
            {
                id = b.uniqueId,
                buildingDataName = b.data != null ? b.data.name : "",
                gridPositionX = b.gridPosition.x,
                gridPositionY = b.gridPosition.y,
                isConstructed = b.isConstructed,
                constructionProgress = b.constructionProgress,
                productionProgress = b.productionProgress,
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
        if (data.villagers == null) return;

        // Get the villager prefab from VillagerSpawner
        GameObject villagerPrefab = null;
        Transform villagerParent = null;
        if (VillagerSpawner.Instance != null)
        {
            villagerPrefab = VillagerSpawner.Instance.GetVillagerPrefab();
            villagerParent = VillagerSpawner.Instance.GetVillagerParent();
        }

        if (villagerPrefab == null)
        {
            Debug.LogError("SettlementManager: Cannot load save - no villager prefab found!");
            return;
        }

        // Destroy any existing villagers (shouldn't be any if VillagerSpawner skipped spawn)
        foreach (var v in allVillagers.ToArray())
        {
            if (v != null)
            {
                Destroy(v.gameObject);
            }
        }
        allVillagers.Clear();

        // Create villagers from save data
        var villagerLookup = new Dictionary<string, Villager>();
        var loadedVillagers = new List<Villager>();

        foreach (var vs in data.villagers)
        {
            Vector3 pos = new Vector3(vs.posX, vs.posY, vs.posZ);
            GameObject villagerObj = Instantiate(villagerPrefab, pos, Quaternion.identity);

            if (villagerParent != null)
            {
                villagerObj.transform.SetParent(villagerParent);
            }

            Villager v = villagerObj.GetComponent<Villager>();
            if (v == null) continue;

            // Mark as loaded from save to skip default initialization in Start()
            v.loadedFromSave = true;

            // Set uniqueId BEFORE Start() runs (Instantiate is synchronous, Start runs next frame)
            v.uniqueId = vs.id;

            // Set all other properties
            v.villagerName = vs.villagerName;
            v.gender = (Gender)vs.gender;
            v.age = vs.age;
            v.lifeExpectancy = vs.lifeExpectancy;
            v.currentLifeStage = (LifeStage)vs.currentLifeStage;
            v.currentHealth = vs.health;
            // maxHealth is not restored — ApplySkillBonuses() derives it from the prefab base
            // plus any active runestone/skill-tree modifiers (which are loaded before this).
            v.morale = vs.morale;
            v.maxMorale = vs.maxMorale;
            v.isHungry = vs.isHungry;
            v.isCold = vs.isCold;
            v.currentJob = (JobType)vs.currentJob;

            v.skills.farming = vs.skills.farming;
            v.skills.fishing = vs.skills.fishing;
            v.skills.mining = vs.skills.mining;
            v.skills.woodcutting = vs.skills.woodcutting;
            v.skills.crafting = vs.skills.crafting;
            v.skills.combat = vs.skills.combat;
            v.skills.sailing = vs.skills.sailing;
            v.skills.intelligence = vs.skills.intelligence;
            v.skills.learningRate = vs.skills.learningRate;

            v.combatStats.strength = vs.combatStats.strength;
            v.combatStats.defense = vs.combatStats.defense;

            v.childrenCount = vs.childrenCount;
            v.timeSinceLastChild = vs.timeSinceLastChild;
            v.isJarl = vs.isJarl;
            v.isOfJarlLineage = vs.isOfJarlLineage;
            v.generationsFromJarl = vs.generationsFromJarl;
            v.spriteVariant = vs.spriteVariant;

            // Restore weapon and shield
            ItemAttachment itemAttachment = v.GetComponent<ItemAttachment>();
            if (itemAttachment != null && WeaponDatabase.Instance != null)
            {
                // Debug: Log what we're trying to load
                if (!string.IsNullOrEmpty(vs.weaponName) || !string.IsNullOrEmpty(vs.shieldName))
                {
                    Debug.Log($"Loading {vs.villagerName}: weapon='{vs.weaponName}', shield='{vs.shieldName}'");
                }

                if (!string.IsNullOrEmpty(vs.weaponName))
                {
                    EquipableItem weaponPrefab = WeaponDatabase.Instance.GetWeaponByName(vs.weaponName);
                    if (weaponPrefab != null)
                    {
                        GameObject weaponInstance = Instantiate(weaponPrefab.gameObject);
                        itemAttachment.EquipWeapon(weaponInstance);
                        Debug.Log($"  -> Weapon '{vs.weaponName}' equipped successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"  -> Weapon '{vs.weaponName}' NOT FOUND in WeaponDatabase!");
                    }
                }
                if (!string.IsNullOrEmpty(vs.shieldName))
                {
                    EquipableItem shieldPrefab = WeaponDatabase.Instance.GetShieldByName(vs.shieldName);
                    if (shieldPrefab != null)
                    {
                        GameObject shieldInstance = Instantiate(shieldPrefab.gameObject);
                        itemAttachment.EquipShield(shieldInstance);
                        Debug.Log($"  -> Shield '{vs.shieldName}' equipped successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"  -> Shield '{vs.shieldName}' NOT FOUND in WeaponDatabase!");
                    }
                }
                if (!string.IsNullOrEmpty(vs.torchName))
                {
                    EquipableItem torchPrefab = WeaponDatabase.Instance.GetTorchByName(vs.torchName);
                    if (torchPrefab != null)
                    {
                        GameObject torchInstance = Instantiate(torchPrefab.gameObject);
                        itemAttachment.EquipTorch(torchInstance);
                        Debug.Log($"  -> Torch '{vs.torchName}' equipped successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"  -> Torch '{vs.torchName}' NOT FOUND in WeaponDatabase!");
                    }
                }
            }
            else if (itemAttachment == null)
            {
                Debug.LogWarning($"Loading {vs.villagerName}: No ItemAttachment component!");
            }
            else if (WeaponDatabase.Instance == null)
            {
                Debug.LogWarning($"Loading {vs.villagerName}: WeaponDatabase.Instance is null!");
            }

            // Restore wounds before ApplySkillBonuses so the HP penalty is included.
            if (vs.activeWounds != null)
            {
                foreach (int w in vs.activeWounds)
                    v.activeWounds.Add((WoundType)w);
            }

            // Derive maxHealth from base + active modifiers (runestones/skill tree/wounds already set).
            v.ApplySkillBonuses();

            villagerObj.name = vs.villagerName;
            villagerLookup[vs.id] = v;
            loadedVillagers.Add(v);
        }

        // Build building lookup by data name (BuildingData SO name)
        // If multiple buildings have the same type, they're stored in a list
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
                b.constructionProgress = bs.constructionProgress;
                b.productionProgress = bs.productionProgress;

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

        // Notify VillagerSpawner about loaded villagers
        if (VillagerSpawner.Instance != null)
        {
            VillagerSpawner.Instance.OnVillagersLoadedFromSave(loadedVillagers);
        }

        // Refresh shadows to pick up newly spawned villagers
        if (ShadowMaster.Instance != null)
        {
            ShadowMaster.Instance.RefreshShadows();
        }

        Debug.Log($"Loaded {loadedVillagers.Count} villagers from save");
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