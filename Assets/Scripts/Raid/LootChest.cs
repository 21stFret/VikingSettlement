using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A chest or container that drops loot when destroyed.
/// Set maxHealth to 1 for instant open, or higher for tougher chests.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LootChest : TargetHealth
{
    [Header("Chest Settings")]
    public string chestName = "Treasure Chest";

    [Header("Loot Table")]
    [Tooltip("Guaranteed loot (always drops)")]
    public List<ChestLoot> guaranteedLoot = new List<ChestLoot>();

    [Tooltip("Random loot (chance-based)")]
    public List<ChestLoot> randomLoot = new List<ChestLoot>();

    [Tooltip("How many random items to roll for")]
    public int randomRolls = 2;

    [Header("Visuals")]
    public Sprite openSprite;
    public ParticleSystem openEffect;

    [Header("Audio")]
    public AudioClip openSound;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    public override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    public override void Die()
    {
        if (isDead) return;
        base.Die();

        Debug.Log($"{chestName} opened!");

        // Change sprite
        if (openSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        // Effects
        if (openEffect != null)
        {
            openEffect.Play();
        }

        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        // Drop guaranteed loot
        foreach (var loot in guaranteedLoot)
        {
            DropLoot(loot);
        }

        // Roll for random loot
        for (int i = 0; i < randomRolls; i++)
        {
            foreach (var loot in randomLoot)
            {
                if (Random.value <= loot.dropChance)
                {
                    DropLoot(loot);
                }
            }
        }

        // Disable collider
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void DropLoot(ChestLoot loot)
    {
        float amount = Random.Range(loot.minAmount, loot.maxAmount + 1);

        // Add to raid loot if in raid
        if (RaidSceneController.Instance != null)
        {
            RaidSceneController.Instance.AddLoot(loot.resourceType, amount);
            Debug.Log($"Chest: +{amount} {loot.resourceType}");
        }
        // Otherwise add directly to resources
        else if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(loot.resourceType, amount);
            Debug.Log($"Chest: +{amount} {loot.resourceType}");
        }
    }
}

[System.Serializable]
public class ChestLoot
{
    public ResourceType resourceType = ResourceType.Iron;

    [Range(0f, 1f)]
    public float dropChance = 1f;

    public int minAmount = 1;
    public int maxAmount = 5;
}
