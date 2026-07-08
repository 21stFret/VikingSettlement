using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : TargetHealth
{
    [Header("Enemy Info")]
    public string enemyName = "Raider";

    [Header("Combat Stats")]
    public float damage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public string weaponName="";
    public string shieldName="";

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float chaseSpeed = 2.5f;

    [Header("Loot")]
    public int goldReward = 10;
    public float lootChance = 0.3f;
    public List<LootTableEntry> lootTable = new List<LootTableEntry>();

    [Header("XP")]
    public int xpReward = 25;

    [Header("References")]
    [SerializeField] private ParticleSystem bloodEffect;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public EnemyController _controller;
    private Material materialInstance;
    private Color originalColor;
    public EnemyPersonalUI personalUI;

    private bool initalized;

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

    public void InitializeEnemyStats()
    {
        if (initalized)
            return;

        initalized = true;
        currentHealth = maxHealth;

        var ia = GetComponent<ItemAttachment>();
        if (ia != null)
        {
            if(weaponName != null || weaponName != "")
            {
                ia.GiveWeaponByName(weaponName);
            }
            else
            {
                ia.GiveRandomWeapon();
            }

            if (shieldName != null || shieldName!="")
            {
                ia.GiveShieldByName(shieldName);
            }
            else
            {
                ia.GiveRandomShield();
            }
        }
    }

    /// <summary>
    /// Apply shield reduction to incoming damage.
    /// </summary>
    protected override float CalculateFinalDamage(float rawDamage, EquipableItem weapon)
    {
        // Active block: route all damage to shield durability, no HP damage.
        // Attacks from behind bypass the block entirely.
        if (_controller != null && _controller.shield != null && !_controller.shield.IsBroken)
        {
            if ((_controller.isBlocking || _controller.isParrying)
                && !_controller.IsAttackFromBehind(_controller.lastAttackerPosition))
            {
                int durDamage = _controller.isParrying
                    ? Mathf.CeilToInt(rawDamage * 0.5f)
                    : Mathf.CeilToInt(rawDamage);
                _controller.shield.TakeDurabilityDamage(durDamage);
                return 0f;
            }
        }

        return rawDamage;
    }

    /// <summary>
    /// Visual feedback when taking damage.
    /// </summary>
    protected override void OnDamageTaken(float finalDamage, EquipableItem weapon)
    {
        if (bloodEffect != null)
        {
            bloodEffect.Play();
        }

        if (personalUI != null)
        {
            personalUI.UpdateBars(true, false);
        }

        StartCoroutine(FlashRedOnDamage());
    }

    public override void Die()
    {
        if (isDead) return;
        base.Die();

        if (_controller != null)
        {
            _controller.SetDead(true);
        }

        // Grant XP to player
        if (SkillTreeManager.Instance != null && xpReward > 0)
        {
            SkillTreeManager.Instance.AddXP(xpReward);
        }

        // Drop loot
        if (UnityEngine.Random.value < lootChance)
        {
            DropLoot();
        }

        Debug.Log($"{enemyName} has been defeated!");

        // Destroy after delay
        personalUI.enabled = false;
        Destroy(gameObject, 5f);
    }

    private void DropLoot()
    {
        if (LootDropManager.Instance != null)
        {
            LootDropManager.Instance.SpawnDrops(transform.position, lootTable);
        }
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

}
