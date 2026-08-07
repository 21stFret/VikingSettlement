using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public LayerMask targetLayer;
    public float inFlightlifetime = 5f;
    public float lifetime = 10f;
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;
    private bool inFlight;
    private Vector2 direction;
    private EquipableItem arrowRef;
    private bool stuck;
    private Vector2 archerPos;
    public GameObject stuckMask;
    private GameObject owner;

    private void Start()
    {
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        arrowRef = gameObject.AddComponent<EquipableItem>();
        arrowRef.itemType = EquipableItem.ItemType.Bow;
        Fire();
        stuckMask.SetActive(false);
    }

    public void Initialize(float speed, float damage, Vector2 dir, Vector2 archer, GameObject own)
    {
        this.speed = speed;
        this.direction = dir;
        this.damage = damage;
        this.archerPos = archer;
        this.owner = own;
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

        ContactPoint2D contact = collision.contacts[0];
        Vector2 hitPoint = contact.point;
        Vector2 hitNormal = contact.normal;

        // Position/rotate the arrow to match the stick point + angle
        transform.position = hitPoint;
        transform.right = -hitNormal; // orient along the surface normal, adjust sign to taste

        // Deal damage if the thing we hit can take it
        var damageable = collision.gameObject.GetComponent<TargetHealth>();
        damageable?.TakeDamage(damage, arrowRef,attackerPos: archerPos);
        inFlight = false;
        ArrowStuck(collision.transform);
    }

    public void ArrowStuck(Transform _transform = null)
    {
        if (stuck) return;
        stuck = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        if( _transform != null )
        {
            transform.SetParent(_transform);
        }
        stuckMask.SetActive(true);
    }
}