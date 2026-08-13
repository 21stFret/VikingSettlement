/// <summary>
/// Ranged enemy AI. Extends EnemyAIBase but never claims a melee engagement slot on its target —
/// locking onto a target is purely a targeting/facing concern for a ranged fighter, not a physical
/// "occupy one of the target's MaxAttackers slots" concern the way it is for melee. See
/// RangedCombatState for the actual positioning/firing behaviour (larger AttackRange, holds at
/// range, retreats and keeps firing if the target closes past CombatFighterStats.MinEngageRange).
/// </summary>
public class ArcherAI : EnemyAIBase
{
    protected override AIStateBase GetInitialState() => new IdleState();

    // Both hooks map to the same state — ranged combat has no melee-style "travel to a slot, then
    // hold" two-phase structure to mirror, so there's nothing to differentiate between "just
    // acquired" and "settled" here. See CombatBlockState.ResumeState's dependency note below.
    internal override AIStateBase GetApproachState() => new RangedCombatState();
    internal override AIStateBase GetPressureState() => new RangedCombatState();

    protected override void OnCurrentTargetChanged(CharacterBase newTarget)
    {
        if (Controller == null || newTarget == null) return;

        // Deliberately skip TryClaimSlot — archers never occupy a melee slot on their target.
        // Still force the target to commit back so melee targets actually turn and fight the
        // archer, mirroring the base CombatAIBase behaviour for the reciprocal-lock half only.
        //
        // NOTE: CombatBlockState.ResumeState branches on CurrentSlotHost (always null for an
        // archer, since we never set it here) to pick between GetPressureState()/GetApproachState()
        // — that only resolves correctly because both hooks above return the same
        // RangedCombatState. Don't diverge them without re-checking that call site.
        CombatAIBase.TryForceReciprocalLock(Controller, newTarget);
    }
}
