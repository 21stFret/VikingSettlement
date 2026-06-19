using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Villager : TargetHealth
{
    public string uniqueId;
    public string villagerName;
    public JobType currentJob = JobType.None;
    public Building assignedBuilding;

    public VillagerSkills skills = new VillagerSkills();
    public float skillGainRate = 1f; // Multiplier for skill gain speed
    public CombatStats combatStats = new CombatStats();
    
    [Header("Status")]
    public float morale = 100f;
    public float maxMorale = 100f;
    public bool isHungry = false;
    public bool isCold = false;
    public bool isOnRaid = false;
    public float healthRegenFromFood = 1; // Health regained when fed
    public float moraleRegenFromFood = 5; // Morale regained when fed

    [Header("Life Cycle")]
    public Gender gender;
    public float age = 0f; // Age in years
    public float lifeExpectancy = 60f; // Expected lifespan in years
    public LifeStage currentLifeStage = LifeStage.Young;
    
    [Header("Reproduction")]
    public Villager partner; // Current partner for reproduction
    public float timeSinceLastChild = 0f;
    public int childrenCount = 0;
    
    [Header("Genetics")]
    public Villager parent1;
    public Villager parent2;

    [Header("Jarl Status")]
    public bool isJarl = false;
    public bool isOfJarlLineage = false;
    public int generationsFromJarl = -1; // -1 if not in lineage, 0 = is Jarl, 1 = child of Jarl, etc.

    public float agingTimer = 0f;
    private float reproductionTimer = 0f;

    [Header("Appearance")]
    public string spriteVariant = ""; // e.g. "blue_villager", "red_villager"

    [Header("Wounds")]
    public List<WoundType> activeWounds = new List<WoundType>();

    [Header("Save/Load")]
    [HideInInspector] public bool loadedFromSave = false; // Set by SettlementManager when loading

    private SettlementManager _settlementManager;
    public VillagerPersonalUI personalUI;

    [Header("Visuals")]
    private Material _material;
    public ParticleSystem bloodEffect;

    [Header("Speech Settings")]
    private float _timeSinceLastSpoke = 0f;
    private float _speechCooldown = 10f; // Minimum time between speeches
    public float _speechMinCooldown = 5f;
    public float _speechMaxCooldown = 10f;

    private CharacterController _controller;

    public ItemAttachment itemAttachment;

    public void Init()
    {
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }

        _settlementManager = SettlementManager.Instance;
        _material = GetComponent<Renderer>().material;
        _controller = GetComponent<CharacterController>();
        itemAttachment = GetComponent<ItemAttachment>();

        if (string.IsNullOrEmpty(villagerName))
        {
            villagerName = VillagerNameGenerator.GenerateNorseName(gender);
            gameObject.name = villagerName;
        }

        // Set initial life stage based on age (skip if loaded from save - it's already set)
        if (!loadedFromSave)
        {
            UpdateLifeStage();
        }

        // Register with settlement manager
        if (_settlementManager != null)
        {
            _settlementManager.RegisterVillager(this);
        }

        // Only set to max if not loaded from save
        if (!loadedFromSave)
        {
            currentHealth = maxHealth;
            morale = maxMorale;
        }

        // Subscribe to skill changes if we're the Jarl
        if (isJarl && SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnSkillUnlocked += OnSkillTreeChanged;
        }

        // add random to speech timer so not all villagers speak at once
        _timeSinceLastSpoke = Random.Range(0f, _speechCooldown);

        // Only assign random sprite if not loaded from save
        if (string.IsNullOrEmpty(spriteVariant))
        {
            if (!loadedFromSave)
            {
                AssignRandomSpriteVariant();
            }
        }
        else
        {
            ApplySpriteVariant();
        }
    }

    private void Update()
    {
        // Update speech timer
        _timeSinceLastSpoke += Time.deltaTime;
        if (_timeSinceLastSpoke >= _speechCooldown)
        {
            MakeASpeechComment();
            _timeSinceLastSpoke = 0f;
            _speechCooldown = Random.Range(_speechMinCooldown, _speechMaxCooldown); // Randomize next speech time
        }
    }
    
    #region Job Management
    
    public void AssignJob(JobType job, Building building)
    {
        // Only mature villagers can work
        if (currentLifeStage != LifeStage.Mature)
        {
            Debug.Log($"{villagerName} is too young/old to work!");
            return;
        }
        
        currentJob = job;
        assignedBuilding = building;
    }
    
    public void UnassignJob()
    {
        currentJob = JobType.None;
        assignedBuilding = null;
    }

    public float GetSkillMultiplier(JobType job)
    {
        float baseSkill = skills.GetSkillForJob(job);
        float moraleModifier = morale / 100f;
        return (1f + baseSkill * 0.5f) * moraleModifier;
    }

    public void UpdateLife(float deltaTime)
    {
        if (currentLifeStage == LifeStage.Mature)
        {
            Work();
        }

        // Ageing
        agingTimer += deltaTime;
        if (agingTimer >= _settlementManager.ageingInterval) // Age once per real-time second (adjustable)
        {
            agingTimer = 0f;
            Age(_settlementManager.ageingAmount); // Increase age by 0.1 years per second
        }

        
        // Handle reproduction timer
        if (currentLifeStage == LifeStage.Mature)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= _settlementManager.reproductionInterval)
            {
                reproductionTimer = 0f;
                timeSinceLastChild += 0.1f;

                // Check for reproduction opportunity
                TryReproduce();
            }
        }

    }

    public void Work()
    {
        if (currentJob == JobType.None || assignedBuilding == null) return;

        // Improve skill over time
        skills.ImproveSkill(currentJob);
    }

    public void MakeASpeechComment()
    {
        personalUI.ShowSpeech("For Odin!", 2.0f);
    }
    
    #endregion
    #region Life Cycle Management
    
    private void Age(float years)
    {
        age += years;
        
        // Update life stage
        LifeStage previousStage = currentLifeStage;
        UpdateLifeStage();
        
        // Handle stage transitions
        if (previousStage != currentLifeStage)
        {
            OnLifeStageChanged(previousStage, currentLifeStage);
        }
        
        // Check for death of old age
        if (age >= lifeExpectancy)
        {
            Die();
        }
    }
    
    private void UpdateLifeStage()
    {
        float youngThreshold = 16f; // Age below which villager is considered young
        
        if (age < youngThreshold)
        {
            currentLifeStage = LifeStage.Young;
        }
        else if (age < lifeExpectancy)
        {
            currentLifeStage = LifeStage.Mature;
        }
        else
        {
            currentLifeStage = LifeStage.Dead;
        }
    }
    
    private void OnLifeStageChanged(LifeStage from, LifeStage to)
    {
        Debug.Log($"{villagerName} transitioned from {from} to {to} at age {age:F1}");
        
        // Unassign job if no longer mature
        if (to != LifeStage.Mature && currentJob != JobType.None)
        {
            UnassignJob();
            if (assignedBuilding != null)
            {
                assignedBuilding.RemoveWorker(this);
            }
        }
        
        // Handle death transition
        if (to == LifeStage.Dead)
        {
            Die();
        }
    }
    
    private void TryReproduce()
    {
        // Only mature villagers can reproduce
        if (currentLifeStage != LifeStage.Mature) return;
        
        // Calculate effective reproduction cooldown with birth rate modifiers
        float effectiveCooldown = _settlementManager.reproductionCooldown;

        // Apply Fertile Lands runestone bonus (+20% birth rate = reduce cooldown)
        if (RunestoneManager.Instance != null)
        {
            effectiveCooldown /= RunestoneManager.Instance.GetBirthRateMultiplier();
        }

        // Apply Gefjon's Blessing birth rate bonus (+10%)
        if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive)
        {
            effectiveCooldown /= (1f + DeathTypeBuff.Instance.GetBirthRatePercent() / 100f);
        }

        // Must have waited long enough since last child
        if (timeSinceLastChild < effectiveCooldown) return;
        
        // Need to find a partner if we don't have one
        if (partner == null)
        {
            partner = FindPotentialPartner();
        }
        
        // If we have a valid partner, create a child
        if (partner != null && partner.currentLifeStage == LifeStage.Mature)
        {
            // Only one partner creates the child to avoid duplicates
            if (gender == Gender.Female)
            {
                CreateChild(this, partner);
                timeSinceLastChild = 0f;
                partner.timeSinceLastChild = 0f;
                childrenCount++;
                partner.childrenCount++;
                personalUI.UpdateStatusEffectIcon(VillagerStatusEffect.Love);
                partner.personalUI.UpdateStatusEffectIcon(VillagerStatusEffect.Love);
            }
        }
    }
    
    private Villager FindPotentialPartner()
    {
        if (SettlementManager.Instance == null) return null;
        
        // Get all villagers
        var allVillagers = SettlementManager.Instance.GetAllVillagers();
        
        foreach (var villager in allVillagers)
        {
            // Check if valid partner
            if (villager != this && 
                villager.gender != this.gender && 
                villager.currentLifeStage == LifeStage.Mature &&
                villager.timeSinceLastChild >= villager._settlementManager.reproductionCooldown)
            {
                return villager;
            }
        }
        
        return null;
    }

    private void CreateChild(Villager mother, Villager father)
    {
        // Instantiate a new villager (you'll need to have a villager prefab set up)
        GameObject childObject = Instantiate(gameObject, transform.position + Vector3.right * 0.5f, Quaternion.identity);
        Villager child = childObject.GetComponent<Villager>();

        // Set basic properties
        child.age = 0f;
        child.lifeExpectancy = Random.Range(50f, 70f); // Slight variation in lifespan
        child.gender = Random.value > 0.5f ? Gender.Male : Gender.Female;
        child.villagerName = VillagerNameGenerator.GenerateNorseName(child.gender);
        gameObject.name = villagerName;

        // Set parents
        child.parent1 = mother;
        child.parent2 = father;

        // Inherit skills from parents (mean of both parents)
        child.skills = VillagerSkills.Inherit(mother.skills, father.skills);

        // Apply Education runestone bonus (+2 to 2 random skills)
        if (RunestoneManager.Instance != null)
        {
            int bonus = RunestoneManager.Instance.GetEducationSkillBonus();
            if (bonus > 0)
            {
                child.skills.ApplyRandomSkillBonuses(bonus, 2);
            }
        }

        // Inherit combat stats (mean of both parents)
        child.combatStats.strength = (mother.combatStats.strength + father.combatStats.strength) / 2f;
        child.combatStats.defense = (mother.combatStats.defense + father.combatStats.defense) / 2f;

        // Add some random variation to inherited traits
        child.skillGainRate = (mother.skillGainRate + father.skillGainRate) / 2f + Random.Range(-0.1f, 0.1f);
        child.skillGainRate = Mathf.Max(0.5f, child.skillGainRate); // Minimum 0.5

        child.currentJob = JobType.None;
        child.assignedBuilding = null;

        // Track Jarl lineage for inheritance
        if (mother.isJarl || father.isJarl)
        {
            child.isOfJarlLineage = true;
            child.generationsFromJarl = 1;
        }
        else if (mother.isOfJarlLineage || father.isOfJarlLineage)
        {
            child.isOfJarlLineage = true;
            // Inherit the closest lineage distance + 1
            int motherGen = mother.generationsFromJarl >= 0 ? mother.generationsFromJarl : int.MaxValue;
            int fatherGen = father.generationsFromJarl >= 0 ? father.generationsFromJarl : int.MaxValue;
            child.generationsFromJarl = Mathf.Min(motherGen, fatherGen) + 1;
        }

        child.spriteVariant = InheritSpriteVariant(mother.spriteVariant, father.spriteVariant);
        child.ApplySpriteVariant();

        child.ApplySkillBonuses();

        Debug.Log($"{mother.villagerName} and {father.villagerName} had a child: {child.villagerName}!");
    }

    #endregion

    #region Health Management

    #region Wounds

    /// <summary>
    /// Add a wound. A 4th wound kills the villager regardless of remaining HP.
    /// </summary>
    public void AddWound(WoundType wound)
    {
        if (activeWounds.Count >= WoundManager.MAX_WOUNDS)
        {
            Debug.Log($"{villagerName} suffered a mortal wound and has died!");
            Die();
            return;
        }

        activeWounds.Add(wound);
        var info = WoundDatabase.Get(wound);
        Debug.Log($"{villagerName} received wound: {info.displayName} — {info.description}");

        ApplySkillBonuses(); // Refresh max HP with new wound penalty
        if (personalUI != null) personalUI.UpdateBars(true, false);
    }

    /// <summary>
    /// Remove a wound (called by HealerHut on purchase).
    /// </summary>
    public void RemoveWound(WoundType wound)
    {
        if (activeWounds.Remove(wound))
        {
            ApplySkillBonuses();
            if (personalUI != null) personalUI.UpdateBars(true, false);
            Debug.Log($"{villagerName}'s {WoundDatabase.Get(wound).displayName} has healed.");
        }
    }

    protected override void OnSignificantHPDamage(float hpDamage)
    {
        if (WoundManager.Instance != null)
            WoundManager.Instance.TryApplyWound(this, hpDamage, maxHealth);
    }

    #endregion

    /// <summary>
    /// Apply defense and shield reduction to incoming damage.
    /// </summary>
    protected override float CalculateFinalDamage(float rawDamage, EquipableItem weapon)
    {
        // Active block: route all damage to shield durability, no HP damage.
        // Attacks from behind bypass the block entirely.
        if (_controller != null && _controller.shield != null && !_controller.shield.IsBroken)
        {
            if ((_controller.isBlocking || _controller.isParrying)
                && !_controller.IsAttackFromBehind(_controller.lastAttackerPosition))
            {
                int durDamage = _controller.isParrying
                    ? Mathf.CeilToInt(rawDamage * 0.5f)
                    : Mathf.CeilToInt(rawDamage);
                _controller.shield.TakeDurabilityDamage(durDamage);
                return 0f;
            }
        }

        float reduced = rawDamage;

        // Apply defense stat
        float totalDefense = combatStats.defense;

        // Apply skill bonuses if this is the Jarl
        if (isJarl && SkillTreeManager.Instance != null)
        {
            totalDefense += SkillTreeManager.Instance.GetEffect(SkillEffectType.DefenseFlat);
            float defensePercent = SkillTreeManager.Instance.GetEffect(SkillEffectType.DefensePercent);
            totalDefense *= (1f + defensePercent / 100f);
        }

        // Apply runestone warrior defense bonus
        if (RunestoneManager.Instance != null)
        {
            float runestoneDefPercent = RunestoneManager.Instance.GetWarriorDefensePercent();
            totalDefense *= (1f + runestoneDefPercent / 100f);
        }

        // Apply death buff warrior defense bonus
        if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive)
        {
            float deathBuffDefPercent = DeathTypeBuff.Instance.GetWarriorDefensePercent();
            totalDefense *= (1f + deathBuffDefPercent / 100f);
        }

        // Apply wound defense penalties
        if (activeWounds.Count > 0)
        {
            float woundDefPenaltyPct = WoundDatabase.TotalDefensePenaltyPct(activeWounds);
            totalDefense *= (1f - woundDefPenaltyPct / 100f);
        }

        reduced -= totalDefense;

        return reduced;
    }

    /// <summary>
    /// Visual feedback when taking damage.
    /// </summary>
    protected override void OnDamageTaken(float finalDamage, EquipableItem weapon)
    {
        if (personalUI != null)
        {
            personalUI.UpdateBars(true, false);
        }

        StopAllCoroutines();

        if (bloodEffect != null)
        {
            bloodEffect.Play();
        }

        StartCoroutine(FlashRedOnDamage());
    }

    public void HandleHunger(bool _isHungry)
    {
        isHungry = _isHungry;
        if (_isHungry)
        {
            ReduceHealthAndMoraleDueToHunger();
        }
        else
        {
            HealFromFood(healthRegenFromFood, moraleRegenFromFood);
        }
    }

    /// <summary>
    /// Shared hunger mode. hungerFraction 0 = fully fed, 1 = fully starving.
    /// Damage and morale loss scale linearly — missing 1/3 food = 33% of full hunger damage.
    /// </summary>
    public void HandleSharedHunger(float hungerFraction)
    {
        isHungry = hungerFraction > 0f;

        if (hungerFraction <= 0f)
        {
            HealFromFood(healthRegenFromFood, moraleRegenFromFood);
            return;
        }

        float minDmg = SettlementManager.Instance != null ? SettlementManager.Instance.minStarvationDamage : 5f;
        float fullDamage = Mathf.Max(currentHealth / 2f, minDmg);
        TakeDamage(fullDamage * hungerFraction, null, true);
        ChangeMorale(-10f * hungerFraction);
        personalUI.ShowSpeech(hungerFraction >= 1f ? "I'm starving..." : "I'm hungry...", 2.0f);
    }

    private void ReduceHealthAndMoraleDueToHunger()
    {
        // Reduce health and morale due to hunger
        int minDmg = SettlementManager.Instance != null ? Mathf.FloorToInt(SettlementManager.Instance.minStarvationDamage) : 5;
        int hungerDamage = Mathf.FloorToInt(currentHealth / 2);
        hungerDamage = Mathf.Max(hungerDamage, minDmg);
        TakeDamage(hungerDamage, null, true); // Lose health due to hunger (true damage bypasses defense)
        ChangeMorale(-10f); // Lose 10 morale due to hunger
        personalUI.ShowSpeech("I'm starving...", 2.0f);
    }

    private void HealFromFood(float healthAmount, float moraleAmount)
    {
        // Apply Gefjon's Blessing heal speed bonus
        if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance.IsActive)
        {
            healthAmount *= (1f + DeathTypeBuff.Instance.GetHealSpeedPercent() / 100f);
        }
        Heal(healthAmount);
        ChangeMorale(moraleAmount);
        personalUI.ShowSpeech("That was a good meal!", 2.0f);
    }
    
    private IEnumerator FlashRedOnDamage()
    {
        _material.SetFloat("_StrongTintFade", 1);
        yield return new WaitForSeconds(0.1f);
        _material.SetFloat("_StrongTintFade", 0f);
    }
    
    public void ChangeMorale(float amount)
    {
        morale = Mathf.Clamp(morale + amount, 0, maxMorale);
    }

    public override void Die()
    {
        if(isDead) return;
        base.Die();
        Debug.Log($"{villagerName} has died at age {age:F1}");

        // If the Jarl is dying, trigger succession
        if (isJarl && JarlManager.Instance != null)
        {
            // Determine cause of death
            DeathCause cause = DeathCause.Other;
            if (lastDamageWasCombat)
                cause = DeathCause.Combat;
            else if (age >= lifeExpectancy)
                cause = DeathCause.OldAge;
            else if (isHungry)
                cause = DeathCause.Starvation;
            else if (isCold)
                cause = DeathCause.Cold;

            JarlManager.Instance.OnCurrentJarlDied(cause);
        }

        // Handle villager death
        if (assignedBuilding != null)
        {
            assignedBuilding.RemoveWorker(this);
        }

        // Clear partner reference
        if (partner != null)
        {
            partner.partner = null;
            partner = null;
        }

        // Unregister from settlement manager
        if (SettlementManager.Instance != null)
        {
            SettlementManager.Instance.UnregisterVillager(this);
        }

        // Clear personal UI reference
        if (personalUI != null)
        {
            personalUI.Hide();
            //personalUI = null;
        }

        _controller.SetDead(true);
    }

    /// <summary>
    /// Remove the villager game object at the end of death animation with animation event
    /// </summary>
    public void RemoveAtEndofAnimation()
    {
        Destroy(gameObject, 0.5f);
    }

    #endregion

    // Public method to set age and parents for initial population
    public void Initialize(float startAge, Gender startGender, Villager parentA = null, Villager parentB = null)
    {
        age = startAge;
        gender = startGender;
        parent1 = parentA;
        parent2 = parentB;
        UpdateLifeStage();

        if (parentA != null && parentB != null)
        {
            skills = VillagerSkills.Inherit(parentA.skills, parentB.skills);
        }
    }
    
    /// <summary>
    /// Assign a random sprite variant from available options
    /// </summary>
    private void AssignRandomSpriteVariant()
    {
        var swapper = GetComponent<SpriteLibrarySwapper>();
        if (swapper != null)
        {
            // Get random variant from the swapper's available variants
            spriteVariant = swapper.GetRandomVariant();
        }
    }

    /// <summary>
    /// Apply the current sprite variant to the character
    /// </summary>
    private void ApplySpriteVariant()
    {
        var swapper = GetComponent<SpriteLibrarySwapper>();
        if (swapper != null && !string.IsNullOrEmpty(spriteVariant))
        {
            swapper.SetVariant(spriteVariant);
        }
    }

    /// <summary>
    /// Inherit sprite variant from parents with chance of mutation
    /// </summary>
    private string InheritSpriteVariant(string parent1Variant, string parent2Variant)
    {
        // 80% chance to inherit from a parent, 20% chance for random mutation
        if (Random.value < 0.8f)
        {
            // Randomly pick one parent's variant
            return Random.value > 0.5f ? parent1Variant : parent2Variant;
        }
        else
        {
            // Mutation - get a random variant
            var swapper = GetComponent<SpriteLibrarySwapper>();
            return swapper != null ? swapper.GetRandomVariant() : parent1Variant;
        }
    }

    #region Skill Bonuses

    private float baseMaxHealth;

    /// <summary>
    /// Apply skill tree bonuses to stats (max health, etc.)
    /// </summary>
    public void ApplySkillBonuses()
    {
        // Store base max health if not already stored
        if (baseMaxHealth == 0f)
            baseMaxHealth = maxHealth;

        float newMaxHealth = baseMaxHealth;

        // Jarl-only: apply skill tree flat/percent bonuses
        if (isJarl && SkillTreeManager.Instance != null)
        {
            float healthFlat = SkillTreeManager.Instance.GetEffect(SkillEffectType.MaxHealthFlat);
            float healthPercent = SkillTreeManager.Instance.GetEffect(SkillEffectType.MaxHealthPercent);
            newMaxHealth = (newMaxHealth + healthFlat) * (1f + healthPercent / 100f);
        }

        // Resilient Blood applies to ALL villagers (+10% max HP)
        if (RunestoneManager.Instance != null)
        {
            newMaxHealth *= RunestoneManager.Instance.GetMaxHealthMultiplier();
        }

        // Wound HP penalties (stacking, applied after all bonuses)
        if (activeWounds.Count > 0)
        {
            float woundHPPenaltyPct = WoundDatabase.TotalMaxHPPenaltyPct(activeWounds);
            newMaxHealth *= (1f - woundHPPenaltyPct / 100f);
        }

        maxHealth = newMaxHealth;

        // Ensure current health doesn't exceed new max
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    private void OnSkillTreeChanged(SkillDefinitionSO skill)
    {
        ApplySkillBonuses();
        if (personalUI != null)
        {
            personalUI.UpdateBars(true, false);
        }
    }

    /// <summary>
    /// Called when this villager becomes or stops being the Jarl
    /// </summary>
    public void OnJarlStatusChanged(bool becameJarl)
    {
        if (becameJarl && SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnSkillUnlocked += OnSkillTreeChanged;
        }
        else if (!becameJarl && SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnSkillUnlocked -= OnSkillTreeChanged;
        }
        ApplySkillBonuses();
    }

    public void CheckBuildingInteractionZone()
    {
        if (!isJarl)
        {
            GetComponentInChildren<WorldInteractionZone>().gameObject.SetActive(false);
        }
    }

    #endregion

    #region Succession Helpers

    /// <summary>
    /// Get all children of this villager from the settlement
    /// </summary>
    public System.Collections.Generic.List<Villager> GetChildren()
    {
        var children = new System.Collections.Generic.List<Villager>();
        if (SettlementManager.Instance == null) return children;

        foreach (var villager in SettlementManager.Instance.GetAllVillagers())
        {
            if (villager.parent1 == this || villager.parent2 == this)
            {
                children.Add(villager);
            }
        }
        return children;
    }

    /// <summary>
    /// Get all siblings (villagers sharing at least one parent)
    /// </summary>
    public System.Collections.Generic.List<Villager> GetSiblings()
    {
        var siblings = new System.Collections.Generic.List<Villager>();
        if (SettlementManager.Instance == null) return siblings;

        foreach (var villager in SettlementManager.Instance.GetAllVillagers())
        {
            if (villager == this) continue;

            bool sharesParent = false;
            if (parent1 != null && (villager.parent1 == parent1 || villager.parent2 == parent1))
                sharesParent = true;
            if (parent2 != null && (villager.parent1 == parent2 || villager.parent2 == parent2))
                sharesParent = true;

            if (sharesParent)
            {
                siblings.Add(villager);
            }
        }
        return siblings;
    }

    /// <summary>
    /// Calculate total skill score for succession ranking
    /// </summary>
    public float GetTotalSkillScore()
    {
        // Combat weighted higher for Jarl candidates
        return skills.combat * 2f +
               skills.farming +
               skills.fishing +
               skills.mining +
               skills.woodcutting +
               skills.crafting +
               skills.sailing;
    }

    #endregion

    #region Scene Change Support

    /// <summary>
    /// Reinitialize references after returning from a raid or scene change.
    /// Called by VillagerSpawner when villagers are reintegrated.
    /// </summary>
    public void ReinitializeAfterSceneChange()
    {
        // Re-cache the settlement manager reference (it's a new instance in the new scene)
        _settlementManager = SettlementManager.Instance;

        // Re-cache material reference
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            _material = renderer.material;
        }

        // Clear raid flag
        isOnRaid = false;

        // Update personal UI bars if it exists
        if (personalUI != null)
        {
            personalUI.UpdateBars(true, true);
        }

        Debug.Log($"Villager {villagerName} reinitialized after scene change");
    }

    #endregion
}

public enum Gender
{
    Male,
    Female
}

public enum LifeStage
{
    Young,
    Mature,
    Dead
}