using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Persistent singleton tracking which compendium entries have been discovered.
/// Survives scene loads and Jarl succession.
/// </summary>
public class CompendiumManager : MonoBehaviour, ISaveable
{
    public static CompendiumManager Instance { get; private set; }

    [Header("Compendium Database")]
    [Tooltip("All available compendium entries in the game")]
    [SerializeField] private CompendiumEntrySO[] allEntries;

    private Dictionary<string, CompendiumEntrySO> entryLookup = new Dictionary<string, CompendiumEntrySO>();
    private HashSet<string> discoveredIds = new HashSet<string>();

    public event Action<CompendiumEntrySO> OnEntryDiscovered;
    public event Action OnCompendiumLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BuildLookup();
    }

    private void BuildLookup()
    {
        entryLookup.Clear();

        foreach (var entry in allEntries)
        {
            if (entry == null) continue;

            if (entryLookup.ContainsKey(entry.id))
            {
                Debug.LogError($"CompendiumManager: duplicate entry id '{entry.id}' found on '{entry.name}'.");
                continue;
            }

            entryLookup[entry.id] = entry;
        }
    }

    #region Discovery

    public void Discover(string id)
    {
        if (!entryLookup.TryGetValue(id, out CompendiumEntrySO entry))
        {
            Debug.LogWarning($"CompendiumManager: attempted to discover unknown id '{id}'.");
            return;
        }

        if (discoveredIds.Contains(id)) return;

        discoveredIds.Add(id);
        OnEntryDiscovered?.Invoke(entry);
    }

    public bool IsDiscovered(string id)
    {
        return discoveredIds.Contains(id);
    }

    public CompendiumEntrySO GetEntry(string id)
    {
        return entryLookup.TryGetValue(id, out CompendiumEntrySO entry) ? entry : null;
    }

    public List<CompendiumEntrySO> GetEntriesByCategory(CompendiumCategory category)
    {
        return allEntries.Where(e => e != null && e.category == category).ToList();
    }

    public List<CompendiumEntrySO> GetDiscoveredEntries()
    {
        return allEntries.Where(e => e != null && discoveredIds.Contains(e.id)).ToList();
    }

    public (int discovered, int total) GetProgress(CompendiumCategory category)
    {
        var entries = GetEntriesByCategory(category);
        int discovered = entries.Count(e => discoveredIds.Contains(e.id));
        return (discovered, entries.Count);
    }

    public (int discovered, int total) GetOverallProgress()
    {
        int total = allEntries.Count(e => e != null);
        int discovered = allEntries.Count(e => e != null && discoveredIds.Contains(e.id));
        return (discovered, total);
    }

    #endregion

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        data.compendiumData = new CompendiumSaveData
        {
            discoveredIds = discoveredIds.ToList()
        };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.compendiumData == null) return;

        discoveredIds.Clear();
        if (data.compendiumData.discoveredIds != null)
        {
            foreach (var id in data.compendiumData.discoveredIds)
            {
                discoveredIds.Add(id);
            }
        }

        OnCompendiumLoaded?.Invoke();
    }

    #endregion
}
