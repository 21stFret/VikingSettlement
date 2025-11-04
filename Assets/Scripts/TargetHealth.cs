using UnityEngine;

public class TargetHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    protected bool isDead = false;
    private float invincibilityDuration = 0.1f;
    private float lastDamageTime = -Mathf.Infinity;

    public virtual void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public virtual void TakeDamage(float damage, bool trueDamage = false)
    {
        if (isDead) return;

        // Check if the target is invincible
        if (Time.time - lastDamageTime < invincibilityDuration) return;

        lastDamageTime = Time.time;
        currentHealth -= damage;

        Debug.Log($"{gameObject.name} took {damage} damage!");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        if (isDead) return;
        // Logic for when the target dies
        //Destroy(gameObject);
        isDead = true;
    }

    public bool IsDead()
    {
        return isDead;
    }
}
