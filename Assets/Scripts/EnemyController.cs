using UnityEngine;

/// <summary>
/// Enemy-specific controller that extends the base CharacterController
/// </summary>
public class EnemyController : CharacterController
{
    private Enemy enemyData;

    protected override void Awake()
    {
        base.Awake();
        enemyData = GetComponent<Enemy>();
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
            // Use enemy's damage stat instead of weapon damage
            float damage = enemyData.GetDamage();
            float weaponDamage = 0f;
            if (weapon != null)
            {
                weaponDamage = weapon.strength;
            }
            float totalDamage = damage + weaponDamage;
            Debug.Log($"{enemyData.enemyName} attacked {hit.name} for {totalDamage} damage!");
            target.TakeDamage(totalDamage);
        }
    }


    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw enemy-specific detection ranges
        if (enemyData != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyData.GetDetectionRange());

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyData.GetAttackRange());
        }
    }
}
