using UnityEngine;

/// <summary>
/// Enemy-specific controller that extends the base CharacterBase
/// </summary>
public class EnemyController : CharacterBase
{
    // enemyData: health/dead-state only (TargetHealth-derived). Combat stats (range/damage/
    // cooldown) live on enemyAI now — see EnemyAIBase.
    private Enemy       enemyData;
    private EnemyAIBase enemyAI;

    protected override void Awake()
    {
        base.Awake();
        enemyData = GetComponent<Enemy>();
        enemyAI   = GetComponent<EnemyAIBase>();
        // characterFaction is no longer forced here — each enemy prefab/instance is configured
        // with its own clan (Draugr, Raider1, Raider2, ...) via the Inspector, so different
        // hostile factions actually fight each other instead of all being lumped into one.
    }

    public override float GetAttackDelay()
    {
        float delay = enemyAI != null ? enemyAI.AttackCooldown : 1.5f;
        if (weapon != null) delay += weapon.attackSpeed;
        return Mathf.Max(0.1f, delay);
    }

    protected override void Update()
    {
        // Don't move if dead
        if (enemyData != null && enemyData.IsDead())
        {
            movement = Vector2.zero;
            canMove = false;
            return;
        }

        base.Update();
    }

    /// <summary>
    /// Override movement to check if enemy is alive
    /// </summary>
    public override void MoveTo(Vector2 destination)
    {
        if (enemyData != null && enemyData.IsDead()) return;
        base.MoveTo(destination);
    }

    /// <summary>
    /// Override attack to check if enemy is alive
    /// </summary>
    public override void Attack()
    {
        if (enemyData != null && enemyData.IsDead()) return;

        // Stop movement during attack
        Stop();

        base.Attack();
    }

    /// <summary>
    /// Enemy-specific bits only — shared hit resolution (damage via CalculateAttackDamage below,
    /// TakeDamage, OnHitBy notify, knockback) all happens once in base. See B52 in
    /// manager_bugs.md — this previously skipped base entirely and duplicated that logic here,
    /// which happened to be correct for enemies (no Player-only bonuses apply) but meant the
    /// shared game-active guard and knockback-on-hit lived in two separate implementations.
    /// </summary>
    public override void OnHitTarget(Collider2D hit)
    {
        if (enemyData == null) return;

        base.OnHitTarget(hit);
        CheckParryAndStun(hit);
    }

    protected override float CalculateAttackDamage(EquipableItem attackWeapon)
    {
        float damage = enemyAI != null ? enemyAI.Damage : 0f;
        float weaponDamage = attackWeapon != null ? attackWeapon.strength : 0f;
        return damage + weaponDamage;
    }


    public override void OnHitBy(CharacterBase attacker)
    {
        base.OnHitBy(attacker); // fires OnHitByAttacker → CombatAIBase.HandleHitBy
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw enemy-specific attack range
        if (enemyAI != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyAI.AttackRange);
        }
    }
}
