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
    [SerializeField] private Sprite sailingIcon;

    [Header("Resource Icons")]
    [SerializeField] private Sprite wheatIcon;
    [SerializeField] private Sprite fishIcon;
    [SerializeField] private Sprite woodIcon;
    [SerializeField] private Sprite stoneIcon;
    [SerializeField] private Sprite ironIcon;
    [SerializeField] private Sprite meadIcon;
    [SerializeField] private Sprite weaponsIcon;
    [SerializeField] private Sprite planksIcon;

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
            case JobType.Weaver: return craftingIcon;
            case JobType.Tanner: return farmerIcon;
            case JobType.Shipwright: return sailingIcon;
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
            case ResourceType.Weapons: return weaponsIcon;
            case ResourceType.Planks: return planksIcon;
            default: return null;
        }
    }
}
