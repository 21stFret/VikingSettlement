using UnityEngine;

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
    public float wrongToolMultiplier = 0.5f;

    [Header("Respawn Settings")]
    [Tooltip("Should this resource respawn after depletion?")]
    public bool canRespawn = true;

    [Tooltip("Time in seconds before respawning")]
    public float respawnTime = 60f;

    [Header("Visual Feedback")]
    [Tooltip("Particle effect to spawn on harvest")]
    public ParticleSystem harvestEffect;

    [Tooltip("Shake the object when hit")]
    public bool shakeOnHit = true;

    [Tooltip("Shake intensity")]
    public float shakeIntensity = 0.1f;

    // State
    private bool isDepleted = false;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalPosition;
    private float shakeTimer = 0f;
    private bool isRespawning = false;

    public override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.localPosition;
        actualYield = 0f;
    }

    private void Update()
    {
        // Handle shake animation
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            float shake = Mathf.Sin(shakeTimer * 50f) * shakeIntensity * (shakeTimer / 0.2f);
            transform.localPosition = originalPosition + new Vector3(shake, 0, 0);

            if (shakeTimer <= 0)
            {
                transform.localPosition = originalPosition;
            }
        }

        // Schedule respawn if enabled
        if (canRespawn && !isRespawning)
        {
            isRespawning = true;
            Invoke(nameof(Respawn), respawnTime);
        }
    }

    public override void TakeDamage(float damage, EquipableItem weapon, bool trueDamage = false)
    {
        if (isDepleted) return;

        canRespawn = true;
        // Calculate yield based on weapon type
        int yield = CalculateYield(weapon);

        // Add resources to pool
        if (ResourceManager.Instance != null && yield > 0)
        {
            ResourceManager.Instance.AddResource(resourceType, yield);
            Debug.Log($"Harvested {yield} {resourceType} from {gameObject.name}");
        }

        // Visual feedback
        if (harvestEffect != null)
        {
            harvestEffect.Play();
        }

        if (shakeOnHit)
        {
            shakeTimer = 0.2f;
        }

        if (currentHealth > 0)
        {
            currentHealth -= yield;

            if (currentHealth <= 0)
            {
                Deplete();
            }
        }
    }

    /// <summary>
    /// Calculate resource yield based on weapon used
    /// </summary>
    private int CalculateYield(EquipableItem weapon)
    {
        float multiplier = 1f;

        if (weapon != null)
        {
            if (weapon.itemType == preferredTool)
            {
                // Using the right tool
                multiplier = preferredToolMultiplier;
            }
            else if (IsWrongTool(weapon.itemType))
            {
                // Using a bad tool
                multiplier = wrongToolMultiplier;
            }
            // else: neutral tool, use base multiplier
        }
        else
        {
            // No weapon (bare hands) - reduced yield
            multiplier = wrongToolMultiplier;
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
    private bool IsWrongTool(EquipableItem.ItemType toolType)
    {
        // Swords are bad for trees and rocks
        if (resourceType == ResourceType.Wood && toolType == EquipableItem.ItemType.Sword)
            return true;
        if (resourceType == ResourceType.Stone && toolType == EquipableItem.ItemType.Sword)
            return true;

        return false;
    }

    /// <summary>
    /// Called when the resource is fully depleted
    /// </summary>
    private void Deplete()
    {
        isDepleted = true;
        isDead = true;

        Debug.Log($"{gameObject.name} depleted!");

        // Hide the object
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
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
    private void Respawn()
    {
        isDepleted = false;
        isRespawning = false;
        canRespawn = false;
        isDead = false;
        currentHealth = maxHealth;

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

        Debug.Log($"{gameObject.name} respawned!");
    }

    /// <summary>
    /// Check if this resource is depleted
    /// </summary>
    public bool IsDepleted()
    {
        return isDepleted;
    }

    /// <summary>
    /// Force respawn (for debugging)
    /// </summary>
    public void ForceRespawn()
    {
        CancelInvoke(nameof(Respawn));
        Respawn();
    }
}
