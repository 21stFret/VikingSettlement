using UnityEngine;

public class IconManager : MonoBehaviour
{
    public static IconManager Instance { get; private set; }
    [Header("Job Icons")]
    [SerializeField] private Sprite farmerIcon;
    [SerializeField] private Sprite woodcutterIcon;
    [SerializeField] private Sprite minerIcon;
    [SerializeField] private Sprite fishermanIcon;
    [SerializeField] private Sprite craftingIcon;
    [SerializeField] private Sprite combatIcon;

    [Header("Resource Icons")]
    [SerializeField] private Sprite wheatIcon;
    [SerializeField] private Sprite fishIcon;
    [SerializeField] private Sprite woodIcon;
    [SerializeField] private Sprite stoneIcon;
    [SerializeField] private Sprite ironIcon;
    [SerializeField] private Sprite meadIcon;
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite weaponsIcon;
    [SerializeField] private Sprite planksIcon;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite honeyIcon;
    [SerializeField] private Sprite meatIcon;
    [SerializeField] private Sprite peltIcon;
    [SerializeField] private Sprite leatherIcon;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Get the icon sprite for a given job type
    /// </summary>
    public Sprite GetIconForJob(JobType jobType)
    {
        switch (jobType)
        {
            case JobType.Farmer: return farmerIcon;
            case JobType.Woodcutter: return woodcutterIcon;
            case JobType.Miner: return minerIcon;
            case JobType.Fisherman: return fishermanIcon;
            case JobType.Smith: return craftingIcon;
            case JobType.Carpenter: return woodcutterIcon;
            case JobType.Tanner: return farmerIcon;
            case JobType.Brewer: return craftingIcon;
            case JobType.Warrior: return combatIcon;
            case JobType.Archer: return combatIcon;
            default: return null;
        }
    }

    /// <summary>
    /// Get the icon sprite for a given resource type
    /// </summary>
    public Sprite GetIconForResource(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Wheat: return wheatIcon;
            case ResourceType.Fish: return fishIcon;
            case ResourceType.Wood: return woodIcon;
            case ResourceType.Stone: return stoneIcon;
            case ResourceType.Iron: return ironIcon;
            case ResourceType.Mead: return meadIcon;
            case ResourceType.Gold: return goldIcon;
            case ResourceType.Weapons: return weaponsIcon;
            case ResourceType.Planks: return planksIcon;
            case ResourceType.Shield: return shieldIcon;
            case ResourceType.Honey: return honeyIcon;
            case ResourceType.Meat: return meatIcon;
            case ResourceType.Pelts: return peltIcon;
            case ResourceType.Leather: return leatherIcon;
            default: return null;
        }
    }
}
