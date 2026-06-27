using UnityEngine;
using System;

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

    [Header("Lighting Adjustments")]
    [Tooltip("Multiplier for ambient light during summer")]
    public float summerAmbientMultiplier = 1.2f;

    [Tooltip("Multiplier for ambient light during winter")]
    public float winterAmbientMultiplier = 0.8f;

    [Tooltip("Sun color tint during winter")]
    public Color winterSunTint = new Color(0.9f, 0.95f, 1f);

    [Tooltip("Sun color tint during summer")]
    public Color summerSunTint = new Color(1f, 1f, 0.95f);

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
    private float woodNeededToday = 0f;

    public FireController fireController;

    // Events
    public event Action<Season> OnSeasonChanged;
    public event Action<bool> OnWarmthChanged; // true = warm, false = cold

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
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDay -= OnNewDay;

        if (GameTickManager.Instance != null)
            GameTickManager.Instance.OnFastUpdate -= FastUpdate;
    }

    private void OnNewDay()
    {
        daysUntilSeasonChange--;

        Debug.Log($"Days until season change: {daysUntilSeasonChange}");

        if (currentSeason == Season.Winter)
            ConsumeFirewood();
        else
            SetWarmthStatus(true);

        if (daysUntilSeasonChange <= 0)
            ChangeSeason();
    }

    private void ConsumeFirewood()
    {
        if (SettlementManager.Instance == null || ResourceManager.Instance == null)
        {
            SetWarmthStatus(true);
            return;
        }

        int   villagerCount    = SettlementManager.Instance.GetPopulation();
        // Summer burns at half rate — fires still needed but less fierce
        float seasonMultiplier = (currentSeason == Season.Winter) ? 1.0f : 0.5f;
        float stormMultiplier  = StormScheduler.Instance != null
            ? StormScheduler.Instance.GetCurrentDayWoodMultiplier()
            : 1.0f;
        woodNeededToday = villagerCount * woodPerVillagerPerDay * seasonMultiplier * stormMultiplier;

        if (RunestoneManager.Instance != null)
            woodNeededToday *= RunestoneManager.Instance.GetFirewoodConsumptionMultiplier();

        float availableWood = ResourceManager.Instance.GetResource(ResourceType.Wood);

        if (availableWood >= woodNeededToday)
        {
            ResourceManager.Instance.SpendResource(ResourceType.Wood, woodNeededToday);
            woodConsumedToday = woodNeededToday;
            SetWarmthStatus(true);
            ApplyWarmEffects();
            Debug.Log($"Winter heating: Consumed {woodNeededToday:F1} wood for {villagerCount} villagers. Settlement is warm.");
        }
        else
        {
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

        foreach (var villager in SettlementManager.Instance.GetAllVillagers())
        {
            if (villager == null || villager.IsDead()) continue;
            villager.isCold = false;
            villager.ChangeMorale(warmthMoraleBonus);
        }

        Debug.Log("Settlement is warm. No negative effects applied.");
    }

    private void ApplyColdEffects()
    {
        if (SettlementManager.Instance == null) return;

        var villagers = SettlementManager.Instance.GetAllVillagers();

        float percentageCold = woodNeededToday > 0 ? 1f - (woodConsumedToday / woodNeededToday) : 0f;
        int villagersAffected = (int)(villagers.Count * percentageCold);

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

    private void FastUpdate()
    {
        UpdateSeasonEffects();
    }

    private void UpdateSeasonEffects()
    {
        Color sunTint = currentSeason == Season.Summer ? summerSunTint : winterSunTint;

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.sunColor = Color.Lerp(
                DayNightManager.Instance.sunColor,
                sunTint,
                0.5f * Time.deltaTime);
        }
    }

    private void ChangeSeason()
    {
        Season newSeason = currentSeason == Season.Summer ? Season.Winter : Season.Summer;

        Debug.Log($"Season changing from {currentSeason} to {newSeason}");

        ApplySeasonEffects(newSeason);

        currentSeason = newSeason;
        daysUntilSeasonChange = daysPerSeason;

        OnSeasonChanged?.Invoke(currentSeason);

        if (currentSeason == Season.Summer)
            currentSolarYear++;
    }

    private void ApplySeasonEffects(Season season)
    {
        if (season == Season.Summer)
            ApplySummerLighting();
        else
            ApplyWinterLighting();
    }

    private void ApplySummerLighting()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.eveningMultiplier = summerAmbientMultiplier;
    }

    private void ApplyWinterLighting()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.eveningMultiplier = winterAmbientMultiplier;
    }

    // Public API

    public Season GetCurrentSeason() => currentSeason;

    public int GetDaysUntilSeasonChange() => daysUntilSeasonChange;

    public int GetCurrentSolarYear() => currentSolarYear;

    public bool IsSettlementWarm() => isSettlementWarm;

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
                ChangeSeason();

            int step = Mathf.Min(remaining, daysUntilSeasonChange);
            daysUntilSeasonChange -= step;
            remaining -= step;

            if (daysUntilSeasonChange <= 0)
                ChangeSeason();
        }
        Debug.Log($"SeasonManager: Advanced {days} days — now {currentSeason}, {daysUntilSeasonChange} days until season change");
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
