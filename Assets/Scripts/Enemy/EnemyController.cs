using UnityEngine;

/// <summary>
/// Enemy-specific controller that extends the base CharacterBase
/// </summary>
public class EnemyController : CharacterBase
{
    private Enemy enemyData;

    protected override void Awake()
    {
        base.Awake();
        enemyData = GetComponent<Enemy>();
        // characterFaction is no longer forced here — each enemy prefab/instance is configured
        // with its own clan (Draugr, Raider1, Raider2, ...) via the Inspector, so different
        // hostile factions actually fight each other instead of all being lumped into one.
    }

    public override float GetAttackDelay()
    {
        float delay = enemyData != null ? enemyData.attackCooldown : attackDelay;
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
    protected override void OnHitTarget(Collider2D hit)
    {
        var target = hit.GetComponent<TargetHealth>();
        if (target != null && enemyData != null)
        {
            if (target.IsDead()) return;

            float damage = enemyData.GetDamage();
            float weaponDamage = 0f;
            if (weapon != null)
            {
                weaponDamage = weapon.strength;
            }
            float totalDamage = damage + weaponDamage;
            Debug.Log($"{enemyData.enemyName} attacked {hit.name} for {totalDamage} damage!");
            target.TakeDamage(totalDamage, weapon);
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
        if (enemyData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyData.GetAttackRange());
        }
    }
}
