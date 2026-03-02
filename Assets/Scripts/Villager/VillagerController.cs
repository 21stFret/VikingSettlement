using System;
using UnityEngine;

public class VillagerController : CharacterController
{
    private Villager villagerData;

    public float combatMoveSpeed;
    public float walkMoveSpeed;

    protected override void Awake()
    {
        base.Awake();
        villagerData = GetComponent<Villager>();
        characterFaction = Faction.Player;
        useReactiveBlocking = true;
    }

    /// <summary>
    /// Combat skill multiplier — scales attack speed, damage, and block cooldown.
    /// Ranges from 0.5× at skill 1 to 1.5× at skill 10.
    /// </summary>
    private float GetCombatSkillMultiplier()
    {
        if (villagerData == null) return 0.5f;
        return Mathf.Lerp(0.5f, 1.5f, (villagerData.skills.combat - 1f) / 9f);
    }

    /// <summary>
    /// Attack delay reduced by combat skill — higher skill = faster attacks.
    /// </summary>
    public override float GetAttackDelay()
    {
        return base.GetAttackDelay() / GetCombatSkillMultiplier();
    }

    /// <summary>
    /// Block cooldown reduced by combat skill — higher skill = charges refill faster.
    /// </summary>
    protected override float GetEffectiveBlockCooldown()
    {
        return base.GetEffectiveBlockCooldown() / GetCombatSkillMultiplier();
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

    protected override void FlipSprite(bool faceRight)
    {
        base.FlipSprite(faceRight);
        if (villagerData != null)
        {
            var personalUI = villagerData.personalUI.textCanvas.transform;
            personalUI.localScale = new Vector3(
                faceRight ? -1f : 1f,
                personalUI.localScale.y,
                personalUI.localScale.z);
        }
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
            float weaponDamage = weapon?.strength ?? 0f;
            float villagerDamage = villagerData.combatStats.strength;
            float damage = (weaponDamage + villagerDamage) * GetCombatSkillMultiplier();
            print($"Villager {villagerData.villagerName} attacked {hit.name} for {damage} damage!");

            target.TakeDamage(damage, weapon);
            CheckParryAndStun(hit);
        }
    }
}
