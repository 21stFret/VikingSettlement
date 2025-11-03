using System;
using UnityEngine;

public class VillagerController : CharacterController
{
    private Villager villagerData;

    protected override void Awake()
    {
        base.Awake();
        villagerData = GetComponent<Villager>();
    }

    protected override void Update()
    {
        // Don't move if dead
        if (villagerData != null && villagerData.IsDead())
        {
            movement = Vector2.zero;
            canMove = false;
            return;
        }

        base.Update();
    }

    /// <summary>
    /// Override movement to check if villager is alive
    /// </summary>
    public override void MoveTo(Vector2 destination)
    {
        if (villagerData != null && villagerData.IsDead()) return;
        base.MoveTo(destination);
    }

    /// <summary>
    /// Override attack to check if villager is alive
    /// </summary>
    public override void Attack()
    {
        if (villagerData != null && villagerData.IsDead()) return;

        // Stop movement during attack
        Stop();

        base.Attack();
    }

    protected override void OnHitTarget(Collider2D hit)
    {
        var target = hit.GetComponent<TargetHealth>();
        if (target != null && villagerData != null)
        {
            // Use villager's damage stat instead of weapon damage
            float weaponDamage = weapon?.strength ?? 0f;
            float villagerDamage = villagerData.combatStats.strength;
            float damage = weaponDamage + villagerDamage;
            print($"Villager {villagerData.villagerName} attacked {hit.name} for {damage} damage!");
            
            target.TakeDamage(damage);

        }
    }
}
