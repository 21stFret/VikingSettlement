using UnityEngine;

public class ShieldDraugrEnemy : Enemy
{

    public override void InitializeEnemyStats()
    {
        base.InitializeEnemyStats();

        var ia = GetComponent<ItemAttachment>();
        var shield = ia.shield.GetComponent<EquipableItem>();
        int rand = shield.maxDurability / Random.Range(2, 3);
        shield.SetDurability(rand);
    }
}
