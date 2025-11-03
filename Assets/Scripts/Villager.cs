using System.Collections;
using UnityEngine;

public class Villager : TargetHealth
{
    public string villagerName;
    public JobType currentJob = JobType.None;
    public Building assignedBuilding;

    public VillagerSkills skills = new VillagerSkills();
    public float skillGainRate = 1f; // Multiplier for skill gain speed
    public CombatStats combatStats = new CombatStats();
    
    [Header("Status")]
    public float health = 100f;
    public float morale = 100f;
    public float maxMorale = 100f;
    
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

    public float agingTimer = 0f;
    private float reproductionTimer = 0f;

    [Header("Appearance")]
    public string spriteVariant = ""; // e.g. "blue_villager", "red_villager"

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

    private void Start()
    {
        _settlementManager = SettlementManager.Instance;
        _material = GetComponentInChildren<Renderer>().material;
        _controller = GetComponent<CharacterController>();

        if (string.IsNullOrEmpty(villagerName))
        {
            villagerName = VillagerNameGenerator.GenerateNorseName();
        }
        
        // Set initial life stage based on age
        UpdateLifeStage();

        // Register with settlement manager
        if (_settlementManager != null)
        {
            _settlementManager.RegisterVillager(this);
        }

        health = maxHealth;
        morale = maxMorale;

        // add random to speech timer so not all villagers speak at once
        _timeSinceLastSpoke = Random.Range(0f, _speechCooldown);

        if (string.IsNullOrEmpty(spriteVariant))
        {
            AssignRandomSpriteVariant();
        }
        else
        {
            ApplySpriteVariant();
        }
    }

    private void Update()
    {
        // Age the villager over time
        agingTimer += Time.deltaTime;
        if (agingTimer >= _settlementManager.ageingInterval) // Age once per real-time second (adjustable)
        {
            agingTimer = 0f;
            Age(_settlementManager.ageingAmount); // Increase age by 0.1 years per second
        }

        // Handle reproduction timer
        if (currentLifeStage == LifeStage.Mature)
        {
            reproductionTimer += Time.deltaTime;
            if (reproductionTimer >= _settlementManager.reproductionInterval)
            {
                reproductionTimer = 0f;
                timeSinceLastChild += 0.1f;

                // Check for reproduction opportunity
                TryReproduce();
            }
        }

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
        return baseSkill * moraleModifier;
    }

    public void Work(float deltaTime)
    {
        // Only mature villagers can work
        if (currentLifeStage != LifeStage.Mature) return;
        if (currentJob == JobType.None || assignedBuilding == null) return;

        // Improve skill over time
        skills.ImproveSkill(currentJob, deltaTime * 0.01f * skillGainRate);
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
        
        // Must have waited long enough since last child
        if (timeSinceLastChild < _settlementManager.reproductionCooldown) return;
        
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
        child.villagerName = VillagerNameGenerator.GenerateNorseName();

        // Set parents
        child.parent1 = mother;
        child.parent2 = father;

        // Inherit skills from parents (mean of both parents)
        child.skills = VillagerSkills.Inherit(mother.skills, father.skills);

        // Inherit combat stats (mean of both parents)
        child.combatStats.strength = (mother.combatStats.strength + father.combatStats.strength) / 2f;
        child.combatStats.defense = (mother.combatStats.defense + father.combatStats.defense) / 2f;

        // Add some random variation to inherited traits
        child.skillGainRate = (mother.skillGainRate + father.skillGainRate) / 2f + Random.Range(-0.1f, 0.1f);
        child.skillGainRate = Mathf.Max(0.5f, child.skillGainRate); // Minimum 0.5

        child.currentJob = JobType.None;
        child.assignedBuilding = null;

        child.spriteVariant = InheritSpriteVariant(mother.spriteVariant, father.spriteVariant);
        child.ApplySpriteVariant();

        Debug.Log($"{mother.villagerName} and {father.villagerName} had a child: {child.villagerName}!");
    }

    #endregion

    #region Health Management

    public override void TakeDamage(float amount)
    {
        float damageafter = Mathf.Max(0, amount - combatStats.defense);
        print($"Raw Damage was {amount} after defense {damageafter}");
        base.TakeDamage(damageafter);
        health = currentHealth;
        personalUI.UpdateBars(true, false);
        StopAllCoroutines();
        bloodEffect.Play();
        StartCoroutine(FlashRedOnDamage());
    }
    
    private IEnumerator FlashRedOnDamage()
    {
        _material.SetFloat("_StrongTintFade", 1);
        yield return new WaitForSeconds(0.1f);
        _material.SetFloat("_StrongTintFade", 0f);
    }
    
    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
    }
    
    public void ChangeMorale(float amount)
    {
        morale = Mathf.Clamp(morale + amount, 0, maxMorale);
    }

    public override void Die()
    {
        base.Die();
        Debug.Log($"{villagerName} has died at age {age:F1}");

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