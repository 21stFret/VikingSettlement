using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public LayerMask targetLayer;
    public float inFlightlifetime = 5f;
    public float lifetime = 10f;
    public float noCollisionTimer = 0.1f;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;
    private bool inFlight;
    private Vector2 direction;
    private EquipableItem arrowRef;
    private bool stuck;
    private Vector2 archerPos;
    public SpriteMask stuckMask;
    private GameObject owner;
    public Vector2 rbVelocity => rb.linearVelocity;

    public void Initialize(float damage, Vector2 dir, Vector2 archer, GameObject own)
    {
        this.direction = dir;
        this.damage = damage;
        this.archerPos = archer;
        this.owner = own;

        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        arrowRef = gameObject.AddComponent<EquipableItem>();
        arrowRef.itemType = EquipableItem.ItemType.Bow;
        Fire();
        stuckMask = GetComponentInChildren<SpriteMask>();
        stuckMask.enabled = false;

        col.enabled = false;
    }

    public void Fire()
    {
        rb.linearVelocity = direction.normalized * speed;
        inFlight = true;
        stuck = false;
    }

    private void FixedUpdate()
    {
        lifetime -= Time.fixedDeltaTime;
        if(lifetime <= 0f)
        {
            Destroy(gameObject);
        }

        if (inFlightlifetime <= 0f || !inFlight)
            return;

        noCollisionTimer -= Time.fixedDeltaTime;
        if (noCollisionTimer <= 0)
        {
            if (col.enabled == false)
                col.enabled = true;
        }

        inFlightlifetime -= Time.fixedDeltaTime;
        if (inFlightlifetime <= 0f)
            animator.SetTrigger("Dip");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == owner) return;

        int hitLayer = collision.gameObject.layer;

        // Ignore anything not in our target mask
        if ((targetLayer.value & (1 << hitLayer)) == 0)
            return;

        if (collision.contacts.Length == 0)
            return;

        // Deal damage if the thing we hit can take it
        var damageable = collision.gameObject.GetComponent<TargetHealth>();
        damageable?.TakeDamage(damage, arrowRef,attackerPos: archerPos);
        inFlight = false;
        ArrowStuck(collision.transform, collision.contacts[0].point);
    }

    public void ArrowStuck(Transform _transform = null, Vector2 Hitpoint = default)
    {
        if (stuck) return;
        stuck = true;
        stuckMask.enabled = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        transform.position = (Vector2)transform.position + direction.normalized * 0.6f;
        if ( _transform != null )
        {
            transform.SetParent(_transform);
        }

    }
}