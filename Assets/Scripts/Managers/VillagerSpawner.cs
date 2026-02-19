using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and manages villagers in the settlement.
/// Handles initial spawn, raid returns, and prevents duplicates.
/// </summary>
public class VillagerSpawner : MonoBehaviour
{
    public static VillagerSpawner Instance { get; private set; }

    [Header("Spawning")]
    [SerializeField] private GameObject villagerPrefab;
    [SerializeField] private List<Transform> spawnPoint;
    [SerializeField] private Transform villagerParent; // Optional parent for organization

    [Header("Initial Settlement")]
    [SerializeField] private int initialVillagerCount = 5;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Jarl Setup")]
    [SerializeField] private bool assignFirstAsJarl = true;

    // Track spawned villagers
    private List<Villager> spawnedVillagers = new List<Villager>();

    public Transform villageCentre;

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

    private void Start()
    {
        // Check if we're returning from a raid with existing villagers
        if (HasReturningRaidSurvivors())
        {
            ReintegrateSurvivors();
            EnsureJarlExists();
            ConnectPlayerToJarl();
        }
        else if (ShouldLoadFromSave())
        {
            // Don't spawn initial villagers - they'll be created from save data
            Debug.Log("VillagerSpawner: Skipping initial spawn - loading from save");
        }
        else if (spawnOnStart)
        {
            SpawnInitialVillagers();
            EnsureJarlExists();
            ConnectPlayerToJarl();
        }
    }

    /// <summary>
    /// Check if we should load from a save file instead of spawning fresh
    /// </summary>
    private bool ShouldLoadFromSave()
    {
        return GameManager.Instance != null && GameManager.Instance.ShouldLoadSave;
    }

    /// <summary>
    /// Called by SettlementManager after loading villagers from save
    /// </summary>
    public void OnVillagersLoadedFromSave(List<Villager> loadedVillagers)
    {
        spawnedVillagers.Clear();
        spawnedVillagers.AddRange(loadedVillagers);

        EnsureJarlExists();
        ConnectPlayerToJarl();

        Debug.Log($"VillagerSpawner: Registered {loadedVillagers.Count} villagers from save");
    }

    /// <summary>
    /// Check if there are DontDestroyOnLoad villagers from a raid
    /// </summary>
    private bool HasReturningRaidSurvivors()
    {
        // Find any villagers that are in DontDestroyOnLoad (null scene or different scene)
        Villager[] allVillagers = FindObjectsByType<Villager>(FindObjectsSortMode.None);
        foreach (var villager in allVillagers)
        {
            if (villager.gameObject.scene.name == "DontDestroyOnLoad" ||
                villager.gameObject.scene != gameObject.scene)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Reintegrate survivors from a raid back into the settlement
    /// </summary>
    private void ReintegrateSurvivors()
    {
        Villager[] allVillagers = FindObjectsByType<Villager>(FindObjectsSortMode.None);

        int spawnIndex = 0;

        foreach (var villager in allVillagers)
        {
            spawnIndex++;
            if (spawnIndex >= spawnPoint.Count)
            {
                spawnIndex = 0;
            }

            // Move DontDestroyOnLoad villagers back to this scene
            if (villager.gameObject.scene.name == "DontDestroyOnLoad")
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                    villager.gameObject,
                    gameObject.scene
                );

                // Parent to villager container if set
                if (villagerParent != null)
                {
                    villager.transform.SetParent(villagerParent);
                }

                // Only reposition raid survivors, not home villagers (they keep their position)
                bool wasRaidSurvivor = RaidManager.Instance != null && RaidManager.Instance.WasRaidSurvivor(villager);
                if (wasRaidSurvivor && spawnPoint != null && spawnPoint.Count > 0)
                {
                    Vector2 offset = Random.insideUnitCircle * 2f;
                    villager.transform.position = spawnPoint[spawnIndex].position + new Vector3(offset.x, offset.y, 0);
                    Debug.Log($"Reintegrated raid survivor: {villager.villagerName}");
                }
                else
                {
                    Debug.Log($"Reintegrated home villager: {villager.villagerName}");
                }

                spawnedVillagers.Add(villager);
            }
            else if (villager.gameObject.scene == gameObject.scene)
            {
                // Already in this scene, just track it
                if (!spawnedVillagers.Contains(villager))
                {
                    spawnedVillagers.Add(villager);
                }
            }
        }

        // Register all villagers with the new SettlementManager instance
        // Their original Start() already ran in the old scene, so we need to re-register
        if (SettlementManager.Instance != null)
        {
            foreach (var villager in spawnedVillagers)
            {
                SettlementManager.Instance.RegisterVillager(villager);

                // Reinitialize villager's scene-specific references
                villager.ReinitializeAfterSceneChange();
            }
            Debug.Log($"Re-registered {spawnedVillagers.Count} villagers with SettlementManager after raid");

            // Restore worker assignments from saved data
            RestoreWorkerAssignments();
        }

        // Clear the reintegration tracking now that we're done
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.ClearReintegrationTracking();
            RaidManager.Instance.ClearSavedWorkerAssignments();
        }
    }

    /// <summary>
    /// Restore worker assignments after returning from a raid
    /// </summary>
    private void RestoreWorkerAssignments()
    {
        if (RaidManager.Instance == null || SettlementManager.Instance == null) return;

        // Build a lookup of buildings by their data name
        var buildingsByName = new Dictionary<string, List<Building>>();
        foreach (var building in SettlementManager.Instance.GetAllBuildings())
        {
            if (building != null && building.data != null)
            {
                string buildingName = building.data.name;
                if (!buildingsByName.ContainsKey(buildingName))
                {
                    buildingsByName[buildingName] = new List<Building>();
                }
                buildingsByName[buildingName].Add(building);
            }
        }

        int assignedCount = 0;

        // Restore assignments for each villager
        foreach (var villager in spawnedVillagers)
        {
            if (villager == null) continue;

            string savedBuildingName = RaidManager.Instance.GetSavedWorkerAssignment(villager.uniqueId);
            if (string.IsNullOrEmpty(savedBuildingName)) continue;

            // Find a building with this name that has capacity
            if (buildingsByName.TryGetValue(savedBuildingName, out List<Building> matchingBuildings))
            {
                foreach (var building in matchingBuildings)
                {
                    if (building.CanAssignWorker())
                    {
                        // Assign the worker
                        villager.AssignJob(building.data.assignedJobType, building);
                        building.AssignWorker(villager);
                        assignedCount++;
                        Debug.Log($"Restored {villager.villagerName} to {savedBuildingName}");
                        break;
                    }
                }
            }
        }

        Debug.Log($"Restored {assignedCount} worker assignments after raid");
    }

    /// <summary>
    /// Spawn the initial villagers for a new settlement
    /// </summary>
    private void SpawnInitialVillagers()
    {
        if (villagerPrefab == null)
        {
            Debug.LogError("VillagerSpawner: No villager prefab assigned!");
            return;
        }

        for (int i = 0; i < initialVillagerCount; i++)
        {
            SpawnVillager();
        }

        Debug.Log($"Spawned {initialVillagerCount} initial villagers");
    }

    /// <summary>
    /// Spawn a single villager
    /// </summary>
    public Villager SpawnVillager(Vector3? position = null, Gender? gender = null, float? age = null)
    {
        if (villagerPrefab == null) return null;

        Vector3 spawnPos = position ?? GetRandomSpawnPosition();

        GameObject villagerObj = Instantiate(villagerPrefab, spawnPos, Quaternion.identity);

        if (villagerParent != null)
        {
            villagerObj.transform.SetParent(villagerParent);
        }

        Villager villager = villagerObj.GetComponent<Villager>();
        if (villager != null)
        {
            // Set gender if specified
            if (gender.HasValue)
            {
                villager.gender = gender.Value;
            }
            else
            {
                villager.gender = Random.value > 0.5f ? Gender.Male : Gender.Female;
            }

            // Set age if specified (default to mature adult)
            if (age.HasValue)
            {
                villager.age = age.Value;
            }
            else
            {
                villager.age = Random.Range(18f, 40f);
            }

            //Give weapon, shield and torch
            villager.itemAttachment.GiveRandomWeapon();
            villager.itemAttachment.GiveRandomShield();
            villager.itemAttachment.GiveRandomTorch();

            villager.GetComponent<VillagerAI>()?.SetVillageCentre(villageCentre, 20f);

            spawnedVillagers.Add(villager);
            villager.ApplySkillBonuses();
        }

        return villager;
    }

    /// <summary>
    /// Get a random position near the spawn point
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoint != null)
        {
            int spawnIndex = Random.Range(0, spawnPoint.Count);
            return spawnPoint[spawnIndex].position;
        }
        return transform.position;
    }

    /// <summary>
    /// Ensure there is a Jarl in the settlement
    /// </summary>
    private void EnsureJarlExists()
    {
        // Check if JarlManager already has a Jarl
        if (JarlManager.Instance != null && JarlManager.Instance.GetCurrentJarl() != null)
        {
            return;
        }

        // Find existing Jarl among villagers
        foreach (var villager in spawnedVillagers)
        {
            if (villager != null && villager.isJarl)
            {
                // Register with JarlManager if needed
                if (JarlManager.Instance != null)
                {
                    JarlManager.Instance.SetInitialJarl(villager);
                }
                return;
            }
        }

        // No Jarl found, assign one
        if (assignFirstAsJarl && spawnedVillagers.Count > 0)
        {
            Villager newJarl = spawnedVillagers[0];
            if (newJarl != null)
            {
                newJarl.isJarl = true;
                newJarl.isOfJarlLineage = true;
                newJarl.generationsFromJarl = 0;

                if (JarlManager.Instance != null)
                {
                    JarlManager.Instance.SetInitialJarl(newJarl);
                }

                Debug.Log($"Assigned {newJarl.villagerName} as Jarl");
            }
        }
    }

    /// <summary>
    /// Connect the player controller to the Jarl
    /// </summary>
    private void ConnectPlayerToJarl()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("VillagerSpawner: No PlayerController found!");
            return;
        }

        // Find the Jarl
        Villager jarl = null;
        foreach (var villager in spawnedVillagers)
        {
            if (villager != null && villager.isJarl)
            {
                jarl = villager;
                break;
            }
        }

        if (jarl != null)
        {
            playerController.SetControlTarget(jarl);
            Debug.Log($"Connected player to Jarl: {jarl.villagerName}");
        }
        else
        {
            Debug.LogWarning("VillagerSpawner: No Jarl found to connect player to!");
        }
    }

    /// <summary>
    /// Get all spawned villagers
    /// </summary>
    public List<Villager> GetAllVillagers()
    {
        // Clean up null references
        spawnedVillagers.RemoveAll(v => v == null);
        return new List<Villager>(spawnedVillagers);
    }

    /// <summary>
    /// Remove a villager from tracking (called when they die)
    /// </summary>
    public void UntrackVillager(Villager villager)
    {
        spawnedVillagers.Remove(villager);
    }

    /// <summary>
    /// Get the villager prefab for spawning
    /// </summary>
    public GameObject GetVillagerPrefab()
    {
        return villagerPrefab;
    }

    /// <summary>
    /// Get the parent transform for villagers
    /// </summary>
    public Transform GetVillagerParent()
    {
        return villagerParent;
    }
}
