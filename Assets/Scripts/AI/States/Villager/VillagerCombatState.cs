using UnityEngine;

public class VillagerCombatState : AIStateBase
{
    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;

        if (ai.CurrentTarget == null || v.VillagerData == null)
        {
            ai.ReleaseEngagementSlot();
            ai.ChangeState(v.IsInRaidMode ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
            return;
        }

        var enemy = ai.CurrentTarget.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead())
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            ai.ChangeState(v.IsInRaidMode ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
            return;
        }

        float hp = v.VillagerData.currentHealth / v.VillagerData.maxHealth * 100f;
        float fleeAt = v.IsInRaidMode ? 15f : v.FleeHealthThreshold;
        if (hp < fleeAt)
        {
            ai.ChangeState(new VillagerFleeState());
            return;
        }

        float dist    = Vector2.Distance(v.transform.position, ai.CurrentTarget.position);
        var   threatCB = ai.CurrentTarget.GetComponent<CharacterBase>();
        var   fm       = FightManager.Instance;

        if (threatCB != null)
            ai.RegisterFightZone(threatCB);

        if (fm != null && fm.IsEngaged(threatCB))
        {
            if (threatCB != null && ai.CurrentSlotHost != threatCB)
            {
                ai.ReleaseEngagementSlot();
                v.TryClaimEngagementSlot(threatCB);
            }
        }
        else
        {
            ai.ReleaseEngagementSlot();
        }

        if (dist <= v.CombatEngageRange && v.VillagerController.weapon != null)
        {
            v.VillagerController.Stop();
            v.VillagerController.FaceTowards(ai.CurrentTarget.position);
            v.VillagerController.Attack();
        }
        else
        {
            Vector2 dest = ai.CurrentSlotHost != null
                ? ai.CurrentSlotHost.GetSlotWorldPos(v.VillagerController)
                : ai.FightZoneCenter;
            v.VillagerController.MoveTo(dest);
        }

        if (dist > ai.DetectionRange * 1.5f)
        {
            ai.ReleaseEngagementSlot();
            ai.CurrentTarget = null;
            ai.ChangeState(v.IsInRaidMode ? (AIStateBase)new VillagerFollowState() : new VillagerIdleState());
            return;
        }

        v.VillagerData.skills.ImproveSkill(JobType.Warrior);
    }
}
