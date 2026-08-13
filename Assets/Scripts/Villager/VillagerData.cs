using UnityEngine;

[System.Serializable]
public class VillagerSkills
{
    public float intelligence = 1f; //increases learning speed
    public float learningRate = 1f; //multiplier for how fast skills improve
    public float farming = 1f;
    public float hunting = 1f;
    public float mining = 1f;
    public float woodcutting = 1f;
    public float crafting = 1f;
    public float combat = 1f;
    public float sailing = 1f;
    public float maxSkillLevel = 10f;

    public float GetSkillForJob(JobType jobType)
    {
        switch (jobType)
        {
            case JobType.Farmer: return farming;
            case JobType.Fisherman: return hunting;
            case JobType.Miner: return mining;
            case JobType.Woodcutter: return woodcutting;
            case JobType.Smith: return crafting;
            case JobType.Carpenter: return woodcutting;
            case JobType.Tanner: return hunting;
            case JobType.Brewer: return crafting;
            case JobType.Warrior: return combat;

            default: return 1f;
        }
    }

    public void SetSkillLevel(JobType jobType, int level)
    {
        switch (jobType)
        {
            case JobType.Farmer: farming = level; break;
            case JobType.Fisherman: hunting = level; break;
            case JobType.Miner: mining = level; break;
            case JobType.Woodcutter: woodcutting = level; break;
            case JobType.Smith: crafting = level; break;
            case JobType.Carpenter: woodcutting = level; break;
            case JobType.Tanner: hunting = level; break;
            case JobType.Brewer: crafting = level; break;
            case JobType.Warrior: combat = level; break;
        } 
    }
    
    public void ImproveJob(JobType jobType)
    {
        float _localLearningRate = Mathf.Max(0.1f, learningRate * (intelligence / 10f));
        float amount = 0.05f * _localLearningRate; // Base improvement amount
        float jobcurrentValue = GetSkillForJob(jobType);
        if (jobcurrentValue + amount > maxSkillLevel)
        {
            jobcurrentValue = maxSkillLevel;
            return;
        }

        switch (jobType)
        {
            case JobType.Farmer: farming += amount; break;
            case JobType.Fisherman: hunting += amount; break;
            case JobType.Miner: mining += amount; break;
            case JobType.Woodcutter: woodcutting += amount; break;
            case JobType.Smith: crafting += amount; break;
            case JobType.Carpenter: woodcutting += amount; break;
            case JobType.Tanner: hunting += amount; break;
            case JobType.Brewer: crafting += amount; break;
            case JobType.Warrior: combat += amount; break;
        }

    }
    
    /// <summary>
    /// Randomize all skills to a value within [min, max]. Called on fresh spawns.
    /// </summary>
    public void Randomize(float min = 1f, float max = 4f)
    {
        farming    = Random.Range(min, max);
        hunting    = Random.Range(min, max);
        mining     = Random.Range(min, max);
        woodcutting = Random.Range(min, max);
        crafting   = Random.Range(min, max);
        combat     = Random.Range(min, max);
        sailing    = Random.Range(min, max);
        intelligence = Random.Range(min, max);
    }

    /// <summary>
    /// Add a flat bonus to a number of randomly chosen skills.
    /// Used by the Education runestone.
    /// </summary>
    public void ApplyRandomSkillBonuses(int bonus, int count)
    {
        // All trainable skills as setter actions
        System.Action<float>[] setters = {
            v => farming += v,
            v => hunting += v,
            v => mining += v,
            v => woodcutting += v,
            v => crafting += v,
            v => combat += v,
            v => sailing += v
        };

        // Fisher-Yates partial shuffle to pick 'count' unique indices
        int[] indices = { 0, 1, 2, 3, 4, 5, 6 };
        int picks = Mathf.Min(count, indices.Length);
        for (int i = 0; i < picks; i++)
        {
            int j = Random.Range(i, indices.Length);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;

            setters[indices[i]](bonus);
        }
    }

    /// <summary>
    /// Create inherited skills from two parents (mean of both)
    /// </summary>
    public static VillagerSkills Inherit(VillagerSkills parent1, VillagerSkills parent2)
    {
        VillagerSkills inheritedSkills = new VillagerSkills();
        
        // Calculate mean of each skill
        inheritedSkills.farming = (parent1.farming + parent2.farming) / 2f;
        inheritedSkills.hunting = (parent1.hunting + parent2.hunting) / 2f;
        inheritedSkills.mining = (parent1.mining + parent2.mining) / 2f;
        inheritedSkills.woodcutting = (parent1.woodcutting + parent2.woodcutting) / 2f;
        inheritedSkills.crafting = (parent1.crafting + parent2.crafting) / 2f;
        inheritedSkills.combat = (parent1.combat + parent2.combat) / 2f;
        inheritedSkills.sailing = (parent1.sailing + parent2.sailing) / 2f;
        inheritedSkills.intelligence = (parent1.intelligence + parent2.intelligence) / 2f;
        
        // Add small random variation (-10% to +10%)
        inheritedSkills.farming *= Random.Range(0.9f, 1.1f);
        inheritedSkills.hunting *= Random.Range(0.9f, 1.1f);
        inheritedSkills.mining *= Random.Range(0.9f, 1.1f);
        inheritedSkills.woodcutting *= Random.Range(0.9f, 1.1f);
        inheritedSkills.crafting *= Random.Range(0.9f, 1.1f);
        inheritedSkills.combat *= Random.Range(0.9f, 1.1f);
        inheritedSkills.sailing *= Random.Range(0.9f, 1.1f);
        
        // Ensure minimum skill of 0.5
        inheritedSkills.farming = Mathf.Max(0.5f, inheritedSkills.farming);
        inheritedSkills.hunting = Mathf.Max(0.5f, inheritedSkills.hunting);
        inheritedSkills.mining = Mathf.Max(0.5f, inheritedSkills.mining);
        inheritedSkills.woodcutting = Mathf.Max(0.5f, inheritedSkills.woodcutting);
        inheritedSkills.crafting = Mathf.Max(0.5f, inheritedSkills.crafting);
        inheritedSkills.combat = Mathf.Max(0.5f, inheritedSkills.combat);
        inheritedSkills.sailing = Mathf.Max(0.5f, inheritedSkills.sailing);
        
        return inheritedSkills;
    }
}

[System.Serializable]
public class CombatStats
{
    public float strength = 5f;
    public float defense = 5f;
}

public enum JobType
{
    None,
    Jarl,
    Farmer,
    Fisherman,
    Woodcutter,
    Miner,
    Smith,
    Carpenter,
    Tanner,
    Warrior,
    Archer,
    Healer,
    Brewer
}

public enum VillagerState
{
    Idle,
    Working,
    Traveling,
    Resting,
    Eating,
    Sleeping,
    Socializing,
    Training,
    Fighting
}