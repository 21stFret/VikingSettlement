using System.Collections;
using UnityEngine;

/// <summary>
/// Add to a Raider enemy prefab alongside EnemyAI and EnemyController.
///
/// Behaviour: when the Raider's shield breaks it pauses briefly then equips a
/// fresh one from WeaponDatabase, making it a persistent shielded threat.
///
/// All combat logic (blocking, chasing, attacking) remains in EnemyAI/EnemyController.
/// This component only handles the shield-replacement loop.
/// </summary>
[RequireComponent(typeof(ItemAttachment))]
[RequireComponent(typeof(CharacterBase))]
public class RaiderBehaviour : MonoBehaviour
{
    [Tooltip("Seconds between shield breaking and the raider grabbing a replacement.")]
    [SerializeField] private float shieldReplaceDelay = 1.2f;

    private ItemAttachment _itemAttachment;
    private CharacterBase  _characterBase;

    private void Awake()
    {
        _itemAttachment = GetComponent<ItemAttachment>();
        _characterBase  = GetComponent<CharacterBase>();
    }

    private IEnumerator Start()
    {
        // Yield one frame so Enemy.Start() has run and the initial shield is equipped.
        yield return null;
        SubscribeToShield();
    }

    // ── Shield subscription ───────────────────────────────────────────────────────

    private void SubscribeToShield()
    {
        if (_characterBase.shield != null)
            _characterBase.shield.OnBroken += OnShieldBroken;
    }

    private void OnShieldBroken()
    {
        StartCoroutine(ReplaceShield());
    }

    // ── Replacement coroutine ─────────────────────────────────────────────────────

    private IEnumerator ReplaceShield()
    {
        yield return new WaitForSeconds(shieldReplaceDelay);

        if (!gameObject.activeInHierarchy) yield break;

        var enemy = GetComponent<Enemy>();
        if (enemy != null && enemy.IsDead()) yield break;

        // Look for a dropped (unequipped) shield nearby
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        float closestDist  = Mathf.Infinity;
        GameObject closest = null;

        foreach (var col in hits)
        {
            if (!col.CompareTag("Shield")) continue;
            var item = col.GetComponent<EquipableItem>();
            if (item == null || item.isEquipped) continue;
            float d = Vector2.Distance(transform.position, col.transform.position);
            if (d < closestDist) { closestDist = d; closest = col.gameObject; }
        }

        if (closest != null)
        {
            var ec = GetComponent<EnemyController>();
            while (closest != null
                && Vector2.Distance(transform.position, closest.transform.position) > 0.1f)
            {
                ec.MoveTo(closest.transform.position);
                yield return null;  // yield each frame so the raider actually moves
            }

            if (closest != null)
                _itemAttachment.EquipShield(closest);
        }
        else
        {
            // No dropped shield in range — grab a fresh one from the database
            if (WeaponDatabase.Instance != null && WeaponDatabase.Instance.GetRandomShield() != null)
                _itemAttachment.GiveRandomShield();
        }

        SubscribeToShield();
        Debug.Log($"[Raider] {gameObject.name} grabbed a replacement shield.");
    }
}
