using UnityEngine;

[System.Serializable]
public class VillagerSkills
{
    public float intelligence = 1f; //increases learning speed
    public float learningRate = 1f; //multiplier for how fast skills improve
    public float farming = 1f;
    public float fishing = 1f;
    public float mining = 1f;
    public float woodcutting = 1f;
    public float crafting = 1f;
    public float combat = 1f;
    public float sailing = 1f;

    public float GetSkillForJob(JobType jobType)
    {
        switch (jobType)
        {
            case JobType.Farmer: return farming;
            case JobType.Fisherman: return fishing;
            case JobType.Miner: return mining;
            case JobType.Woodcutter: return woodcutting;
            case JobType.Smith: return crafting;
            case JobType.Carpenter: return woodcutting;
            case JobType.Weaver: return crafting;
            case JobType.Tanner: return farming;
            case JobType.Shipwright: return sailing;
            case JobType.Brewer: return crafting;
            case JobType.Warrior: return combat;
            case JobType.Archer: return combat;
            default: return 1f;
        }
    }
    
    public void ImproveSkill(JobType jobType)
    {
        float _localLearningRate = Mathf.Max(0.1f, learningRate * (intelligence / 10f));
        float amount = 0.05f * _localLearningRate; // Base improvement amount
        switch (jobType)
        {
            case JobType.Farmer: farming += amount; break;
            case JobType.Fisherman: fishing += amount; break;
            case JobType.Miner: mining += amount; break;
            case JobType.Woodcutter: woodcutting += amount; break;
            case JobType.Smith: crafting += amount; break;
            case JobType.Carpenter: woodcutting += amount; break;
            case JobType.Weaver: crafting += amount; break;
            case JobType.Tanner: farming += amount; break;
            case JobType.Shipwright: sailing += amount; break;
            case JobType.Brewer: crafting += amount; break;
            case JobType.Warrior: combat += amount; break;
            case JobType.Archer: combat += amount; break;
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
        inheritedSkills.fishing = (parent1.fishing + parent2.fishing) / 2f;
        inheritedSkills.mining = (parent1.mining + parent2.mining) / 2f;
        inheritedSkills.woodcutting = (parent1.woodcutting + parent2.woodcutting) / 2f;
        inheritedSkills.crafting = (parent1.crafting + parent2.crafting) / 2f;
        inheritedSkills.combat = (parent1.combat + parent2.combat) / 2f;
        inheritedSkills.sailing = (parent1.sailing + parent2.sailing) / 2f;
        inheritedSkills.intelligence = (parent1.intelligence + parent2.intelligence) / 2f;
        
        // Add small random variation (-10% to +10%)
        inheritedSkills.farming *= Random.Range(0.9f, 1.1f);
        inheritedSkills.fishing *= Random.Range(0.9f, 1.1f);
        inheritedSkills.mining *= Random.Range(0.9f, 1.1f);
        inheritedSkills.woodcutting *= Random.Range(0.9f, 1.1f);
        inheritedSkills.crafting *= Random.Range(0.9f, 1.1f);
        inheritedSkills.combat *= Random.Range(0.9f, 1.1f);
        inheritedSkills.sailing *= Random.Range(0.9f, 1.1f);
        
        // Ensure minimum skill of 0.5
        inheritedSkills.farming = Mathf.Max(0.5f, inheritedSkills.farming);
        inheritedSkills.fishing = Mathf.Max(0.5f, inheritedSkills.fishing);
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
    Steward,
    Farmer,
    Fisherman,
    Woodcutter,
    Miner,
    Smith,
    Carpenter,
    Weaver,
    Tanner,
    Warrior,
    Archer,
    Shipwright,
    Merchant,
    Healer,
    Shaman,
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

public enum SkillType
{
    Farming,
    Fishing,
    Mining,
    Woodcutting,
    Crafting,
    Combat,
    Sailing
}