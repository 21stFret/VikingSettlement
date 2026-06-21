using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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

        // Find closest Shield
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        float closestDistance = Mathf.Infinity;
        GameObject _closestShield = null;
        foreach (var shield in hits)
        {
            if (!shield.CompareTag("Shield")) continue;
            if (shield.GetComponent<EquipableItem>().isEquipped)
            {
                // Already equipped by someone else
                continue;
            }
            float distance = Vector2.Distance(transform.position, shield.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                _closestShield = shield.gameObject;
            }
        }
        if (_closestShield != null)
        {
            // Equip the closest shield
            EnemyController ec = GetComponent<EnemyController>();
            while(Vector2.Distance(transform.position, _closestShield.transform.position) > 0.09f)
            {
                ec.MoveTo(_closestShield.transform.position);
                if (Vector2.Distance(transform.position, _closestShield.transform.position) < 0.1f)
                {

                    ec.itemAttachment.EquipShield(_closestShield);
                }
            }


        }

        // Re-subscribe to the new shield so the loop continues indefinitely.
        SubscribeToShield();

        Debug.Log($"[Raider] {gameObject.name} grabbed a replacement shield.");
    }
}
