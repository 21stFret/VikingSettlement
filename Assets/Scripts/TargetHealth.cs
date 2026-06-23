using UnityEngine;
using System;

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
    public virtual void TakeDamage(float damage, EquipableItem weapon = null, bool trueDamage = false)
    {
        if (isDead) return;

        // Check if the target is invincible
        if (Time.time - lastDamageTime < invincibilityDuration) return;

        lastDamageTime = Time.time;

        // Calculate final damage after reductions (unless trueDamage)
        float finalDamage = trueDamage ? damage : CalculateFinalDamage(damage, weapon);
        finalDamage = Mathf.Max(0f, finalDamage); // Never negative

        currentHealth -= finalDamage;

        lastDamageWasCombat = (weapon != null);

        // Combat hits (weapon != null) can trigger wound rolls on significant damage
        if (weapon != null && finalDamage > 0f)
        {
            OnSignificantHPDamage(finalDamage);
            OnDamageTaken(finalDamage, weapon);
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
    protected virtual float CalculateFinalDamage(float rawDamage, EquipableItem weapon)
    {
        return rawDamage;
    }

    /// <summary>
    /// Called after damage is applied. Override for visual feedback, UI updates, etc.
    /// </summary>
    protected virtual void OnDamageTaken(float finalDamage, EquipableItem weapon)
    {
        // Base implementation does nothing - override in subclasses
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
