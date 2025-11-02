using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HittableObject : TargetHealth
{
    public void Hit(float damage)
    {
        TakeDamage(damage);
    }
}
