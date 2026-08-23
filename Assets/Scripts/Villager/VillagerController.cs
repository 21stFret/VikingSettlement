using System;
using UnityEngine;

public class VillagerController : CharacterBase
{
    private Villager villagerData;

    protected override void Awake()
    {
        base.Awake();
        villagerData = GetComponent<Villager>();
        characterFaction = Faction.Player;
        // Walk/chase speed live on the AI component (CharacterAI.MoveSpeed/ChaseSpeed) — shared
        // with EnemyAIBase instead of this class keeping its own separately-named pair.
        moveSpeed = AI != null ? AI.MoveSpeed : moveSpeed;
    }

    /// <summary>
    /// Combat skill multiplier — scales attack speed, damage, and block cooldown.
    /// Ranges from 0.5× at skill 1 to 1.5× at skill 10.
    /// </summary>
    public float GetCombatSkillMultiplier()
    {
        if (villagerData == null) return 0.5f;
        return Mathf.Lerp(0.75f, 1.5f, (villagerData.skills.combat - 1f) / 9f);
    }

    /// <summary>
    /// Attack delay reduced by combat skill — higher skill = faster attacks.
    /// </summary>
    public override float GetAttackDelay()
    {
        return base.GetAttackDelay() / GetCombatSkillMultiplier();
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
            var personalUI = villagerData.personalUI.transform;
            float scaleX = Mathf.Abs(personalUI.localScale.x);
            personalUI.localScale = new Vector3(
                faceRight ? -scaleX : scaleX,
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

    /// <summary>
    /// Villager-specific bits only — shared hit resolution (damage via CalculateAttackDamage
    /// below, TakeDamage, OnHitBy notify, knockback) all happens once in base. See B52 in
    /// manager_bugs.md: this used to call base AND redo the whole thing itself, which meant
    /// TakeDamage/OnHitBy fired twice per villager swing (the second TakeDamage silently
    /// swallowed by TargetHealth's invincibility window, so this class's own damage formula
    /// never actually applied) and OnHitBy's reactive listeners ran twice.
    /// </summary>
    public override void OnHitTarget(Collider2D hit)
    {
        if (villagerData == null || isRolling) return;

        base.OnHitTarget(hit);
        CheckParryAndStun(hit);
    }

    /// <summary>
    /// Weapon strength + the villager's own strength stat, scaled by combat skill (+ morale)
    /// via Villager.GetSkillMultiplier — the canonical combat-skill multiplier per team decision
    /// (2026-08-23); GetCombatSkillMultiplier() above stays reserved for attack-speed scaling only.
    /// Reduced by any active wound penalty.
    /// </summary>
    protected override float CalculateAttackDamage(EquipableItem attackWeapon)
    {
        float weaponDamage = attackWeapon?.strength ?? 0f;
        float damage = (weaponDamage + villagerData.combatStats.strength) * villagerData.GetSkillMultiplier(JobType.Warrior);

        if (villagerData.activeWounds.Count > 0)
        {
            float woundDmgPenaltyPct = WoundDatabase.TotalAttackDamagePenaltyPct(villagerData.activeWounds);
            damage *= (1f - woundDmgPenaltyPct / 100f);
        }

        return damage;
    }
}
