using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach to any object to make it harvestable when struck.
/// Yields resources based on weapon type used.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HarvestableResource : TargetHealth
{
    [Header("Resource Settings")]
    [Tooltip("Type of resource this object yields")]
    public ResourceType resourceType = ResourceType.Wood;

    [Tooltip("Base amount yielded per hit")]
    public float baseYield = 1;
    private float actualYield = 1;

    [Header("Tool Bonuses")]
    [Tooltip("Best tool for this resource")]
    public EquipableItem.ItemType preferredTool = EquipableItem.ItemType.Axe;

    [Tooltip("Multiplier when using preferred tool")]
    public float preferredToolMultiplier = 2f;

    [Tooltip("Multiplier when using wrong tool")]
    public float wrongToolMultiplier = 1f;

    [Header("Respawn Settings")]
    [Tooltip("Should this resource respawn after depletion?")]
    public bool canRespawn = false;

    [HideInInspector] 
    public bool pendingRespawn = false;

    [Tooltip("Time in seconds before respawning")]
    public float respawnTime = 60f;

    [Header("XP")]
    [Tooltip("XP granted to the Jarl's skill tree per successful harvest")]
    public int xpPerHarvest = 5;

    [Header("Visual Feedback")]
    [Tooltip("Particle effect to spawn on harvest")]
    public ParticleSystem harvestEffect;

    [Tooltip("Shake the object when hit")]
    public bool shakeOnHit = true;

    [Tooltip("Shake intensity")]
    public float shakeIntensity = 0.1f;
    private SpriteRenderer spriteRenderer;
    protected Vector3 originalPosition;
    public UnityEvent OnHit;
    public bool hideOnDeplete = false;

    public override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.localPosition;
        actualYield = 0f;
    }

    public virtual void ShakeOnHit(Transform _transform)
    {
        StartCoroutine(ShakeObject(_transform));
    }

    private IEnumerator ShakeObject(Transform _transform)
    {
        float shakeTimer = 0.2f;
        // Handle shake animation
        while (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            float shake = Mathf.Sin(shakeTimer * 50f) * shakeIntensity * (shakeTimer / 0.2f);
            _transform.localPosition = originalPosition + new Vector3(shake, 0, 0);
            yield return null;
        }
        _transform.localPosition = originalPosition;
    }

    public override void TakeDamage(float damage, EquipableItem weapon, bool trueDamage = false, Vector2 attackerPos = default)
    {
        if (isDead) return;
        base.TakeDamage(damage, weapon, trueDamage, attackerPos);

        Villager attacker = weapon != null ? weapon.GetComponentInParent<Villager>() : null;
        int yield = CalculateYield(weapon, attacker);

        // Add resources to pool
        if (ResourceManager.Instance != null && yield > 0)
        {
            ResourceManager.Instance.AddResource(resourceType, yield);
            Debug.Log($"Harvested {yield} {resourceType} from {gameObject.name}");
        }

        if (attacker != null && yield > 0)
        {
            if (SkillTreeManager.Instance != null && xpPerHarvest > 0)
                SkillTreeManager.Instance.AddXP(xpPerHarvest);

            attacker.skills.ImproveJob(GetJobTypeForResource());
        }

        OnHit?.Invoke();

        // Visual feedback
        if (harvestEffect != null)
        {
            harvestEffect.Play();
        }

        if (shakeOnHit)
        {
            ShakeOnHit(transform);
        }
    }

    /// <summary>
    /// Calculate resource yield based on weapon used
    /// </summary>
    private int CalculateYield(EquipableItem weapon, Villager attacker = null)
    {
        float multiplier = 1f;

        if (weapon != null)
        {
            if (weapon.itemType == preferredTool)
            {
                multiplier = preferredToolMultiplier;
            }
            else if (IsWrongTool(weapon.itemType))
            {
                multiplier = wrongToolMultiplier;
            }
        }
        else
        {
            multiplier = wrongToolMultiplier;
        }

        if (attacker != null)
        {
            JobType job = GetJobTypeForResource();
            multiplier *= attacker.GetSkillMultiplier(job);
        }

        actualYield += baseYield * multiplier;
        if (actualYield >= 1)
        {
            //remove all whole numbers and leave only decimal
            int yield = Mathf.FloorToInt(actualYield);
            actualYield -= yield;

            return yield;
        }
        return 0;
    }

    /// <summary>
    /// Check if this is a particularly bad tool for this resource
    /// </summary>
    private JobType GetJobTypeForResource()
    {
        switch (resourceType)
        {
            case ResourceType.Wood:  return JobType.Woodcutter;
            case ResourceType.Stone: return JobType.Miner;
            case ResourceType.Iron:  return JobType.Miner;
            case ResourceType.Wheat: return JobType.Farmer;
            case ResourceType.Fish:  return JobType.Fisherman;
            default:                 return JobType.Warrior;
        }
    }

    private bool IsWrongTool(EquipableItem.ItemType toolType)
    {
        // Swords are bad for trees and rocks
        if (resourceType == ResourceType.Wood && toolType == EquipableItem.ItemType.Sword)
            return true;
        if (resourceType == ResourceType.Stone && toolType == EquipableItem.ItemType.Sword)
            return true;

        return false;
    }

    public override void Die()
    {
        base.Die();
        Deplete();
    }

    /// <summary>
    /// Called when the resource is fully depleted
    /// </summary>
    protected virtual void Deplete()
    {
        Debug.Log($"{gameObject.name} depleted!");

        pendingRespawn = true;
        if (canRespawn) 
        {
            Invoke(nameof(Respawn), respawnTime);
        }       

        if (hideOnDeplete)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        // Disable collider
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

    }

    /// <summary>
    /// Respawn the resource
    /// </summary>
    protected virtual void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        pendingRespawn = false;
        // Show the object
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Enable collider
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }

        Debug.Log($"{gameObject.name} respawned!");
    }
}
