using System.Collections.Generic;
using UnityEngine;

public class LootDropManager : MonoBehaviour
{
    public static LootDropManager Instance { get; private set; }

    [Header("Prefab")]
    public GameObject lootDropPrefab;

    [Header("Sprite Mapping")]
    public LootSpriteEntry[] spriteMapping;
    public Sprite defaultSprite;

    [Header("Drop Settings")]
    public float popForce = 3f;
    public float dropLifetime = 60f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.05f;
    public float settleDelay = 0.5f;

    [Header("Pool Settings")]
    public int initialPoolSize = 20;

    private Dictionary<ResourceType, Sprite> spriteLookup;
    private Queue<LootDrop> pool = new Queue<LootDrop>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildSpriteLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PrewarmPool();
    }

    private void BuildSpriteLookup()
    {
        spriteLookup = new Dictionary<ResourceType, Sprite>();
        if (spriteMapping == null) return;

        foreach (var entry in spriteMapping)
        {
            if (entry.sprite != null)
            {
                spriteLookup[entry.resourceType] = entry.sprite;
            }
        }
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            LootDrop drop = CreateNewDrop();
            pool.Enqueue(drop);
        }
    }

    private LootDrop CreateNewDrop()
    {
        GameObject go = Instantiate(lootDropPrefab, Vector3.zero, Quaternion.identity, transform);
        LootDrop drop = go.GetComponent<LootDrop>();
        go.SetActive(false);
        return drop;
    }

    private LootDrop GetFromPool()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        return CreateNewDrop();
    }

    public void ReturnToPool(LootDrop drop)
    {
        pool.Enqueue(drop);
    }

    public void SpawnDrops(Vector3 position, List<LootTableEntry> lootTable)
    {
        if (lootTable == null || lootDropPrefab == null) return;

        foreach (var entry in lootTable)
        {
            if (Random.value <= entry.dropChance)
            {
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                if (amount > 0)
                {
                    SpawnDrop(position, entry.resourceType, amount);
                }
            }
        }
    }

    public void SpawnDrop(Vector3 position, ResourceType type, int amount)
    {
        if (lootDropPrefab == null) return;

        LootDrop drop = GetFromPool();

        // Resolve sprite
        Sprite sprite = defaultSprite;
        if (spriteLookup != null && spriteLookup.TryGetValue(type, out Sprite mapped))
        {
            sprite = mapped;
        }

        drop.Activate(position, type, amount, sprite, dropLifetime, bobSpeed, bobHeight, settleDelay);

        // Apply random pop force
        Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
            rb.AddForce(dir * popForce, ForceMode2D.Impulse);
        }
    }

    public Sprite GetSpriteForType(ResourceType type)
    {
        if (spriteLookup != null && spriteLookup.TryGetValue(type, out Sprite sprite))
        {
            return sprite;
        }
        return defaultSprite;
    }
}

[System.Serializable]
public class LootSpriteEntry
{
    public ResourceType resourceType;
    public Sprite sprite;
}
