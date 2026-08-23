using UnityEngine;
using System;
using DG.Tweening;

public class TargetHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    protected bool isDead = false;
    protected bool lastDamageWasCombat = false;
    private float invincibilityDuration = 0.1f;
    private float lastDamageTime = -Mathf.Infinity;

    // Events
    public event Action OnDeath;

    public virtual void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    /// <summary>
    /// Main entry point for taking damage. Handles invincibility, damage reduction, and death.
    /// </summary>
    /// <param name="damage">Raw incoming damage</param>
    /// <param name="weapon">Weapon used (can be null)</param>
    /// <param name="trueDamage">If true, bypasses all damage reduction</param>
    public virtual void TakeDamage(float damage, EquipableItem weapon = null, bool trueDamage = false, Vector2 attackerPos = default)
    {
        if (isDead) return;

        // Check if the target is invincible
        if (Time.time - lastDamageTime < invincibilityDuration) return;

        lastDamageTime = Time.time;

        // Calculate final damage after reductions (unless trueDamage)
        float finalDamage = trueDamage ? damage : CalculateFinalDamage(damage, weapon, attackerPos);
        finalDamage = Mathf.Max(0f, finalDamage); // Never negative

        currentHealth -= finalDamage;

        lastDamageWasCombat = (weapon != null);

        // Combat hits (weapon != null) can trigger wound rolls on significant damage
        if (finalDamage > 0f)
        {
            OnSignificantHPDamage(finalDamage);
            OnDamageTaken(finalDamage, weapon);
        }
        if (gameObject.layer != 9 && weapon != null)
        {
            weapon.TakeDurabilityDamage(1);
        }

        Debug.Log($"{gameObject.name} took {finalDamage} damage (raw: {damage}, trueDamage: {trueDamage})");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Calculate final damage after applying defense, shields, etc.
    /// Override in subclasses to add damage reduction.
    /// </summary>
    protected virtual float CalculateFinalDamage(float rawDamage, EquipableItem weapon, Vector2 attackerPos)
    {
        return rawDamage;
    }

    /// <summary>
    /// Called after damage is applied. Override for visual feedback, UI updates, etc.
    /// </summary>
    protected virtual void OnDamageTaken(float finalDamage, EquipableItem weapon)
    {
        // Base implementation does nothing - override in subclasses.
        // Deliberately NOT calling HitFeedback.Instance?.OnHit() here: this base class is also
        // the parent of BreakableBarrel/CuttableGrass/HarvestableResource/HarvestableWheat/
        // LootChest, none of which override this method, so a call here would fire camera
        // shake/rumble/hit-stop on every chop/harvest swing too, not just combat. Wired into
        // Villager.OnDamageTaken and Enemy.OnDamageTaken instead, which covers all actual
        // character-vs-character combat. Revisit if "juice" on destructibles is wanted too.
    }

    protected virtual void OnBlocked(EquipableItem weapon)
    {
        if (weapon == null) return;
        // Base implementation does nothing - override in subclasses
        if(weapon.itemType == EquipableItem.ItemType.Bow)
        {
            Arrow arrow = weapon.GetComponent<Arrow>();
            arrow.ArrowStuck(GetComponent<CharacterBase>().shield.transform);
            arrow.transform.localPosition = new Vector3(UnityEngine.Random.Range(0.25f, 0.9f), UnityEngine.Random.Range(-0.3f, 0.3f), 0);
        }
    }

    /// <summary>
    /// Called when a weapon-based hit deals damage. Override to roll for wounds.
    /// </summary>
    protected virtual void OnSignificantHPDamage(float hpDamage) { }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Release all attacker slots so orbiters can re-engage a new target
        GetComponent<CharacterBase>()?.ReleaseAllSlots();

        // Release the slot this character itself was holding on its own target — its own
        // Update loop stops running once dead, so without this the claim would otherwise only
        // clear via OnDestroy, which can be long-delayed (corpse/removal effects) or never fire.
        GetComponent<CharacterAI>()?.ReleaseEngagementSlot();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.simulated = false;
        }

        // Fire death event
        OnDeath?.Invoke();
    }

    public bool IsDead()
    {
        return isDead;
    }

    /// <summary>
    /// Heal the target by the specified amount.
    /// </summary>
    public virtual void Heal(float amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
