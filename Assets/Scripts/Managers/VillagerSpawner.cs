using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory for creating villager GameObjects. Owns the prefab, parent, spawn points, and
/// village centre reference. Handles both fresh spawns (new game) and save restoration.
/// Does NOT track the villager list — SettlementManager is the single source of truth
/// for who exists in the settlement.
/// </summary>
public class VillagerSpawner : MonoBehaviour
{
    public static VillagerSpawner Instance { get; private set; }

    [Header("Spawning")]
    [SerializeField] private GameObject villagerPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private Transform villagerParent;

    [Header("Initial Settlement")]
    [SerializeField] private int initialVillagerCount = 5;

    public Transform villageCentre;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Spawn the starting villagers for a brand-new game.
    /// Called by Bootstrap only when not loading from a save file.
    /// </summary>
    public void SpawnInitialSettlement()
    {
        if (villagerPrefab == null)
        {
            Debug.LogError("VillagerSpawner: No villager prefab assigned!");
            return;
        }

        List<Gender> genders = BuildBalancedGenderList(initialVillagerCount);

        for (int i = 0; i < initialVillagerCount; i++)
            SpawnVillager(gender: genders[i]);

        Debug.Log($"VillagerSpawner: Spawned {initialVillagerCount} initial villagers");
    }

    private List<Gender> BuildBalancedGenderList(int count)
    {
        var list = new List<Gender>();
        int half = count / 2;
        for (int i = 0; i < half; i++) list.Add(Gender.Male);
        for (int i = 0; i < half; i++) list.Add(Gender.Female);
        if (count % 2 != 0)
            list.Add(Random.value > 0.5f ? Gender.Male : Gender.Female);

        // Fisher-Yates shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    /// <summary>
    /// Instantiate a single fresh villager, configure it, and register it with SettlementManager
    /// via Villager.Init().
    /// </summary>
    public Villager SpawnVillager(Vector3? position = null, Gender? gender = null, float? age = null, bool giveEquipment = true)
    {
        if (villagerPrefab == null) return null;

        Vector3 spawnPos = position ?? GetRandomSpawnPosition();
        GameObject villagerObj = Instantiate(villagerPrefab, spawnPos, Quaternion.identity);

        if (villagerParent != null)
            villagerObj.transform.SetParent(villagerParent);

        Villager villager = villagerObj.GetComponent<Villager>();
        if (villager == null) return null;

        villager.gender = gender ?? (Random.value > 0.5f ? Gender.Male : Gender.Female);
        villager.age = age ?? Random.Range(18f, 40f);

        villager.skills.Randomize();

        if (giveEquipment)
        {
            villager.itemAttachment.GiveRandomWeapon();
            villager.itemAttachment.GiveRandomShield();
        }
        //villager.itemAttachment.GiveRandomTorch();

        foreach (var ai in villager.GetComponents<VillagerAIBase>())
            ai.SetVillageCentre(villageCentre, 20f);

        villager.ApplySkillBonuses();
        villager.Init();

        if (JarlManager.Instance.CurrentJarl == null)
        {
            JarlManager.Instance.EnsureInitialJarl();
        }

        villager.CheckBuildingInteractionZone();

        return villager;
    }

    /// <summary>
    /// Restore a single villager from save data: instantiate, apply all saved fields,
    /// restore equipment and wounds, then register with SettlementManager via Init().
    /// Called by SettlementManager.LoadSaveData() for each entry in the save file.
    /// Cross-references (parents, partner, assigned building) are resolved by
    /// SettlementManager after all villagers have been restored.
    /// </summary>
    public Villager RestoreVillagerFromSave(VillagerSave vs)
    {
        if (villagerPrefab == null)
        {
            Debug.LogError("VillagerSpawner: No villager prefab assigned — cannot restore from save!");
            return null;
        }

        Vector3 pos = new Vector3(vs.posX, vs.posY, vs.posZ);
        GameObject villagerObj = Instantiate(villagerPrefab, pos, Quaternion.identity);

        if (villagerParent != null)
            villagerObj.transform.SetParent(villagerParent);

        Villager v = villagerObj.GetComponent<Villager>();
        if (v == null) return null;

        // Flag early so Init() skips default-value branches.
        v.loadedFromSave = true;

        // Identity — set uniqueId before Init() so it isn't re-generated.
        v.uniqueId = vs.id;
        v.villagerName = vs.villagerName;
        villagerObj.name = vs.villagerName;

        // Core stats
        v.gender          = (Gender)vs.gender;
        v.age             = vs.age;
        v.lifeExpectancy  = vs.lifeExpectancy;
        v.currentLifeStage = (LifeStage)vs.currentLifeStage;
        v.currentHealth   = vs.health;
        v.morale          = vs.morale;
        v.maxMorale       = vs.maxMorale;
        v.isHungry        = vs.isHungry;
        v.isCold          = vs.isCold;
        v.currentJob      = (JobType)vs.currentJob;

        // Skills
        v.skills.farming       = vs.skills.farming;
        v.skills.fishing       = vs.skills.fishing;
        v.skills.mining        = vs.skills.mining;
        v.skills.woodcutting   = vs.skills.woodcutting;
        v.skills.crafting      = vs.skills.crafting;
        v.skills.combat        = vs.skills.combat;
        v.skills.sailing       = vs.skills.sailing;
        v.skills.intelligence  = vs.skills.intelligence;
        v.skills.learningRate  = vs.skills.learningRate;

        // Combat
        v.combatStats.strength = vs.combatStats.strength;
        v.combatStats.defense  = vs.combatStats.defense;

        // Lineage / reproduction
        v.childrenCount        = vs.childrenCount;
        v.timeSinceLastChild   = vs.timeSinceLastChild;
        v.isJarl               = vs.isJarl;
        v.isOfJarlLineage      = vs.isOfJarlLineage;
        v.generationsFromJarl  = vs.generationsFromJarl;

        // Appearance
        v.spriteVariant = vs.spriteVariant;

        // Equipment
        RestoreEquipment(v, vs);

        // Wounds (before ApplySkillBonuses so the HP penalty is included)
        if (vs.activeWounds != null)
        {
            foreach (int w in vs.activeWounds)
                v.activeWounds.Add((WoundType)w);
        }

        // maxHealth derived from base + active modifiers (runestones/skill tree/wounds).
        v.ApplySkillBonuses();

        // Register with SettlementManager and finalise visuals.
        v.Init();

        v.CheckBuildingInteractionZone();

        return v;
    }

    private void RestoreEquipment(Villager v, VillagerSave vs)
    {
        ItemAttachment itemAttachment = v.GetComponent<ItemAttachment>();
        if (itemAttachment == null)
        {
            Debug.LogWarning($"VillagerSpawner: {vs.villagerName} has no ItemAttachment — equipment skipped.");
            return;
        }

        if (WeaponDatabase.Instance == null)
        {
            Debug.LogWarning($"VillagerSpawner: WeaponDatabase not found — equipment skipped for {vs.villagerName}.");
            return;
        }

        if (!string.IsNullOrEmpty(vs.weaponName))
        {
            EquipableItem prefab = WeaponDatabase.Instance.GetWeaponByName(vs.weaponName);
            if (prefab != null)
                itemAttachment.EquipWeapon(Instantiate(prefab.gameObject));
            else
                Debug.LogWarning($"VillagerSpawner: Weapon '{vs.weaponName}' not found for {vs.villagerName}.");
        }

        if (!string.IsNullOrEmpty(vs.shieldName))
        {
            EquipableItem prefab = WeaponDatabase.Instance.GetShieldByName(vs.shieldName);
            if (prefab != null)
                itemAttachment.EquipShield(Instantiate(prefab.gameObject));
            else
                Debug.LogWarning($"VillagerSpawner: Shield '{vs.shieldName}' not found for {vs.villagerName}.");
        }

        if (!string.IsNullOrEmpty(vs.torchName))
        {
            EquipableItem prefab = WeaponDatabase.Instance.GetTorchByName(vs.torchName);
            if (prefab != null)
                itemAttachment.EquipTorch(Instantiate(prefab.gameObject));
            else
                Debug.LogWarning($"VillagerSpawner: Torch '{vs.torchName}' not found for {vs.villagerName}.");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        return transform.position;
    }
}
