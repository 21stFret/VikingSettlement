using UnityEngine;

/// <summary>
/// Drop onto any enemy prefab in a test scene to replace all AI with
/// fully manual inspector-controlled states.
///
/// - Toggle Blocking / Parrying in the Inspector at runtime — states hold
///   until you change them.
/// - Enable Auto Attack to have the dummy swing at the nearest Villager
///   in range; disable to make it stand still.
/// - The Rigidbody is position-frozen so the dummy never drifts, but
///   SetMovement is still called internally so the facing direction and
///   attack hitbox stay correct.
///
/// Any component whose class name ends with "AI" is auto-disabled on Awake.
/// </summary>
[RequireComponent(typeof(CharacterBase))]
public class CombatDummy : MonoBehaviour
{
    [Header("Manual State — toggle at runtime in the Inspector")]
    public bool blocking = false;
    public bool parrying = false;
    public bool shooting = false;

    [Header("Auto Attack")]
    [Tooltip("When true the dummy attacks the nearest Villager in range each frame it can.")]
    public bool autoAttack = false;
    [SerializeField] private float autoAttackRange = 3f;

    public CharacterBase target;

    private CharacterBase _cc;

    private bool init;

    private void Start()
    {
        init = false;
        Invoke("Init", 0.2f);
    }

    private void Init()
    {
        _cc = GetComponent<CharacterBase>();
        // Freeze position so SetMovement (used only for facing/hitbox direction)
        // does not actually slide the dummy around the scene
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

        // Disable any AI components (VillagerAI, EnemyAI, etc.)
        foreach (var behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour == this) continue;
            if (behaviour.GetType().Name.EndsWith("AI"))
                behaviour.enabled = false;
        }

        if (_cc.itemAttachment.weapon != null)
        {
            _cc.itemAttachment.EquipWeapon(_cc.itemAttachment.weapon);
        }


        _cc.CurrentTarget = FindNearestVillager()?.GetComponent<CharacterBase>();

        init = true;
    }

    private void Update()
    {
        if (!init) return;
        // Push manual states onto the controller every frame
        _cc.isBlocking = blocking;
        _cc.isParrying = parrying;

        if (autoAttack)
        { 
            if (_cc.CurrentTarget != null)
            {
                // SetMovement updates cachedMoveX so the attack hitbox
                // fires in the correct direction; position is frozen so no drift
                /*
                Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;
                _cc.SetMovement(dir);
                */

                if (_cc.CanAttack())
                    _cc.Attack();

                return;
            }
            else
            {
                _cc.CurrentTarget = FindNearestVillager()?.GetComponent<CharacterBase>();
            }
        }
        // Not attacking — stay still
        _cc.Stop();
    }

    private Villager FindNearestVillager()
    {
        float bestSqDist = autoAttackRange * autoAttackRange;
        Villager nearest = null;

        foreach (var v in FindObjectsByType<Villager>())
        {
            if (v.IsDead()) continue;
            float sqDist = ((Vector2)(v.transform.position - transform.position)).sqrMagnitude;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                nearest = v;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, autoAttackRange);
    }
}
