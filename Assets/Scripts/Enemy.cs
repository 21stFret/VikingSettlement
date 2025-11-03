using UnityEngine;
using System.Collections;

public class Enemy : TargetHealth
{
    [Header("Enemy Info")]
    public string enemyName = "Raider";
    public EnemyType enemyType = EnemyType.Raider;

    [Header("Combat Stats")]
    public float damage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public float detectionRange = 10f;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float chaseSpeed = 2.5f;

    [Header("Loot")]
    public int goldReward = 10;
    public float lootChance = 0.3f;

    [Header("References")]
    [SerializeField] private ParticleSystem bloodEffect;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EnemyController _controller;
    private Material materialInstance;
    private Color originalColor;

    public VillagerPersonalUI personalUI;

    public enum EnemyType
    {
        Raider,
        Warrior,
        Berserker,
        Archer,
        Wolf
    }

    public override void Awake()
    {
        base.Awake();
        _controller = GetComponent<EnemyController>();

        // Create material instance for visual effects
        if (spriteRenderer != null)
        {
            materialInstance = spriteRenderer.material;
            originalColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        // Initialize enemy based on type
        InitializeEnemyStats();
    }

    private void InitializeEnemyStats()
    {
        currentHealth = maxHealth;
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);

        // Visual feedback
        if (bloodEffect != null)
        {
            bloodEffect.Play();
        }

        personalUI.UpdateBars(true, false);

        StartCoroutine(FlashRedOnDamage());

        Debug.Log($"{enemyName} took {amount} damage");
    }

    public override void Die()
    {
        base.Die();

        if (_controller != null)
        {
            _controller.SetDead(true);
        }

        // Drop loot
        if (Random.value < lootChance)
        {
            DropLoot();
        }

        Debug.Log($"{enemyName} has been defeated!");

        // Destroy after delay
        Destroy(gameObject, 5f);
    }

    private void DropLoot()
    {
        // TODO: Implement loot drop system
        // For now just log it
        Debug.Log($"{enemyName} dropped {goldReward} gold!");
    }

    private IEnumerator FlashRedOnDamage()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Get the damage this enemy deals
    /// </summary>
    public float GetDamage()
    {
        return damage;
    }

    /// <summary>
    /// Get the attack range of this enemy
    /// </summary>
    public float GetAttackRange()
    {
        return attackRange;
    }

    /// <summary>
    /// Get the detection range of this enemy
    /// </summary>
    public float GetDetectionRange()
    {
        return detectionRange;
    }

}
