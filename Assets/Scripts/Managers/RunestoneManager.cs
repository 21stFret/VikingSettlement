using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RunestoneManager : MonoBehaviour, ISaveable
{
    public static RunestoneManager Instance { get; private set; }

    public const int MAX_ACTIVE_RUNESTONES = 3;

    [Header("Icons")]
    public RunestoneIconEntry[] iconMapping;
    public Sprite defaultIcon;

    [Header("State")]
    [SerializeField] private List<RunestoneType> activeRunestones = new List<RunestoneType>();

    public IReadOnlyList<RunestoneType> ActiveRunestones => activeRunestones;
    public bool IsAtCapacity => activeRunestones.Count >= MAX_ACTIVE_RUNESTONES;

    // Events
    public event Action<List<RunestoneType>, Villager> OnSelectionStarted;
    public event Action OnSelectionComplete;

    private List<RunestoneType> currentOptions;
    private Villager deadJarlForSelection;
    private Dictionary<RunestoneType, Sprite> iconLookup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildIconLookup();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void BuildIconLookup()
    {
        iconLookup = new Dictionary<RunestoneType, Sprite>();
        if (iconMapping == null) return;

        foreach (var entry in iconMapping)
        {
            if (entry.icon != null)
            {
                iconLookup[entry.type] = entry.icon;
            }
        }
    }

    public Sprite GetIcon(RunestoneType type)
    {
        if (iconLookup != null && iconLookup.TryGetValue(type, out Sprite icon))
        {
            return icon;
        }
        return defaultIcon;
    }

    #region Selection Logic

    public void StartSelection(Villager deadJarl)
    {
        deadJarlForSelection = deadJarl;
        currentOptions = GenerateOptions(deadJarl);

        Debug.Log($"Runestone selection started for {deadJarl.villagerName}. Options: {string.Join(", ", currentOptions)}");
        OnSelectionStarted?.Invoke(currentOptions, deadJarl);
    }

    public List<RunestoneType> GenerateOptions(Villager deadJarl)
    {
        var options = new List<RunestoneType>();
        var excluded = new HashSet<RunestoneType>(activeRunestones);

        // Determine Jarl's category from highest skill
        RunestoneCategory jarlCategory = DetermineJarlCategory(deadJarl);

        // Pick 1 from Jarl's category
        var categoryPool = RunestoneDatabase.GetByCategory(jarlCategory)
            .Where(r => !excluded.Contains(r.type))
            .ToList();

        if (categoryPool.Count > 0)
        {
            var picked = categoryPool[UnityEngine.Random.Range(0, categoryPool.Count)];
            options.Add(picked.type);
            excluded.Add(picked.type);
        }

        // Pick 2 wildcards from remaining pool
        var remainingPool = RunestoneDatabase.GetAllExcluding(excluded);
        ShuffleList(remainingPool);

        int needed = 3 - options.Count;
        for (int i = 0; i < Mathf.Min(needed, remainingPool.Count); i++)
        {
            options.Add(remainingPool[i].type);
        }

        return options;
    }

    private RunestoneCategory DetermineJarlCategory(Villager jarl)
    {
        if (jarl == null || jarl.skills == null) return RunestoneCategory.Economy;

        float combatScore = jarl.skills.combat;
        float economyScore = Mathf.Max(jarl.skills.farming, jarl.skills.fishing,
            jarl.skills.mining, jarl.skills.woodcutting, jarl.skills.crafting);

        return combatScore > economyScore ? RunestoneCategory.Combat : RunestoneCategory.Economy;
    }

    public void SelectRunestone(RunestoneType type)
    {
        if (activeRunestones.Count < MAX_ACTIVE_RUNESTONES)
        {
            activeRunestones.Add(type);
            var info = RunestoneDatabase.GetInfo(type);
            Debug.Log($"Runestone activated: {info.name} - {info.description}");
            CompleteSelection();
        }
        else
        {
            // At capacity — UI must call ReplaceRunestone instead.
            // Calling CompleteSelection here would silently skip the replacement step,
            // so log an error and leave selection open so the UI can recover.
            Debug.LogError($"SelectRunestone called at capacity ({MAX_ACTIVE_RUNESTONES}). Call ReplaceRunestone to swap an existing stone.");
        }
    }

    public void ReplaceRunestone(RunestoneType toRemove, RunestoneType newType)
    {
        if (activeRunestones.Remove(toRemove))
        {
            activeRunestones.Add(newType);
            var removedInfo = RunestoneDatabase.GetInfo(toRemove);
            var newInfo = RunestoneDatabase.GetInfo(newType);
            Debug.Log($"Runestone replaced: {removedInfo.name} -> {newInfo.name}");
            CompleteSelection();
        }
        else
        {
            Debug.LogError($"Cannot replace runestone {toRemove} - not found in active list!");
        }
    }

    private void CompleteSelection()
    {
        currentOptions = null;
        deadJarlForSelection = null;
        OnSelectionComplete?.Invoke();
    }

    #endregion

    #region Modifier Query Methods

    public bool HasRunestone(RunestoneType type)
    {
        return activeRunestones.Contains(type);
    }

    /// <summary>Rationing: -1 flat food consumption per villager</summary>
    public float GetFoodConsumptionModifier()
    {
        return HasRunestone(RunestoneType.Rationing) ? -1f : 0f;
    }

    /// <summary>Tireless Workers: 1.15x production speed</summary>
    public float GetProductionSpeedMultiplier()
    {
        return HasRunestone(RunestoneType.TirelessWorkers) ? 1.15f : 1f;
    }

    /// <summary>Winter's Friend: 0.7x firewood consumption</summary>
    public float GetWoodConsumptionMultiplier()
    {
        return HasRunestone(RunestoneType.WintersFriend) ? 0.7f : 1f;
    }

    /// <summary>Ancestors' Fury: 0.7x Jarl cooldowns</summary>
    public float GetJarlCooldownMultiplier()
    {
        return HasRunestone(RunestoneType.AncestorsFury) ? 0.7f : 1f;
    }

    /// <summary>Iron Discipline: +15% warrior defense</summary>
    public float GetWarriorDefensePercent()
    {
        return HasRunestone(RunestoneType.IronDiscipline) ? 15f : 0f;
    }

    /// <summary>Weapon Maintenance: 1.10x warrior damage</summary>
    public float GetWarriorDamageMultiplier()
    {
        return HasRunestone(RunestoneType.WeaponMaintenance) ? 1.10f : 1f;
    }

    /// <summary>Resilient Blood: 1.10x max health for all villagers</summary>
    public float GetMaxHealthMultiplier()
    {
        return HasRunestone(RunestoneType.ResilientBlood) ? 1.10f : 1f;
    }

    /// <summary>Fertile Lands: 1.20x birth rate</summary>
    public float GetBirthRateMultiplier()
    {
        return HasRunestone(RunestoneType.FertileLands) ? 1.20f : 1f;
    }

    /// <summary>Education: +2 to 2 random skills for new villagers</summary>
    public int GetEducationSkillBonus()
    {
        return HasRunestone(RunestoneType.Education) ? 2 : 0;
    }

    /// <summary>Raiding Ships: 1.10x raid loot</summary>
    public float GetRaidLootMultiplier()
    {
        return HasRunestone(RunestoneType.RaidingShips) ? 1.10f : 1f;
    }

    /// <summary>Swift Recovery: +1 wound heal tier (deferred)</summary>
    public int GetWoundHealTierBonus()
    {
        return HasRunestone(RunestoneType.SwiftRecovery) ? 1 : 0;
    }

    /// <summary>Runekeeper's Gift: -1 rune cost (deferred)</summary>
    public int GetRuneCostReduction()
    {
        return HasRunestone(RunestoneType.RunekeepersGift) ? 1 : 0;
    }

    #endregion

    #region Utility

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    #endregion

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        data.runestoneData = new RunestoneSaveData
        {
            activeRunestoneTypes = activeRunestones.Select(r => (int)r).ToArray()
        };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.runestoneData == null || data.runestoneData.activeRunestoneTypes == null) return;

        activeRunestones.Clear();
        foreach (int type in data.runestoneData.activeRunestoneTypes)
        {
            activeRunestones.Add((RunestoneType)type);
        }

        Debug.Log($"Loaded {activeRunestones.Count} active runestones: {string.Join(", ", activeRunestones)}");
    }

    #endregion
}

[System.Serializable]
public class RunestoneIconEntry
{
    public RunestoneType type;
    public Sprite icon;
}
