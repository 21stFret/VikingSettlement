using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simulates settlement activity over a period of time (for when player is away on raids)
/// Calculates: production, consumption, random events
/// </summary>
public static class SettlementSimulator
{
    // Event chances per day
    private const float WOLF_ATTACK_CHANCE = 0.1f;      // 10% per day
    private const float GOOD_HARVEST_CHANCE = 0.15f;    // 15% per day
    private const float VILLAGER_SICK_CHANCE = 0.05f;   // 5% per day
    private const float TRADER_VISIT_CHANCE = 0.1f;     // 10% per day
    private const float BIRTH_CHANCE = 0.05f;           // 5% per day (if eligible couples)

    /// <summary>
    /// Simulate what happens at the settlement over a number of days
    /// </summary>
    /// <param name="days">Number of whole game-days to simulate (for events)</param>
    /// <param name="absentVillagers">Villagers who are away (on raid)</param>
    /// <param name="exactDays">Exact fractional days for resource calculations (optional)</param>
    /// <param name="raidStartAbsoluteDay">Absolute day the raid began — used for storm lookup. -1 disables storm-aware wood.</param>
    public static SettlementReport SimulateTime(int days, List<Villager> absentVillagers = null, float exactDays = -1f, int raidStartAbsoluteDay = -1)
    {
        // Use exact days for resource calculations if provided, otherwise use whole days
        float resourceDays = exactDays > 0 ? exactDays : days;

        SettlementReport report = new SettlementReport
        {
            daysPassed = days,
            exactDaysPassed = resourceDays,
            resourceChanges = new Dictionary<ResourceType, float>(),
            events = new List<SettlementEvent>()
        };

        // Initialize resource changes to 0
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            report.resourceChanges[type] = 0f;
        }

        // Get settlement state
        if (SettlementManager.Instance == null)
        {
            Debug.LogWarning("SettlementSimulator: No SettlementManager found!");
            return report;
        }

        var allVillagers = SettlementManager.Instance.GetAllVillagers();
        var presentVillagers = allVillagers.Where(v =>
            v != null &&
            !v.IsDead() &&
            (absentVillagers == null || !absentVillagers.Contains(v))
        ).ToList();

        var allBuildings = SettlementManager.Instance.GetAllBuildings();

        // Calculate production, consumption, and skill gains
        SimulateProductionAndConsumption(presentVillagers, allBuildings, report, resourceDays, raidStartAbsoluteDay);

        // Simulate random events for each whole day
        for (int day = 1; day <= days; day++)
        {
            SimulateRandomEvents(day, presentVillagers, report);
        }

        return report;
    }

    /// <summary>
    /// Calculate how many production completions a building would have over a time period
    /// </summary>
    private static int CalculateBuildingCompletions(Building building, float totalSeconds)
    {
        if (building == null || building.data == null) return 0;
        if (building.assignedWorkers.Count == 0) return 0;

        // Get production rate based on building type
        float baseRate = building.data.productionType == ProductionType.Crafting
            ? building.data.craftingRecipe?.craftingRate ?? 0f
            : building.data.productionRate;

        if (baseRate <= 0) return 0;

        // Calculate production speed same as Building.GetProductionSpeed
        float productionSpeed = 0f;
        foreach (var worker in building.assignedWorkers)
        {
            if (worker == null || worker.IsDead()) continue;
            productionSpeed += baseRate * worker.GetSkillMultiplier(building.data.assignedJobType);
        }

        // Calculate total progress and completions
        float totalProgress = productionSpeed * totalSeconds;
        return Mathf.FloorToInt(totalProgress / 100f);
    }

    /// <summary>
    /// Simulate production and consumption scaled by exact fractional days
    /// Uses same logic as Building.UpdateProduction - progress fills based on rate * skill, produces on 100%
    /// </summary>
    private static void SimulateProductionAndConsumption(List<Villager> villagers, List<Building> buildings, SettlementReport report, float days, int raidStartAbsoluteDay = -1)
    {
        // B8: use authoritative day length instead of hardcoded 120f
        float dayLength = DayNightManager.Instance != null
            ? DayNightManager.Instance.dayLengthInSeconds
            : 120f;
        if (DayNightManager.Instance == null)
            Debug.LogWarning("SettlementSimulator: DayNightManager unavailable, falling back to 120f day length.");
        float totalSeconds = days * dayLength;

        // === PRODUCTION ===
        foreach (var building in buildings)
        {
            if (building == null || !building.isConstructed) continue;
            if (building.assignedWorkers.Count == 0) continue;

            var data = building.data;
            if (data == null) continue;

            // Calculate completions for this building
            int completions = CalculateBuildingCompletions(building, totalSeconds);
            if (completions <= 0) continue;

            // Handle resource gathering
            if (data.productionType == ProductionType.ResourceGathering && data.producedResource != ResourceType.None)
            {
                float seasonalMultiplier = building.GetSeasonalMultiplier();
                int production = Mathf.FloorToInt(completions * data.productionAmount * seasonalMultiplier);
                report.resourceChanges[data.producedResource] += production;
            }
            // Handle crafting
            else if (data.productionType == ProductionType.Crafting && data.craftingRecipe != null)
            {
                // Check if resources would be available (simplified - assume they are)
                int output = Mathf.FloorToInt(completions * data.craftingRecipe.outputAmount);
                report.resourceChanges[data.craftingRecipe.outputResource] += output;

                // Consume input resources
                foreach (var input in data.craftingRecipe.inputResources)
                {
                    report.resourceChanges[input.resourceType] -= input.amount * completions;
                }
            }

            // Skill gains for workers (each completion = 1 ImproveSkill per worker)
            foreach (var worker in building.assignedWorkers)
            {
                if (worker == null || worker.IsDead()) continue;
                for (int i = 0; i < completions; i++)
                {
                    worker.skills.ImproveSkill(data.assignedJobType);
                }
            }
        }

        // === CONSUMPTION (scaled by days) ===
        int villagerCount = villagers.Count;

        // B25: FloorToInt + partial day so production seconds match resource days exactly
        int   wholeDays  = Mathf.FloorToInt(days);
        float partialDay = days - wholeDays;

        // B9sim: Wheat is a raw resource — bread chain not yet implemented, Fish is sole food source
        float fishPerVillager = SettlementManager.Instance != null
            ? SettlementManager.Instance.fishPerVillagerPerDay
            : 1f;
        float foodNeeded    = villagerCount * fishPerVillager * days; // exact float — partial day already baked in
        float fishAvailable = GetAvailableResource(ResourceType.Fish);

        if (fishAvailable >= foodNeeded)
        {
            report.resourceChanges[ResourceType.Fish] -= foodNeeded;
        }
        else
        {
            report.resourceChanges[ResourceType.Fish] -= fishAvailable;
            report.moraleChange -= 5f * days;

            report.events.Add(new SettlementEvent
            {
                eventName = "Food Shortage",
                description = "The settlement didn't have enough food while you were away.",
                eventType = SettlementEventType.Negative
            });
        }

        // B10sim: Storm-aware firewood — per-day loop so each day's storm multiplier is respected
        float woodPerVillagerDay = SettlementManager.Instance != null
            ? SettlementManager.Instance.woodPerVillagerPerDay
            : 0.5f;
        bool isWinter = SeasonManager.Instance != null
            && SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
        float seasonMult = isWinter ? 1.0f : 0.5f;

        // Build storm lookup keyed by absolute day — O(1) per simulated day
        var stormLookup = new Dictionary<int, float>();
        if (raidStartAbsoluteDay >= 0)
        {
            if (StormScheduler.Instance != null)
            {
                var stormDays = StormScheduler.Instance.GetStormDaysInRange(
                    raidStartAbsoluteDay, raidStartAbsoluteDay + wholeDays);
                foreach (var (absDay, mult) in stormDays)
                    stormLookup[absDay] = mult;
            }
            else
            {
                Debug.LogWarning("SettlementSimulator: StormScheduler unavailable — firewood storm multiplier defaulting to 1.0");
            }
        }

        float totalWoodNeeded = 0f;
        for (int dayIndex = 0; dayIndex < wholeDays; dayIndex++)
        {
            int   absDay    = raidStartAbsoluteDay >= 0 ? raidStartAbsoluteDay + dayIndex : -1;
            float stormMult = (absDay >= 0 && stormLookup.ContainsKey(absDay)) ? stormLookup[absDay] : 1.0f;
            totalWoodNeeded += villagerCount * woodPerVillagerDay * seasonMult * stormMult;
        }
        // Partial day at the end
        if (partialDay > 0f)
        {
            int   absPartialDay = raidStartAbsoluteDay >= 0 ? raidStartAbsoluteDay + wholeDays : -1;
            float stormMult     = (absPartialDay >= 0 && stormLookup.ContainsKey(absPartialDay)) ? stormLookup[absPartialDay] : 1.0f;
            totalWoodNeeded += villagerCount * woodPerVillagerDay * seasonMult * stormMult * partialDay;
        }

        if (totalWoodNeeded > 0f)
        {
            float woodAvailable = GetAvailableResource(ResourceType.Wood);
            if (woodAvailable >= totalWoodNeeded)
            {
                report.resourceChanges[ResourceType.Wood] -= totalWoodNeeded;
            }
            else
            {
                report.resourceChanges[ResourceType.Wood] -= woodAvailable;
                report.moraleChange -= 10f;
                report.villagerDamage += villagerCount * 2f * days;

                report.events.Add(new SettlementEvent
                {
                    eventName = "Freezing Winter",
                    description = "The settlement ran out of firewood. Villagers suffered from the cold.",
                    eventType = SettlementEventType.Negative
                });
            }
        }
    }

    /// <summary>
    /// Simulate random events for a single day
    /// </summary>
    private static void SimulateRandomEvents(int dayNumber, List<Villager> villagers, SettlementReport report)
    {
        // Wolf attack
        if (Random.value < WOLF_ATTACK_CHANCE)
        {
            float damage = Random.Range(10f, 25f);
            report.villagerDamage += damage;

            report.events.Add(new SettlementEvent
            {
                eventName = "Wolf Attack",
                description = $"Wolves attacked the settlement on day {dayNumber}. Some villagers were injured.",
                eventType = SettlementEventType.Negative
            });
        }

        // Good harvest bonus
        if (Random.value < GOOD_HARVEST_CHANCE)
        {
            ResourceType bonusType = Random.value > 0.5f ? ResourceType.Wheat : ResourceType.Fish;
            int bonusAmount = Mathf.FloorToInt(Random.Range(5f, 15f));
            report.resourceChanges[bonusType] += bonusAmount;
            report.moraleChange += 2f;

            report.events.Add(new SettlementEvent
            {
                eventName = "Bountiful Harvest",
                description = $"The gods blessed the settlement with extra {bonusType} on day {dayNumber}.",
                eventType = SettlementEventType.Positive
            });
        }

        // Villager gets sick
        if (Random.value < VILLAGER_SICK_CHANCE && villagers.Count > 0)
        {
            report.villagerDamage += Random.Range(5f, 15f);

            report.events.Add(new SettlementEvent
            {
                eventName = "Illness",
                description = $"A villager fell ill on day {dayNumber}.",
                eventType = SettlementEventType.Negative
            });
        }

        // Trader visit
        if (Random.value < TRADER_VISIT_CHANCE)
        {
            // Random resource bonus from trade
            ResourceType tradeResource = GetRandomTradeResource();
            int tradeAmount = Mathf.FloorToInt(Random.Range(3f, 10f));
            report.resourceChanges[tradeResource] += tradeAmount;

            report.events.Add(new SettlementEvent
            {
                eventName = "Trader Visit",
                description = $"A traveling trader visited on day {dayNumber}, bringing {tradeAmount} {tradeResource}.",
                eventType = SettlementEventType.Positive
            });
        }

        // Good spirits / morale boost
        if (Random.value < 0.1f)
        {
            report.moraleChange += 5f;

            report.events.Add(new SettlementEvent
            {
                eventName = "High Spirits",
                description = $"The villagers celebrated together on day {dayNumber}.",
                eventType = SettlementEventType.Positive
            });
        }
    }

    #region Helpers

    private static float GetAvailableResource(ResourceType type)
    {
        if (ResourceManager.Instance != null)
        {
            return ResourceManager.Instance.GetResource(type);
        }
        return 0f;
    }

    private static ResourceType GetRandomTradeResource()
    {
        ResourceType[] tradeResources = new ResourceType[]
        {
            ResourceType.Iron,
            ResourceType.Tools,
            ResourceType.Weapons,
            ResourceType.Leather,
            ResourceType.Clothing
        };

        return tradeResources[Random.Range(0, tradeResources.Length)];
    }

    #endregion
}
