using UnityEngine;

public class JarlAI : VillagerAIBase
{
    [Header("Jarl Overrides")]
    [SerializeField] private float jarlDetectionRange = 12f;
    [SerializeField] private float jarlFleeThreshold  = 15f;

    public override float DetectionRange      => jarlDetectionRange;
    public override float FleeHealthThreshold => jarlFleeThreshold;

    public override bool IsCombatJob() => true;

    protected override AIStateBase GetInitialState() => new VillagerIdleState();
}
