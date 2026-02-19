using UnityEngine;

/// <summary>
/// Handles wound chance rolling when significant HP damage is dealt in combat.
/// Wounds themselves are stored on each Villager; this manager owns the roll logic.
/// </summary>
public class WoundManager : MonoBehaviour
{
    public static WoundManager Instance { get; private set; }

    public const int MAX_WOUNDS = 3; // 4th wound kills

    // Damage-fraction thresholds from spec
    private const float THRESHOLD_LOW    = 0.30f;
    private const float THRESHOLD_MED    = 0.40f;
    private const float THRESHOLD_HIGH   = 0.50f;
    private const float THRESHOLD_SEVERE = 0.70f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// Called after HP damage is applied in combat. Rolls for a wound if the hit
    /// exceeded 30% of the target's max HP.
    /// </summary>
    public void TryApplyWound(Villager villager, float hpDamageDealt, float maxHP)
    {
        if (villager == null || villager.IsDead()) return;
        if (maxHP <= 0f) return;

        float fraction = hpDamageDealt / maxHP;
        if (fraction < THRESHOLD_LOW) return;

        float chance = GetWoundChance(fraction);
        if (Random.value > chance) return;

        int woundCount = System.Enum.GetValues(typeof(WoundType)).Length;
        WoundType wound = (WoundType)Random.Range(0, woundCount);
        villager.AddWound(wound);
    }

    private float GetWoundChance(float fraction)
    {
        if (fraction >= THRESHOLD_SEVERE) return 0.80f;
        if (fraction >= THRESHOLD_HIGH)   return 0.50f;
        if (fraction >= THRESHOLD_MED)    return 0.30f;
        return 0.15f;
    }
}
