using UnityEngine;

/// <summary>
/// Companion component on a Healer's Hut building.
/// Wound removal is purchase-based: call HealWound() to spend resources and
/// instantly remove one wound. Requires a worker assigned to the building.
/// Cost per wound: 3 Honey + 5 Gold.
/// </summary>
[RequireComponent(typeof(Building))]
public class HealerHut : MonoBehaviour
{
    public const int HoneyCost = 3;
    public const int GoldCost  = 5;

    private Building _building;

    private void Awake()
    {
        _building = GetComponent<Building>();
    }

    public bool HasWorker => _building != null && _building.assignedWorkers.Count > 0;

    /// <summary>
    /// Returns true if the settlement has enough resources to heal one wound
    /// and this hut has an assigned healer.
    /// </summary>
    public bool CanHeal()
    {
        if (!HasWorker) return false;
        if (ResourceManager.Instance == null) return false;
        return ResourceManager.Instance.GetResource(ResourceType.Honey) >= HoneyCost
            && ResourceManager.Instance.GetResource(ResourceType.Gold)  >= GoldCost;
    }

    /// <summary>
    /// Spend resources and immediately remove the specified wound from the villager.
    /// Returns true on success.
    /// </summary>
    public bool HealWound(Villager target, WoundType wound)
    {
        if (target == null || target.IsDead()) return false;

        if (!target.activeWounds.Contains(wound))
        {
            Debug.LogWarning($"HealerHut: {target.villagerName} does not have wound {wound}.");
            return false;
        }

        if (!CanHeal())
        {
            Debug.LogWarning("HealerHut: Not enough resources or no healer assigned.");
            return false;
        }

        ResourceManager.Instance.SpendResource(ResourceType.Honey, HoneyCost);
        ResourceManager.Instance.SpendResource(ResourceType.Gold,  GoldCost);
        target.RemoveWound(wound);

        Debug.Log($"HealerHut: Healed {wound} on {target.villagerName}. Cost: {HoneyCost} honey, {GoldCost} gold.");
        return true;
    }
}
