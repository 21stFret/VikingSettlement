using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class LootDrop : MonoBehaviour
{
    public ResourceType resourceType;
    public int amount = 1;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private bool settled = false;
    private float settleTimer;
    private float lifetimeTimer;
    private float bobBaseY;
    private float bobTime;

    // Config set by LootDropManager on spawn
    [HideInInspector] public float settleDelay = 0.5f;
    [HideInInspector] public float lifetime = 60f;
    [HideInInspector] public float bobSpeed = 2f;
    [HideInInspector] public float bobHeight = 0.05f;

    private GameObject _particleSystem;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
    }

    /// <summary>
    /// Resets all state so this pooled object can be reused as a fresh drop.
    /// Called by LootDropManager when pulling from the pool.
    /// </summary>
    public void Activate(Vector3 position, ResourceType type, int amt, Sprite sprite,
                         float life, float bob, float bobH, float settle)
    {
        resourceType = type;
        amount = amt;
        lifetime = life;
        bobSpeed = bob;
        bobHeight = bobH;
        settleDelay = settle;

        // Reset state
        settled = false;
        settleTimer = settleDelay;
        lifetimeTimer = lifetime;
        bobTime = 0f;
        bobBaseY = 0f;

        // Position
        transform.position = position;

        // Rigidbody
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Collider
        col.enabled = true;

        // Sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sprite != null)
        {
            sr.sprite = sprite;
        }

        _particleSystem = transform.GetChild(0).gameObject;
        
        // Particle system
        if (_particleSystem != null)
        {
            _particleSystem.transform.parent = null;
            _particleSystem.transform.position = position;
            _particleSystem.SetActive(true);
        }

        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        if (_particleSystem != null)
        {
            _particleSystem.SetActive(false);
        }

        gameObject.SetActive(false);
        LootDropManager.Instance?.ReturnToPool(this);
    }

    private void Update()
    {
        // Auto-return after lifetime
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Deactivate();
            return;
        }

        if (!settled)
        {
            settleTimer -= Time.deltaTime;
            if (settleTimer <= 0f)
            {
                settled = true;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
                bobBaseY = transform.position.y;
            }
        }
        else
        {
            // Bob animation
            bobTime += Time.deltaTime * bobSpeed;
            Vector3 pos = transform.position;
            pos.y = bobBaseY + Mathf.Sin(bobTime) * bobHeight;
            transform.position = pos;
        }
        _particleSystem.transform.position = transform.position; // Keep particle system at the same position as the loot drop
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != 6) return; // Player layer

        // In a raid, defer to trip loot (applied on return to settlement) instead of
        // granting resources mid-raid — keeps enemy "pocket" drops consistent with chest loot.
        if (RaidSceneController.Instance != null)
        {
            RaidSceneController.Instance.AddLoot(resourceType, amount);
        }
        else if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(resourceType, amount);
        }

        Deactivate();
    }
}
