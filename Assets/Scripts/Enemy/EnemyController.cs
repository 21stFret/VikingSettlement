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
    /// Override to add enemy-specific hit behavior (e.g., enemy damage)
    /// </summary>
    public override void OnHitTarget(Collider2D hit)
    {
        var target = hit.GetComponent<TargetHealth>();
        if (target != null && enemyData != null)
        {
            if (target.IsDead()) return;

            float damage = enemyAI != null ? enemyAI.Damage : 0f;
            float weaponDamage = 0f;
            if (weapon != null)
            {
                weaponDamage = weapon.strength;
            }
            float totalDamage = damage + weaponDamage;
            Debug.Log($"{enemyData.enemyName} attacked {hit.name} for {totalDamage} damage!");
            target.TakeDamage(totalDamage, weapon, attackerPos: (Vector2)transform.position);
            hit.GetComponent<CharacterBase>()?.OnHitBy(this);
            CheckParryAndStun(hit);
        }
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
