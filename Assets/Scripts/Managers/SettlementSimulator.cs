using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simulates settlement activity day-by-day for the time the player was away on a raid — production,
/// consumption, and random events resolve in the same per-day order the live game would (production →
/// events → wood/cold → food/hunger → death check), so a villager who dies partway through the raid stops
/// producing/consuming for the remaining simulated days, and shortage checks see stock already adjusted
/// by that same day's production. Builds a single SettlementReport applied atomically at the end —
/// nothing in this file ever calls a live mutating method (TakeDamage, SpendResource, Die, ShowSpeech,
/// coroutines, etc.) from inside the day loop.
/// </summary>
public static class SettlementSimulator
{
    // Event chances per simulated day
    private const float WOLF_ATTACK_CHANCE = 0.1f;      // 10% per day
    private const float GOOD_HARVEST_CHANCE = 0.15f;    // 15% per day
    private const float VILLAGER_SICK_CHANCE = 0.05f;   // 5% per day
    private const float TRADER_VISIT_CHANCE = 0.1f;     // 10% per day
    private const float HIGH_SPIRITS_CHANCE = 0.1f;     // 10% per day

    /// <summary>
    /// Per-villager health/morale tracked individually across the day loop, seeded from real current
    /// values. Never written back to the live Villager until the report is applied by the caller.
    /// </summary>
    private class VillagerSimState
    {
        public Villager liveRef;
        public float startHealth, startMorale; // seeded snapshot — used to compute deltas at the end
        public float health, maxHealth;
        public float morale, maxMorale;
        public bool isDead;
        public DeathCause deathCause;    // Combat / Starvation / Cold / Other only — no OldAge (out of scope)
        public bool isHungry, isCold;    // mirror live flags, refreshed every simulated day
        public bool lastDamageWasCombat; // mirrors TargetHealth.lastDamageWasCombat — last write wins, same
                                          // as live TakeDamage's unconditional overwrite on every hit
        public JobType jobType;          // cached from currentJob at seed time (stable for raid duration)
    }

    /// <summary>
    /// Simulate what happens at the settlement over a number of days.
    /// </summary>
    /// <param name="days">Number of whole game-days to simulate (for events/day loop)</param>
    /// <param name="absentVillagers">Villagers who are away (on raid) — excluded entirely from the simulation</param>
    /// <param name="exactDays">Exact fractional days for resource calculations (optional)</param>
    /// <param name="raidStartAbsoluteDay">Absolute day the raid began — used for season/storm lookahead. -1 disables it.</param>
    public static SettlementReport SimulateTime(int days, List<Villager> absentVillagers = null, float exactDays = -1f, int raidStartAbsoluteDay = -1)
    {
        float resourceDays = exactDays > 0 ? exactDays : days;

        SettlementReport report = new SettlementReport
        {
            daysPassed = days,
            exactDaysPassed = resourceDays,
            resourceChanges = new Dictionary<ResourceType, float>(),
            events = new List<SettlementEvent>(),
            villagerOutcomes = new List<VillagerOutcome>(),
            skillGains = new List<SkillGain>()
        };

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            report.resourceChanges[type] = 0f;

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

        // ---- Seed per-villager sim state (pure, read-only snapshot). Order preserved from
        // GetAllVillagers() registration order — required so the cold-effects "first N" selection
        // matches live SettlementManager.ApplyColdEffects exactly. ----
        var orderedStates = new List<VillagerSimState>();
        var stateByVillager = new Dictionary<Villager, VillagerSimState>();
        foreach (var v in presentVillagers)
        {
            var state = new VillagerSimState
            {
                liveRef = v,
                startHealth = v.currentHealth, health = v.currentHealth, maxHealth = v.maxHealth,
                startMorale = v.morale, morale = v.morale, maxMorale = v.maxMorale,
                isDead = false, isHungry = v.isHungry, isCold = v.isCold, lastDamageWasCombat = false,
                jobType = v.currentJob
            };
            orderedStates.Add(state);
            stateByVillager[v] = state;
        }

        // ---- Per-building worker roster — simulator-owned, mutated only in simulator memory, never
        // touches the live Building.assignedWorkers list. Skips buildings needing repair, matching live
        // Building.UpdateProduction's guard (the previous simulator didn't check this). ----
        var buildingRoster = new Dictionary<Building, List<Villager>>();
        foreach (var b in allBuildings)
        {
            if (b == null || !b.isConstructed || b.needsRepair) continue;
            buildingRoster[b] = b.assignedWorkers.Where(w => w != null && stateByVillager.ContainsKey(w)).ToList();
        }

        // Resource types the settlement is actually gathering this raid (staffed ResourceGathering
        // buildings only) — used to keep random events from blessing/trading resources nobody produces.
        var producedResourceTypes = new HashSet<ResourceType>(
            buildingRoster.Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => kvp.Key.data)
                .Where(d => d != null && d.productionType == ProductionType.ResourceGathering && d.resourceOutputs != null)
                .SelectMany(d => d.resourceOutputs)
                .Select(o => o.resourceType)
                .Where(rt => rt != ResourceType.None));

        // Deferred skill-gain bookkeeping — completions are tallied here and applied once, atomically, by
        // the caller (RaidManager.ApplySettlementReport), never mutated live from inside this loop.
        var skillCompletionsAccrued = new Dictionary<Villager, int>();

        // ---- Read-only global snapshots — treated as constant for the raid's duration, same
        // simplification the previous simulator already made for Runestone multipliers. ----
        float dayLength = DayNightManager.Instance != null ? DayNightManager.Instance.dayLengthInSeconds : 120f;
        if (DayNightManager.Instance == null)
            Debug.LogWarning("SettlementSimulator: DayNightManager unavailable, falling back to 120f day length.");

        float fishPerVillagerBase = SettlementManager.Instance.foodPerVillagerPerDay;
        float woodPerVillagerBase = SettlementManager.Instance.woodPerVillagerPerDay;
        HungerDistributionMode hungerMode = SettlementManager.Instance.GetHungerMode();
        float minStarvationDamage = SettlementManager.Instance.minStarvationDamage;
        float coldMoralePenalty = SettlementManager.Instance.coldMoralePenalty;
        float coldHealthDamage = SettlementManager.Instance.coldHealthDamage;
        float warmthMoraleBonus = SettlementManager.Instance.warmthMoraleBonus;

        float rationingModifier = RunestoneManager.Instance != null ? RunestoneManager.Instance.GetFoodConsumptionModifier() : 0f;
        float productionSpeedMult = RunestoneManager.Instance != null ? RunestoneManager.Instance.GetProductionSpeedMultiplier() : 1f;
        float woodConsumptionMult = RunestoneManager.Instance != null ? RunestoneManager.Instance.GetWoodConsumptionMultiplier() : 1f;

        bool gefjonActive = DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive;
        float gefjonHealPct = gefjonActive ? DeathTypeBuff.Instance.GetHealSpeedPercent() : 0f;
        float gefjonFoodPct = gefjonActive ? DeathTypeBuff.Instance.GetFoodProductionPercent() : 0f;

        // B25: FloorToInt so production seconds match resource days exactly; caller already floors `days`
        // the same way, computed defensively here too.
        int wholeDays = Mathf.FloorToInt(resourceDays);
        float partialDay = resourceDays - wholeDays;

        // Storm schedule is precomputed ahead of time for the whole winter, so future-day queries within
        // the raid are valid. Must be read before SeasonManager.AdvanceDays() shifts the window — same as
        // the previous simulator already did.
        var stormLookup = new Dictionary<int, float>();
        if (raidStartAbsoluteDay >= 0)
        {
            if (StormScheduler.Instance != null)
            {
                var stormDays = StormScheduler.Instance.GetStormDaysInRange(raidStartAbsoluteDay, raidStartAbsoluteDay + wholeDays);
                foreach (var (absDay, mult) in stormDays)
                    stormLookup[absDay] = mult;
            }
            else
            {
                Debug.LogWarning("SettlementSimulator: StormScheduler unavailable — firewood storm multiplier defaulting to 1.0");
            }
        }

        // Carries fractional production progress between simulated days, mirroring live
        // Building.productionProgress -= 100f overflow. Without this a per-day floor would discard more
        // precision than the old single end-of-period floor.
        var progressCarry = new Dictionary<Building, float>();

        // Local mutable copy of each equipment-queue building's (e.g. Blacksmith) queue, so the day loop
        // can drain it into report.craftedEquipment without touching the live Building.equipmentQueue -
        // the leftover is written back atomically via report.remainingEquipmentQueues at the end.
        var equipmentQueueSim = new Dictionary<Building, List<string>>();
        foreach (var b in buildingRoster.Keys)
        {
            if (b.HasEquipmentQueue)
                equipmentQueueSim[b] = new List<string>(b.equipmentQueue);
        }

        // ================= WHOLE-DAY LOOP =================
        for (int dayIndex = 0; dayIndex < wholeDays; dayIndex++)
        {
            int absDay = raidStartAbsoluteDay >= 0 ? raidStartAbsoluteDay + dayIndex : -1;
            SeasonManager.Season season = SeasonManager.Instance != null
                ? SeasonManager.Instance.GetSeasonForDayOffset(dayIndex)
                : SeasonManager.Season.Summer;
            ColdDayType coldDay = (absDay >= 0 && StormScheduler.Instance != null)
                ? StormScheduler.Instance.GetColdDayType(absDay)
                : ColdDayType.Chilly;
            float stormMul = (absDay >= 0 && stormLookup.TryGetValue(absDay, out float sm)) ? sm : 1f;

            // Snapshot of who's alive at the START of this day — production/events/wood/food all operate
            // on this same set; the death check at the end catches anyone whose health reached 0 today.
            var aliveStates = orderedStates.Where(s => !s.isDead).ToList();
            int population = aliveStates.Count;

            // ---- STEP A: PRODUCTION ----
            foreach (var kvp in buildingRoster)
            {
                Building building = kvp.Key;
                List<Villager> workers = kvp.Value;
                if (workers.Count == 0) continue;

                var data = building.data;
                if (data == null) continue;

                float baseRate = data.productionType == ProductionType.Crafting
                    ? data.craftingRecipe?.craftingRate ?? 0f
                    : data.productionRate;
                if (baseRate <= 0f) continue;

                float productionSpeed = 0f;
                foreach (var w in workers)
                {
                    var s = stateByVillager[w];
                    productionSpeed += baseRate * SettlementFormulas.GetSkillMultiplier(w.skills.GetSkillForJob(data.assignedJobType), s.morale);
                }
                productionSpeed *= productionSpeedMult; // Tireless Workers, applied once to the summed speed

                float carry = progressCarry.TryGetValue(building, out float c) ? c : 0f;
                carry += productionSpeed * dayLength;
                int completions = Mathf.FloorToInt(carry / 100f);
                carry -= completions * 100f;

                if (completions <= 0)
                {
                    progressCarry[building] = carry;
                    continue;
                }

                int achievedCompletions = completions;

                if (data.productionType == ProductionType.ResourceGathering && data.resourceOutputs != null && data.resourceOutputs.Count > 0)
                {
                    float seasonalMult = SeasonManager.Instance != null
                        ? SeasonManager.Instance.GetProductionMultiplierForDayOffset(data.buildingType, dayIndex)
                        : 1f;
                    foreach (var resourceOutput in data.resourceOutputs)
                    {
                        int perCompletion = SettlementFormulas.GetResourceGatheringOutputPerCompletion(
                            resourceOutput.resourceType, resourceOutput.amount, seasonalMult, gefjonActive, gefjonFoodPct);
                        report.resourceChanges[resourceOutput.resourceType] += perCompletion * completions;
                    }
                }
                else if (building.HasEquipmentQueue)
                {
                    achievedCompletions = DrainEquipmentQueue(building, data, equipmentQueueSim[building], completions, report);
                    if (equipmentQueueSim[building].Count == 0) carry = 0f; // nothing in progress to bank leftover progress into
                }
                else if (data.productionType == ProductionType.Crafting && data.craftingRecipe != null)
                {
                    // Crafting inputs assumed always available — matches original simulator's explicit
                    // "simplified" comment; out of scope to fix here.
                    int output = Mathf.FloorToInt(completions * data.craftingRecipe.outputAmount);
                    report.resourceChanges[data.craftingRecipe.outputResource] += output;
                    foreach (var input in data.craftingRecipe.inputResources)
                        report.resourceChanges[input.resourceType] -= input.amount * completions;
                }

                progressCarry[building] = carry;

                if (achievedCompletions > 0)
                {
                    foreach (var w in workers)
                        skillCompletionsAccrued[w] = skillCompletionsAccrued.GetValueOrDefault(w, 0) + achievedCompletions;
                }
            }

            // ---- STEP B: RANDOM EVENTS (per-villager identity) ----
            SimulateRandomEventsForDay(dayIndex + 1, aliveStates, report, producedResourceTypes);

            // ---- STEP C: WOOD / COLD ----
            float woodNeeded = SettlementFormulas.GetWoodCost(population, woodPerVillagerBase, season, coldDay, stormMul, woodConsumptionMult);
            if (woodNeeded > 0f)
            {
                // Available stock adjusted for production/consumption already resolved earlier THIS raid
                // (fixes the pre-raid-only-stock shortage bug).
                float woodAvailable = Mathf.Max(0f, GetAvailableResource(ResourceType.Wood) + report.resourceChanges[ResourceType.Wood]);
                if (woodAvailable >= woodNeeded)
                {
                    report.resourceChanges[ResourceType.Wood] -= woodNeeded;
                    foreach (var s in aliveStates)
                    {
                        s.isCold = false;
                        s.morale = Mathf.Clamp(s.morale + warmthMoraleBonus, 0, s.maxMorale);
                    }
                }
                else
                {
                    report.resourceChanges[ResourceType.Wood] -= woodAvailable;
                    int affectedCount = SettlementFormulas.GetColdAffectedCount(population, woodNeeded, woodAvailable);
                    for (int i = 0; i < Mathf.Min(affectedCount, aliveStates.Count); i++)
                    {
                        var s = aliveStates[i]; // first N in seed/registration order — matches live exactly
                        if (coldMoralePenalty > 0) s.morale = Mathf.Clamp(s.morale - coldMoralePenalty, 0, s.maxMorale);
                        if (coldHealthDamage > 0) { s.health -= coldHealthDamage; s.lastDamageWasCombat = false; }
                        s.isCold = true;
                    }

                    report.events.Add(new SettlementEvent
                    {
                        eventName = "Freezing Winter",
                        description = $"The settlement ran out of firewood on day {dayIndex + 1}. Villagers suffered from the cold.",
                        eventType = SettlementEventType.Negative
                    });
                }
            }

            // ---- STEP D: FOOD / HUNGER ----
            float effectiveFish = SettlementFormulas.GetEffectiveFoodPerVillager(fishPerVillagerBase, rationingModifier);
            float totalFishNeeded = SettlementFormulas.GetTotalFoodNeeded(population, effectiveFish);
            if (totalFishNeeded > 0f)
            {
                float fishAvailable = Mathf.Max(0f, GetAvailableResource(ResourceType.Fish) + report.resourceChanges[ResourceType.Fish]);
                if (fishAvailable >= totalFishNeeded)
                {
                    report.resourceChanges[ResourceType.Fish] -= totalFishNeeded;
                    foreach (var s in aliveStates)
                    {
                        ApplyFedHeal(s, gefjonActive, gefjonHealPct);
                        s.isHungry = false;
                    }
                }
                else
                {
                    report.resourceChanges[ResourceType.Fish] -= fishAvailable;

                    if (hungerMode == HungerDistributionMode.Shared)
                    {
                        float fraction = SettlementFormulas.GetSharedHungerFraction(fishAvailable, effectiveFish, population);
                        foreach (var s in aliveStates)
                        {
                            if (fraction <= 0f)
                            {
                                ApplyFedHeal(s, gefjonActive, gefjonHealPct);
                                s.isHungry = false;
                            }
                            else
                            {
                                var effect = SettlementFormulas.GetSharedHungerEffect(s.health, minStarvationDamage, fraction);
                                s.health -= effect.healthDamage;
                                s.morale = Mathf.Clamp(s.morale + effect.moraleDelta, 0, s.maxMorale);
                                s.isHungry = true;
                                s.lastDamageWasCombat = false;
                            }
                        }
                    }
                    else // Prioritized
                    {
                        int fedCount = SettlementFormulas.GetFedCount(fishAvailable, effectiveFish, population);
                        var shuffled = aliveStates.OrderBy(_ => Random.value).ToList(); // fresh shuffle each simulated day, matches live's per-mealtime shuffle
                        for (int i = 0; i < shuffled.Count; i++)
                        {
                            var s = shuffled[i];
                            if (i < fedCount)
                            {
                                ApplyFedHeal(s, gefjonActive, gefjonHealPct);
                                s.isHungry = false;
                            }
                            else
                            {
                                var effect = SettlementFormulas.GetPrioritizedHungerEffect(s.health, minStarvationDamage);
                                s.health -= effect.healthDamage;
                                s.morale = Mathf.Clamp(s.morale + effect.moraleDelta, 0, s.maxMorale);
                                s.isHungry = true;
                                s.lastDamageWasCombat = false;
                            }
                        }
                    }

                    report.events.Add(new SettlementEvent
                    {
                        eventName = "Food Shortage",
                        description = $"The settlement didn't have enough food on day {dayIndex + 1} while you were away.",
                        eventType = SettlementEventType.Negative
                    });
                }
            }

            // ---- STEP E: DEATH CHECK ----
            foreach (var s in aliveStates)
            {
                if (s.health <= 0f && !s.isDead)
                {
                    s.isDead = true;
                    s.deathCause = s.lastDamageWasCombat ? DeathCause.Combat
                        : s.isHungry ? DeathCause.Starvation
                        : s.isCold ? DeathCause.Cold
                        : DeathCause.Other;
                    // No OldAge — aging/old-age death is explicitly out of scope for this simulator.

                    // Simulator-local roster only — next day's production naturally uses the reduced list.
                    // Building.assignedWorkers (live) is never touched inside the loop.
                    foreach (var roster in buildingRoster.Values)
                        roster.Remove(s.liveRef);
                }
            }
        }

        // ================= PARTIAL DAY (resource deltas only — no events, no death, no hunger/cold health/morale) =================
        if (partialDay > 0f)
        {
            int dayIndex = wholeDays;
            int absDay = raidStartAbsoluteDay >= 0 ? raidStartAbsoluteDay + wholeDays : -1;
            SeasonManager.Season season = SeasonManager.Instance != null
                ? SeasonManager.Instance.GetSeasonForDayOffset(dayIndex)
                : SeasonManager.Season.Summer;
            ColdDayType coldDay = (absDay >= 0 && StormScheduler.Instance != null)
                ? StormScheduler.Instance.GetColdDayType(absDay)
                : ColdDayType.Chilly;
            float stormMul = (absDay >= 0 && stormLookup.TryGetValue(absDay, out float sm)) ? sm : 1f;

            var aliveStates = orderedStates.Where(s => !s.isDead).ToList();
            int population = aliveStates.Count;

            // Production, scaled by partialDay, using post-whole-day-loop skill/morale/roster state.
            foreach (var kvp in buildingRoster)
            {
                Building building = kvp.Key;
                List<Villager> workers = kvp.Value;
                if (workers.Count == 0) continue;

                var data = building.data;
                if (data == null) continue;

                float baseRate = data.productionType == ProductionType.Crafting
                    ? data.craftingRecipe?.craftingRate ?? 0f
                    : data.productionRate;
                if (baseRate <= 0f) continue;

                float productionSpeed = 0f;
                foreach (var w in workers)
                {
                    var s = stateByVillager[w];
                    productionSpeed += baseRate * SettlementFormulas.GetSkillMultiplier(w.skills.GetSkillForJob(data.assignedJobType), s.morale);
                }
                productionSpeed *= productionSpeedMult;

                float carry = progressCarry.TryGetValue(building, out float c) ? c : 0f;
                carry += productionSpeed * dayLength * partialDay;
                int completions = Mathf.FloorToInt(carry / 100f);
                // Final pass — no need to persist the remainder further.

                if (completions <= 0) continue;

                int achievedCompletions = completions;

                if (data.productionType == ProductionType.ResourceGathering && data.resourceOutputs != null && data.resourceOutputs.Count > 0)
                {
                    float seasonalMult = SeasonManager.Instance != null
                        ? SeasonManager.Instance.GetProductionMultiplierForDayOffset(data.buildingType, dayIndex)
                        : 1f;
                    foreach (var resourceOutput in data.resourceOutputs)
                    {
                        int perCompletion = SettlementFormulas.GetResourceGatheringOutputPerCompletion(
                            resourceOutput.resourceType, resourceOutput.amount, seasonalMult, gefjonActive, gefjonFoodPct);
                        report.resourceChanges[resourceOutput.resourceType] += perCompletion * completions;
                    }
                }
                else if (building.HasEquipmentQueue)
                {
                    achievedCompletions = DrainEquipmentQueue(building, data, equipmentQueueSim[building], completions, report);
                }
                else if (data.productionType == ProductionType.Crafting && data.craftingRecipe != null)
                {
                    int output = Mathf.FloorToInt(completions * data.craftingRecipe.outputAmount);
                    report.resourceChanges[data.craftingRecipe.outputResource] += output;
                    foreach (var input in data.craftingRecipe.inputResources)
                        report.resourceChanges[input.resourceType] -= input.amount * completions;
                }

                if (achievedCompletions > 0)
                {
                    foreach (var w in workers)
                        skillCompletionsAccrued[w] = skillCompletionsAccrued.GetValueOrDefault(w, 0) + achievedCompletions;
                }
            }

            // Wood — deduct only, capped at what's available.
            float woodNeeded = SettlementFormulas.GetWoodCost(population, woodPerVillagerBase, season, coldDay, stormMul, woodConsumptionMult) * partialDay;
            if (woodNeeded > 0f)
            {
                float woodAvailable = Mathf.Max(0f, GetAvailableResource(ResourceType.Wood) + report.resourceChanges[ResourceType.Wood]);
                report.resourceChanges[ResourceType.Wood] -= Mathf.Min(woodNeeded, woodAvailable);
            }

            // Food — deduct only, capped at what's available.
            float effectiveFish = SettlementFormulas.GetEffectiveFoodPerVillager(fishPerVillagerBase, rationingModifier);
            float totalFishNeeded = SettlementFormulas.GetTotalFoodNeeded(population, effectiveFish) * partialDay;
            if (totalFishNeeded > 0f)
            {
                float fishAvailable = Mathf.Max(0f, GetAvailableResource(ResourceType.Fish) + report.resourceChanges[ResourceType.Fish]);
                report.resourceChanges[ResourceType.Fish] -= Mathf.Min(totalFishNeeded, fishAvailable);
            }
        }

        // Hand back each equipment-queue building's leftover queue so the caller can write it back to the
        // live Building.equipmentQueue in one atomic pass (RaidManager.ApplySettlementReport).
        foreach (var kvp in equipmentQueueSim)
            report.remainingEquipmentQueues[kvp.Key.uniqueId] = kvp.Value;

        // ================= BUILD REPORT (still pure — no live mutation) =================
        foreach (var s in orderedStates)
        {
            report.villagerOutcomes.Add(new VillagerOutcome
            {
                villagerId = s.liveRef.uniqueId,
                died = s.isDead,
                deathCause = s.deathCause,
                healthDelta = s.isDead ? 0f : (s.health - s.startHealth),
                moraleDelta = s.isDead ? 0f : (s.morale - s.startMorale)
            });
        }
        foreach (var kvp in skillCompletionsAccrued)
        {
            if (stateByVillager[kvp.Key].isDead) continue;
            report.skillGains.Add(new SkillGain
            {
                villagerId = kvp.Key.uniqueId,
                jobType = stateByVillager[kvp.Key].jobType,
                completions = kvp.Value
            });
        }

        return report;
    }

    /// <summary>
    /// Mirrors Villager.HealFromFood — applies Gefjon's Blessing heal-speed bonus, heals, restores morale.
    /// </summary>
    private static void ApplyFedHeal(VillagerSimState s, bool gefjonActive, float gefjonHealPct)
    {
        var (healthGain, moraleGain) = SettlementFormulas.GetFoodHealEffect(
            s.liveRef.healthRegenFromFood, s.liveRef.moraleRegenFromFood, gefjonActive, gefjonHealPct);
        s.health = Mathf.Min(s.maxHealth, s.health + healthGain);
        s.morale = Mathf.Clamp(s.morale + moraleGain, 0, s.maxMorale);
    }

    /// <summary>
    /// Random events for a single simulated day, applied directly to per-villager sim state so casualties
    /// and morale changes carry into the rest of that same day's (and subsequent days') processing.
    /// </summary>
    private static void SimulateRandomEventsForDay(int dayNumber, List<VillagerSimState> aliveStates, SettlementReport report, HashSet<ResourceType> producedResourceTypes)
    {
        // Wolf attack — one random victim takes the full damage.
        if (aliveStates.Count > 0 && Random.value < WOLF_ATTACK_CHANCE)
        {
            var victim = aliveStates[Random.Range(0, aliveStates.Count)];
            victim.health -= Random.Range(10f, 25f);
            victim.lastDamageWasCombat = true;

            report.events.Add(new SettlementEvent
            {
                eventName = "Wolf Attack",
                description = $"Wolves attacked the settlement on day {dayNumber}. Some villagers were injured.",
                eventType = SettlementEventType.Negative
            });
        }

        // Good harvest bonus — resource-only, restricted to whichever of Wheat/Fish the settlement is
        // actually gathering this raid. No staffed farm/fishing dock means no harvest to bless.
        if (Random.value < GOOD_HARVEST_CHANCE)
        {
            var harvestCandidates = new List<ResourceType>();
            if (producedResourceTypes.Contains(ResourceType.Wheat)) harvestCandidates.Add(ResourceType.Wheat);
            if (producedResourceTypes.Contains(ResourceType.Fish)) harvestCandidates.Add(ResourceType.Fish);

            if (harvestCandidates.Count > 0)
            {
                ResourceType bonusType = harvestCandidates[Random.Range(0, harvestCandidates.Count)];
                int bonusAmount = Mathf.FloorToInt(Random.Range(5f, 15f));
                report.resourceChanges[bonusType] += bonusAmount;

                report.events.Add(new SettlementEvent
                {
                    eventName = "Bountiful Harvest",
                    description = $"The gods blessed the settlement with extra {bonusType} on day {dayNumber}.",
                    eventType = SettlementEventType.Positive
                });
            }
        }

        // Villager gets sick — one random victim, non-combat damage.
        if (aliveStates.Count > 0 && Random.value < VILLAGER_SICK_CHANCE)
        {
            var victim = aliveStates[Random.Range(0, aliveStates.Count)];
            victim.health -= Random.Range(5f, 15f);
            victim.lastDamageWasCombat = false;

            report.events.Add(new SettlementEvent
            {
                eventName = "Illness",
                description = $"A villager fell ill on day {dayNumber}.",
                eventType = SettlementEventType.Negative
            });
        }

        // Trader visit — resource-only.
        if (Random.value < TRADER_VISIT_CHANCE)
        {
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

        // Good spirits / morale boost — applies to everyone alive that day.
        if (aliveStates.Count > 0 && Random.value < HIGH_SPIRITS_CHANCE)
        {
            foreach (var s in aliveStates)
                s.morale = Mathf.Clamp(s.morale + 5f, 0, s.maxMorale);

            report.events.Add(new SettlementEvent
            {
                eventName = "High Spirits",
                description = $"The villagers celebrated together on day {dayNumber}.",
                eventType = SettlementEventType.Positive
            });
        }
    }

    #region Helpers

    /// <summary>
    /// Drain up to <paramref name="completions"/> items off the front of an equipment-queue building's
    /// local queue copy (one full 0-100 cycle per item, same as live Building.UpdateEquipmentCrafting),
    /// recording each finished item into report.craftedEquipment and deducting its input cost. Stale
    /// entries (item no longer in data.craftableEquipment) are dropped without consuming a completion.
    /// Returns how many items actually finished — may be less than `completions` if the queue ran dry.
    /// </summary>
    private static int DrainEquipmentQueue(Building building, BuildingData data, List<string> queue, int completions, SettlementReport report)
    {
        int used = 0;
        while (used < completions && queue.Count > 0)
        {
            EquipmentRecipe recipe = data.craftableEquipment.Find(r => r.itemName == queue[0]);
            if (recipe == null)
            {
                queue.RemoveAt(0); // stale entry, drop and move on without spending a completion
                continue;
            }

            // Inputs assumed always available — same simplification as the legacy craftingRecipe path.
            foreach (var input in recipe.inputResources)
                report.resourceChanges[input.resourceType] -= input.amount;

            report.craftedEquipment.Add(new CraftedEquipmentEntry { buildingId = building.uniqueId, itemName = recipe.itemName });
            queue.RemoveAt(0);
            used++;
        }
        return used;
    }

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
            ResourceType.Shield
        };

        return tradeResources[Random.Range(0, tradeResources.Length)];
    }

    #endregion
}
