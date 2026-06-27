using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages seasonal changes and their visual effects
/// </summary>
public class SeasonManager : MonoBehaviour, ISaveable
{
    public static SeasonManager Instance { get; private set; }

    [Header("Season Settings")]
    [Tooltip("Number of days per season. Age 1 default = 30. Will be driven by Age progression in future.")]
    public int daysPerSeason = 30;

    [Tooltip("Current season")]
    [SerializeField] private Season currentSeason = Season.Summer;

    [Tooltip("Days remaining in current season")]
    [SerializeField] private int daysUntilSeasonChange;

    [Header("Summer Effects")]
    [Tooltip("Parent object containing all sun beam lights")]
    public GameObject summerSunBeamsParent;
    public ParticleSystem sunDustParticleSystem;
    public ParticleSystem fireFliesParticleSystem;

    [Tooltip("Individual sun beam lights (will auto-populate from parent if empty)")]
    public List<Light2D> summerSunBeams = new List<Light2D>();

    [Tooltip("Intensity of sun beams during summer")]
    public float sunBeamIntensity = 0.8f;

    [Tooltip("Should sun beams flicker/animate?")]
    public bool animateSunBeams = true;

    [Tooltip("Speed of sun beam animation")]
    public float sunBeamAnimationSpeed = 2f;

    [Header("Winter Effects")]
    [Tooltip("Snow particle system")]
    public ParticleSystem snowParticleSystem;

    [Tooltip("Should snow intensity vary with time of day?")]
    public bool varySnowWithTimeOfDay = true;

    [Tooltip("Base emission rate for snow particles")]
    public float baseSnowEmissionRate = 50f;

    [Tooltip("Maximum emission rate for snow particles")]
    public float maxSnowEmissionRate = 100f;

    [Header("Season Transition")]
    [Tooltip("How long the transition between seasons takes (in seconds)")]
    public float transitionDuration = 5f;

    [SerializeField] private bool isTransitioning = false;
    [SerializeField] private float transitionProgress = 0f;

    [Header("Lighting Adjustments")]
    [Tooltip("Multiplier for ambient light during summer")]
    public float summerAmbientMultiplier = 1.2f;

    [Tooltip("Multiplier for ambient light during winter")]
    public float winterAmbientMultiplier = 0.8f;

    [Header("Production Multipliers - Summer")]
    [Tooltip("Farm production in summer (1.0 = 100%)")]
    public float summerFarmMultiplier = 1.0f;
    [Tooltip("Fishing production in summer")]
    public float summerFishingMultiplier = 0.8f;
    [Tooltip("Lumber production in summer")]
    public float summerLumberMultiplier = 1.0f;

    [Header("Production Multipliers - Winter")]
    [Tooltip("Farm production in winter (crops struggle)")]
    public float winterFarmMultiplier = 0.25f;
    [Tooltip("Fishing production in winter (ice fishing)")]
    public float winterFishingMultiplier = 1.2f;
    [Tooltip("Lumber production in winter (harder in snow)")]
    public float winterLumberMultiplier = 0.6f;

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

    [Tooltip("Sun color tint during winter")]
    public Color winterSunTint = new Color(0.9f, 0.95f, 1f);

    [Tooltip("Sun color tint during summer")]
    public Color summerSunTint = new Color(1f, 1f, 0.95f);

    public FireController fireController; // Reference to the FireController to toggle fire effects based on warmth

    // Events
    public event Action<Season> OnSeasonChanged;
    public event Action<bool> OnWarmthChanged; // true = warm, false = cold

    // Private variables for animation
    private float sunBeamAnimationTime = 0f;
    private ParticleSystem.EmissionModule snowEmission;

    private int currentSolarYear = 1;

    public enum Season
    {
        Summer,
        Winter
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-populate sun beams from parent if not manually assigned
        if (summerSunBeamsParent != null && summerSunBeams.Count == 0)
        {
            Light2D[] beams = summerSunBeamsParent.GetComponentsInChildren<Light2D>();
            summerSunBeams.AddRange(beams);
        }

        // Get snow emission module if particle system exists
        if (snowParticleSystem != null)
        {
            snowEmission = snowParticleSystem.emission;
        }
    }

    public void Initialize()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay += OnNewDay;
        else
            Debug.LogWarning("SeasonManager: DayNightManager not found during Initialize!");

        if (GameTickManager.Instance != null)
            GameTickManager.Instance.OnFastUpdate += FastUpdate;
        else
            Debug.LogWarning("SeasonManager: GameTickManager not found during Initialize!");

        ApplySeasonEffects(currentSeason);

        if (fireController != null)
            fireController.Setup();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNewDay -= OnNewDay;
        }

        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnFastUpdate -= FastUpdate;
        }
    }

    private void OnNewDay()
    {
        daysUntilSeasonChange--;

        Debug.Log($"Days until season change: {daysUntilSeasonChange}");

        // Handle winter warmth/firewood consumption
        if (currentSeason == Season.Winter)
        {
            ConsumeFirewood();
        }
        else
        {
            // Always warm in summer
            SetWarmthStatus(true);
        }

        // Check if it's time to change seasons
        if (daysUntilSeasonChange <= 0)
        {
            ChangeSeason();
        }
    }

    /// <summary>
    /// Consume firewood to keep the settlement warm during winter
    /// </summary>
    private void ConsumeFirewood()
    {
        if (SettlementManager.Instance == null || ResourceManager.Instance == null)
        {
            SetWarmthStatus(true);
            return;
        }

        // Calculate wood needed based on population
        int   villagerCount    = SettlementManager.Instance.GetPopulation();
        // Summer burns at half rate — fires still needed but less fierce
        float seasonMultiplier = (currentSeason == Season.Winter) ? 1.0f : 0.5f;
        float stormMultiplier  = StormScheduler.Instance != null
            ? StormScheduler.Instance.GetCurrentDayWoodMultiplier()
            : 1.0f;
        woodNeededToday = villagerCount * woodPerVillagerPerDay * seasonMultiplier * stormMultiplier;

        // Apply Winter's Friend runestone bonus (-30% firewood)
        if (RunestoneManager.Instance != null)
        {
            woodNeededToday *= RunestoneManager.Instance.GetFirewoodConsumptionMultiplier();
        }

        // Try to consume wood
        float availableWood = ResourceManager.Instance.GetResource(ResourceType.Wood);

        if (availableWood >= woodNeededToday)
        {
            // Enough wood - stay warm
            ResourceManager.Instance.SpendResource(ResourceType.Wood, woodNeededToday);
            woodConsumedToday = woodNeededToday;
            SetWarmthStatus(true);
            ApplyWarmEffects();
            Debug.Log($"Winter heating: Consumed {woodNeededToday:F1} wood for {villagerCount} villagers. Settlement is warm.");
        }
        else
        {
            // Not enough wood - consume what we have but settlement is cold
            ResourceManager.Instance.SpendResource(ResourceType.Wood, availableWood);
            woodConsumedToday = availableWood;
            SetWarmthStatus(false);
            ApplyColdEffects();
            Debug.LogWarning($"Winter heating: Only {availableWood:F1} wood available, needed {woodNeededToday:F1}. Settlement is COLD!");
        }
    }

    private void ApplyWarmEffects()
    {
        if (SettlementManager.Instance == null) return;

        var villagers = SettlementManager.Instance.GetAllVillagers();

        foreach (var villager in villagers)
        {
            if (villager == null || villager.IsDead()) continue;

            // Remove cold status
            villager.isCold = false;
            villager.ChangeMorale(warmthMoraleBonus); 
        }

        Debug.Log("Settlement is warm. No negative effects applied.");
    }

    /// <summary>
    /// Apply negative effects when settlement is cold
    /// </summary>
    private void ApplyColdEffects()
    {
        if (SettlementManager.Instance == null) return;

        var villagers = SettlementManager.Instance.GetAllVillagers();

        float villagersEffected = 0;
        float percentageCold = 0f;
        if (woodNeededToday > 0)
        {
            percentageCold = 1f - (woodConsumedToday / woodNeededToday);
        }
        villagersEffected = villagers.Count * percentageCold;

        for(int i = 0; i < (int)villagersEffected; i++)
        {
            var villager = villagers[i];
            if (villager == null || villager.IsDead()) continue;

            // Apply morale penalty
            if (coldMoralePenalty > 0)
            {
                villager.ChangeMorale(-coldMoralePenalty);
            }

            // Apply health damage if enabled (true damage - cold bypasses armor)
            if (coldHealthDamage > 0)
            {
                villager.TakeDamage(coldHealthDamage, null, true);
            }

            villager.personalUI.ShowSpeech("I'm freezing!", 2.0f);
            villager.isCold = true;
            villager.personalUI.UpdateStatusEffectIcon(VillagerStatusEffect.Cold);
        }

        Debug.Log($"Cold effects applied: -{coldMoralePenalty} morale{(coldHealthDamage > 0 ? $", -{coldHealthDamage} health" : "")} to all villagers");
    }

    /// <summary>
    /// Update warmth status and fire event if changed
    /// </summary>
    private void SetWarmthStatus(bool isWarm)
    {
        if (isSettlementWarm != isWarm)
        {
            isSettlementWarm = isWarm;
            OnWarmthChanged?.Invoke(isWarm);
        }
    }

    private void FastUpdate()
    {
        // Handle season transition
        if (isTransitioning)
        {
            transitionProgress += Time.deltaTime / transitionDuration;

            if (transitionProgress >= 1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
            }
        }

        // Update current season effects
        UpdateSeasonEffects();
    }

    private void UpdateSeasonEffects()
    {
        var sunTint = Color.white;
        switch (currentSeason)
        {
            case Season.Summer:
                UpdateSummerEffects();
                sunTint = summerSunTint;
                break;
            case Season.Winter:
                UpdateWinterEffects();
                sunTint = winterSunTint;
                break;
        }

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.sunColor = Color.Lerp(
                DayNightManager.Instance.sunColor,
                sunTint,
                0.5f * Time.deltaTime);
        }
    }

    private void UpdateSummerEffects()
    {
        if (summerSunBeams.Count == 0) return;

        // Animate sun beams if enabled
        if (animateSunBeams)
        {
            sunBeamAnimationTime += Time.deltaTime * sunBeamAnimationSpeed;

            for (int i = 0; i < summerSunBeams.Count; i++)
            {
                if (summerSunBeams[i] == null) continue;

                // Create a slightly different animation offset for each beam
                float offset = i * 0.5f;
                float animatedIntensity = sunBeamIntensity * 
                    (0.7f + 0.3f * Mathf.Sin(sunBeamAnimationTime + offset));

                // Only show sun beams during daytime
                if (DayNightManager.Instance != null)
                {
                    float timeOfDay = DayNightManager.Instance.GetTimeOfDay();
                    bool isDaytime = timeOfDay >= 0.25f && timeOfDay <= 0.75f;

                    if (isDaytime)
                    {
                        summerSunBeams[i].intensity = animatedIntensity;
                        if(!sunDustParticleSystem.isPlaying)
                        {
                            sunDustParticleSystem.Play();
                        }
                        fireFliesParticleSystem.Stop();
                    }
                    else
                    {
                        summerSunBeams[i].intensity = 0f;
                        sunDustParticleSystem.Stop();
                        if(!fireFliesParticleSystem.isPlaying)
                        {
                            fireFliesParticleSystem.Play();
                        }
                    }
                }
                else
                {
                    summerSunBeams[i].intensity = animatedIntensity;
                }
            }
        }
    }

    private void UpdateWinterEffects()
    {
        if (snowParticleSystem == null) return;

        // Vary snow intensity with time of day if enabled
        if (varySnowWithTimeOfDay && DayNightManager.Instance != null)
        {
            float timeOfDay = DayNightManager.Instance.GetTimeOfDay();
            
            // Snow falls heavier at night
            float snowIntensity;
            if (timeOfDay < 0.5f)
            {
                // Morning to noon - lighter snow
                snowIntensity = Mathf.Lerp(maxSnowEmissionRate, baseSnowEmissionRate, timeOfDay * 2f);
            }
            else
            {
                // Noon to night - heavier snow
                snowIntensity = Mathf.Lerp(baseSnowEmissionRate, maxSnowEmissionRate, (timeOfDay - 0.5f) * 2f);
            }

            snowEmission.rateOverTime = snowIntensity;
        }
    }

    private void ChangeSeason()
    {
        // Toggle between summer and winter
        Season newSeason = currentSeason == Season.Summer ? Season.Winter : Season.Summer;

        Debug.Log($"Season changing from {currentSeason} to {newSeason}");

        // Start transition
        isTransitioning = true;
        transitionProgress = 0f;

        // Apply new season effects
        ApplySeasonEffects(newSeason);

        currentSeason = newSeason;
        daysUntilSeasonChange = daysPerSeason;

        // Trigger event
        OnSeasonChanged?.Invoke(currentSeason);

        // Increment solar year if transitioning from Winter to Summer
        if (currentSeason == Season.Summer)
        {            
            currentSolarYear++;
        }
    }

    private void ApplySeasonEffects(Season season)
    {
        switch (season)
        {
            case Season.Summer:
                EnableSummerEffects(true);
                EnableWinterEffects(false);
                ApplySummerLighting();
                break;
            case Season.Winter:
                EnableWinterEffects(true);
                EnableSummerEffects(false);
                ApplyWinterLighting();
                break;
        }
    }

    private void EnableSummerEffects(bool value)
    {
        // Enable sun beams
        if (summerSunBeamsParent != null)
        {
            summerSunBeamsParent.SetActive(value);
        }

        foreach (var beam in summerSunBeams)
        {
            if (beam != null)
            {
                beam.enabled = value;
            }
        }

        if (sunDustParticleSystem != null)
        {
            if (value && !sunDustParticleSystem.isPlaying)
            {
                sunDustParticleSystem.Play();
            }
            else if (!value && sunDustParticleSystem.isPlaying)
            {
                sunDustParticleSystem.Stop();
            }
        }

        if (fireFliesParticleSystem != null)
        {
            if (!value && !fireFliesParticleSystem.isPlaying)
            {
                fireFliesParticleSystem.Play();
            }
            else if (value && fireFliesParticleSystem.isPlaying)
            {
                fireFliesParticleSystem.Stop();
            }
        }

        Debug.Log($"Summer effects enabled : {value}");
    }

    private void EnableWinterEffects(bool value)
    {
        // Enable snow particles
        if (snowParticleSystem != null)
        {
            snowParticleSystem.gameObject.SetActive(value);
            if (value && !snowParticleSystem.isPlaying)
            {
                snowParticleSystem.Play();
            }
            else if (!value && snowParticleSystem.isPlaying)
            {
                snowParticleSystem.Stop();
            }
            snowEmission.rateOverTime = baseSnowEmissionRate;
        }

        Debug.Log($"Winter effects enabled : {value}");
    }

    private void ApplySummerLighting()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.eveningMultiplier = summerAmbientMultiplier;
            
        }
    }

    private void ApplyWinterLighting()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.eveningMultiplier = winterAmbientMultiplier;
        }
    }

    // Public API Methods

    /// <summary>
    /// Get the current season
    /// </summary>
    public Season GetCurrentSeason()
    {
        return currentSeason;
    }

    /// <summary>
    /// Get days remaining until next season change
    /// </summary>
    public int GetDaysUntilSeasonChange()
    {
        return daysUntilSeasonChange;
    }

    /// <summary>
    /// Advance season tracking by N days without firing per-day events — used by raid return.
    /// Firewood consumption is already handled by SettlementSimulator for the elapsed period.
    /// </summary>
    public void AdvanceDays(int days)
    {
        int remaining = days;
        while (remaining > 0)
        {
            if (daysUntilSeasonChange <= 0)
                ChangeSeason(); // resets daysUntilSeasonChange to daysPerSeason

            int step = Mathf.Min(remaining, daysUntilSeasonChange);
            daysUntilSeasonChange -= step;
            remaining -= step;

            if (daysUntilSeasonChange <= 0)
                ChangeSeason();
        }
        Debug.Log($"SeasonManager: Advanced {days} days — now {currentSeason}, {daysUntilSeasonChange} days until season change");
    }

    /// <summary>
    /// Force a season change (for debugging/testing)
    /// </summary>
    public void ForceSeasonChange()
    {
        ChangeSeason();
    }

    /// <summary>
    /// Set a specific season (for debugging/testing)
    /// </summary>
    public void SetSeason(Season season)
    {
        if (season != currentSeason)
        {
            currentSeason = season;
            ApplySeasonEffects(season);
            daysUntilSeasonChange = daysPerSeason;
        }
    }

    /// <summary>
    /// Check if currently transitioning between seasons
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// Get the current transition progress (0-1)
    /// </summary>
    public float GetTransitionProgress()
    {
        return transitionProgress;
    }

    /// <summary>
    /// Get the current solar year
    /// </summary>
    public int GetCurrentSolarYear()
    {
        return currentSolarYear;
    }

    /// <summary>
    /// Get the production multiplier for a building type based on current season
    /// </summary>
    public float GetProductionMultiplier(BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Farm:
                return currentSeason == Season.Summer ? summerFarmMultiplier : winterFarmMultiplier;

            case BuildingType.FishermansHut:
                return currentSeason == Season.Summer ? summerFishingMultiplier : winterFishingMultiplier;

            case BuildingType.LumberCamp:
                return currentSeason == Season.Summer ? summerLumberMultiplier : winterLumberMultiplier;

            // Indoor/underground work is unaffected by seasons
            case BuildingType.Sawmill:
            case BuildingType.Quarry:
            case BuildingType.Mine:
            case BuildingType.Blacksmith:
            case BuildingType.CarpenterWorkshop:
            case BuildingType.WeaversHut:
            case BuildingType.Tannery:
            case BuildingType.Barracks:
            case BuildingType.ArcheryRange:
            case BuildingType.TradingPost:
            case BuildingType.HealersHut:
            case BuildingType.ShamansHut:
            case BuildingType.MeadHall:
            case BuildingType.Longhouse:
            case BuildingType.Shipyard:
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// Get a description of the seasonal effect for a building (for UI tooltips)
    /// </summary>
    public string GetSeasonalEffectDescription(BuildingType buildingType)
    {
        float multiplier = GetProductionMultiplier(buildingType);

        if (Mathf.Approximately(multiplier, 1.0f))
            return "";

        int percentage = Mathf.RoundToInt(multiplier * 100f);
        string seasonName = currentSeason.ToString();

        if (multiplier < 1.0f)
            return $"{seasonName}: {percentage}% production (reduced)";
        else
            return $"{seasonName}: {percentage}% production (bonus)";
    }

    #region Warmth System API

    /// <summary>
    /// Check if the settlement is currently warm
    /// </summary>
    public bool IsSettlementWarm()
    {
        return isSettlementWarm;
    }

    /// <summary>
    /// Get the amount of wood needed per day in winter
    /// </summary>
    public float GetWoodNeededPerDay()
    {
        if (currentSeason != Season.Winter) return 0f;

        if (SettlementManager.Instance != null)
        {
            return SettlementManager.Instance.GetPopulation() * woodPerVillagerPerDay;
        }
        return woodNeededToday;
    }

    /// <summary>
    /// Get how much wood was consumed today
    /// </summary>
    public float GetWoodConsumedToday()
    {
        return woodConsumedToday;
    }

    /// <summary>
    /// Returns today's effective wood cost without triggering consumption.
    /// Accounts for season (half rate in summer) and active storm multiplier.
    /// Safe to call from UI and WeatherManager at any time.
    /// </summary>
    public float GetTodayWoodCost()
    {
        if (SettlementManager.Instance == null) return 0f;
        int population = SettlementManager.Instance.GetPopulation();
        float seasonMultiplier = (currentSeason == Season.Winter) ? 1.0f : 0.5f;
        float stormMultiplier = StormScheduler.Instance != null
            ? StormScheduler.Instance.GetCurrentDayWoodMultiplier()
            : 1.0f;
        return population * woodPerVillagerPerDay * seasonMultiplier * stormMultiplier;
    }

    /// <summary>
    /// Get warmth status description for UI
    /// </summary>
    public string GetWarmthStatusText()
    {
        if (currentSeason != Season.Winter)
            return "Warm (Summer)";

        if (isSettlementWarm)
            return "Warm (Heated)";
        else
            return "COLD - Need more firewood!";
    }

    #endregion

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        if (data.gameState == null)
            data.gameState = new GameStateSave();

        data.gameState.currentSeason = (int)currentSeason;
        data.gameState.daysUntilSeasonChange = daysUntilSeasonChange;
        data.gameState.currentSolarYear = currentSolarYear;
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.gameState == null) return;

        currentSeason = (Season)data.gameState.currentSeason;
        daysUntilSeasonChange = data.gameState.daysUntilSeasonChange;
        currentSolarYear = data.gameState.currentSolarYear;
    }

    #endregion
}