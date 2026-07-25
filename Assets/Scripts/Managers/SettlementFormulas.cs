using UnityEngine;

/// <summary>
/// Pure math shared by live gameplay code (SettlementManager, Villager, Building, SeasonManager) and
/// SettlementSimulator. No singleton reads, no UI/coroutine calls, no mutation — every input is an
/// explicit parameter so the exact same formula can run against live state or simulated state.
/// </summary>
public static class SettlementFormulas
{
    // Mirrors Villager.GetSkillMultiplier
    public static float GetSkillMultiplier(float baseSkill, float morale)
        => (1f + baseSkill * 0.2f) * (morale / 100f);

    // Mirrors Building.CalculateBuildingCompletions / GetProductionSpeed*periodSeconds/100 progress fill
    public static int CalculateCompletions(float productionSpeed, float periodSeconds)
        => Mathf.FloorToInt(productionSpeed * periodSeconds / 100f);

    /// <summary>
    /// Mirrors Building.UpdateResourceGathering's adjustedProductionAmount calc (per-day/per-completion
    /// seasonal adjustment, floored at 1 whenever the season multiplier is positive).
    /// </summary>
    public static float GetAdjustedProductionAmount(float productionAmount, float seasonalMultiplier)
        => Mathf.Max(seasonalMultiplier > 0 ? 1 : 0, Mathf.Round(productionAmount * seasonalMultiplier));

    /// <summary>
    /// Mirrors CompleteResourceGathering's Gefjon's Blessing food-production bonus — applied to an
    /// already-adjusted amount, only for Fish/Wheat output. Kept separate from
    /// GetAdjustedProductionAmount so it can be applied exactly once to a value that may already be
    /// seasonally adjusted (live) or freshly computed (simulator), without re-clamping.
    /// </summary>
    public static float ApplyGefjonFoodBonus(ResourceType producedResource, float amount, bool gefjonActive, float gefjonFoodProductionPercent)
    {
        if ((producedResource == ResourceType.Fish || producedResource == ResourceType.Wheat) && gefjonActive)
            amount *= (1f + gefjonFoodProductionPercent / 100f);
        return amount;
    }

    /// <summary>
    /// Full per-completion resource-gathering output: seasonal adjustment + Gefjon's Blessing, rounded once.
    /// </summary>
    public static int GetResourceGatheringOutputPerCompletion(
        ResourceType producedResource, float productionAmount, float seasonalMultiplier,
        bool gefjonActive, float gefjonFoodProductionPercent)
    {
        float amount = GetAdjustedProductionAmount(productionAmount, seasonalMultiplier);
        amount = ApplyGefjonFoodBonus(producedResource, amount, gefjonActive, gefjonFoodProductionPercent);
        return Mathf.RoundToInt(amount);
    }

    // Mirrors SettlementManager.GetTodayWoodCost
    public static float GetSeasonWoodMultiplier(SeasonManager.Season season)
        => season == SeasonManager.Season.Winter ? 1.0f : 0.5f;

    public static float GetColdDayWoodMultiplier(ColdDayType coldDay) => coldDay switch
    {
        ColdDayType.Frozen => 2.5f,
        ColdDayType.Cold   => 1.5f,
        _                  => 1.0f
    };

    public static float GetWoodCost(int population, float woodPerVillagerPerDay,
        SeasonManager.Season season, ColdDayType coldDay, float stormMultiplier, float runestoneMultiplier)
        => population * woodPerVillagerPerDay
           * GetSeasonWoodMultiplier(season) * GetColdDayWoodMultiplier(coldDay)
           * stormMultiplier * runestoneMultiplier;

    /// <summary>
    /// Mirrors SettlementManager.ApplyColdEffects. Truncating cast, NOT FloorToInt/Round — matches live
    /// behavior exactly (only matters for negative percentageCold, which can't happen here, but keeping
    /// the identical operator avoids any subtle divergence).
    /// </summary>
    public static int GetColdAffectedCount(int villagerCount, float woodNeededToday, float woodConsumedToday)
    {
        float percentageCold = woodNeededToday > 0 ? 1f - (woodConsumedToday / woodNeededToday) : 0f;
        return (int)(villagerCount * percentageCold);
    }

    // Mirrors SettlementManager.HandleMealTime
    public static float GetEffectiveFishPerVillager(float fishPerVillagerPerDay, float rationingModifier)
        => Mathf.Max(0f, fishPerVillagerPerDay + rationingModifier);

    public static float GetTotalFishNeeded(int villagerCount, float effectiveFishPerVillager)
        => villagerCount * effectiveFishPerVillager;

    // Mirrors ApplyPrioritizedHunger's fedCount calc
    public static int GetFedCount(float availableFish, float fishPerVillager, int villagerCount)
        => fishPerVillager > 0 ? Mathf.FloorToInt(availableFish / fishPerVillager) : villagerCount;

    public readonly struct HungerEffect
    {
        public readonly float healthDamage;
        public readonly float moraleDelta;
        public HungerEffect(float healthDamage, float moraleDelta)
        {
            this.healthDamage = healthDamage;
            this.moraleDelta = moraleDelta;
        }
    }

    // Mirrors Villager.ReduceHealthAndMoraleDueToHunger (Prioritized mode, not-fed path)
    public static HungerEffect GetPrioritizedHungerEffect(float currentHealth, float minStarvationDamage)
    {
        int minDmg = Mathf.FloorToInt(minStarvationDamage);
        int dmg = Mathf.Max(Mathf.FloorToInt(currentHealth / 2), minDmg);
        return new HungerEffect(dmg, -10f);
    }

    // Mirrors SettlementManager.ApplySharedHunger fraction calc. 0 = fully fed, 1 = fully starved.
    public static float GetSharedHungerFraction(float availableFish, float fishPerVillager, int villagerCount)
    {
        if (fishPerVillager <= 0f || villagerCount == 0) return 0f;
        float foodPerVillager = availableFish / villagerCount;
        return Mathf.Clamp01((fishPerVillager - foodPerVillager) / fishPerVillager);
    }

    // Mirrors Villager.HandleSharedHunger (hungerFraction > 0 branch)
    public static HungerEffect GetSharedHungerEffect(float currentHealth, float minStarvationDamage, float hungerFraction)
    {
        float fullDamage = Mathf.Max(currentHealth / 2f, minStarvationDamage);
        return new HungerEffect(fullDamage * hungerFraction, -10f * hungerFraction);
    }

    // Mirrors Villager.TryReproduce morale scaling. Couple's average morale of 100 leaves the cooldown
    // unchanged; lower morale stretches it out (less frequent births), floored so a miserable couple can
    // still eventually have a child rather than being locked out entirely.
    public static float GetMoraleBirthRateMultiplier(float averageMorale)
        => Mathf.Clamp(averageMorale / 100f, 0.2f, 1f);

    // Mirrors Villager.HealFromFood (fed path, both hunger modes)
    public static (float healthGain, float moraleGain) GetFoodHealEffect(
        float healthRegenFromFood, float moraleRegenFromFood, bool gefjonBlessingActive, float gefjonHealSpeedPercent)
    {
        float health = healthRegenFromFood;
        if (gefjonBlessingActive)
            health *= (1f + gefjonHealSpeedPercent / 100f);
        return (health, moraleRegenFromFood);
    }
}
