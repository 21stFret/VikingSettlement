using UnityEngine;

[CreateAssetMenu(fileName = "CombatFighterStats", menuName = "AI/Combat Fighter Stats")]
public class CombatFighterStats : ScriptableObject
{
    [Range(0f, 1f)]
    [Tooltip("Probability of committing to an attack during pressure phase.")]
    public float AggressionLevel = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("1 = always block, 0 = always dodge.")]
    public float BlockVsDodgeRatio = 0.7f;

    [Tooltip("Orbit distance when no melee slot is available on the target.")]
    public float ThreatCircleDistance = 2.5f;

    [Tooltip("Seconds in pressure state before attempting an attack roll.")]
    public float PressureTime = 1.5f;

    [Tooltip("Seconds in recovering state before returning to pressure.")]
    public float RecoveryTime = 0.8f;

    [Tooltip("Distance to step back during retreat.")]
    public float RetreatDistance = 3f;

    [Tooltip("How many reactive blocks this fighter can raise before its guard breaks.")]
    public int MaxBlockCharges = 1;

    [Tooltip("Seconds to fully recover block charges once depleted.")]
    public float BlockCooldown = 5f;
}
