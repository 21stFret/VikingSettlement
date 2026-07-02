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

    public FireController fireController;

    // Events
    public event Action<Season> OnSeasonChanged;

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

        if (daysUntilSeasonChange <= 0)
            ChangeSeason();
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
