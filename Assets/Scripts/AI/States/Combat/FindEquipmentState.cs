using UnityEngine;

/// <summary>
/// Mid-combat interrupt: walks to and equips the nearest dropped, unequipped shield, then hands
/// back to combat. Entered via CombatAIBase.HandleShieldBroken when the currently-equipped shield's
/// durability hits zero while genuinely engaged in a fight — generalizes what used to be a
/// Raider-only bolted-on MonoBehaviour (RaiderBehaviour) racing the FSM's own movement calls every
/// frame into a proper state any shielded combatant (villager or enemy) can use.
///
/// Distinct from VillagerPrepareCombatState, which handles the PRE-combat "find a shield before
/// engaging" case — they share the underlying scan (CombatAIBase.FindNearestDroppedShield) but are
/// triggered differently (FSM entry vs. event-driven interrupt) and aren't merged.
/// </summary>
public class FindEquipmentState : AIStateBase
{
    private const float SearchRadius = 5f;

    public override void OnUpdate(CharacterAI ai)
    {
        if (ai.CurrentTarget == null || IsTargetDead(ai))
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            if (ai is CombatAIBase combat)
                combat.ForceTargetSearch();
            else
                ai.ChangeState(new VillagerIdleState());
            return;
        }

        if (ai.Controller.shield != null)
        {
            // Already re-equipped via some other path — nothing left to do here.
            ResumeCombat(ai);
            return;
        }

        GameObject closest = CombatAIBase.FindNearestDroppedShield(ai.transform.position, SearchRadius);
        if (closest == null)
        {
            // Nothing nearby to grab — fall back into combat unshielded, matching
            // VillagerPrepareCombatState's existing fallback.
            ResumeCombat(ai);
            return;
        }

        ai.Controller.MoveTo(closest.transform.position);
        if (Vector2.Distance(ai.transform.position, closest.transform.position) < 0.1f)
            ai.Controller.itemAttachment.EquipShield(closest);
    }

    /// <summary>Routed through GetApproachState() so a ranged fighter (ArcherAI) whose shield
    /// breaks mid-fight resumes RangedCombatState, not melee approach — see CombatAIBase.</summary>
    private static void ResumeCombat(CharacterAI ai)
    {
        if (ai is CombatAIBase combat)
            ai.ChangeState(combat.GetApproachState());
        else
            ai.ChangeState(new VillagerIdleState());
    }

    private static bool IsTargetDead(CharacterAI ai) =>
        ai.CurrentTarget?.GetComponent<TargetHealth>()?.IsDead() == true;
}
