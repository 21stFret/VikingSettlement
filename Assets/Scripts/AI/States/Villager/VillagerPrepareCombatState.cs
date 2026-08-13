using UnityEngine;

public class VillagerPrepareCombatState : AIStateBase
{
    public override void OnUpdate(CharacterAI ai)
    {
        var v = (VillagerAIBase)ai;

        if (ai.CurrentTarget == null || v.VillagerData == null)
        {
            ai.ChangeState(new VillagerIdleState());
            return;
        }

        if (v.VillagerController.shield != null)
        {
            ai.ChangeState(new CombatApproachState());
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(v.transform.position, 5f);
        float closestDist = Mathf.Infinity;
        GameObject closestShield = null;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Shield")) continue;
            var item = h.GetComponent<EquipableItem>();
            if (item == null || item.isEquipped) continue;

            float d = Vector2.Distance(v.transform.position, h.transform.position);
            if (d < closestDist) { closestDist = d; closestShield = h.gameObject; }
        }

        if (closestShield != null)
        {
            v.VillagerController.MoveTo(closestShield.transform.position);
            if (Vector2.Distance(v.transform.position, closestShield.transform.position) < 0.1f)
                v.VillagerController.itemAttachment.EquipShield(closestShield);
        }
        else
        {
            ai.ChangeState(new CombatApproachState());
        }
    }
}
