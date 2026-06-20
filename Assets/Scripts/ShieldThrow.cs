using UnityEngine;

public class ShieldThrow : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private Faction throwerFaction;
    private GameObject throwerObject;
    private float travelled;
    private EquipableItem shieldItem;

    public void Launch(Vector2 dir, float spd, float range, CharacterBase thrower)
    {
        direction = dir.normalized;
        speed = spd;
        maxRange = range;
        throwerFaction = thrower.characterFaction;
        throwerObject = thrower.gameObject;
        shieldItem = GetComponent<EquipableItem>();
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        travelled += step;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.35f);
        foreach (var col in hits)
        {
            if (col.gameObject == gameObject || col.gameObject == throwerObject) continue;

            var cb = col.GetComponent<CharacterBase>();
            if (cb == null) continue;
            if (cb.characterFaction == throwerFaction) continue;

            var health = col.GetComponent<TargetHealth>();
            if (health != null && health.IsDead()) continue;

            HitTarget(cb);
            return;
        }

        if (travelled >= maxRange)
            Land();
    }

    private void HitTarget(CharacterBase target)
    {
        target.ApplyStun(2f);

        if (shieldItem != null && shieldItem.maxDurability > 0)
            shieldItem.TakeDurabilityDamage(Mathf.CeilToInt(shieldItem.maxDurability * 0.5f));

        Land();
    }

    private void Land()
    {
        if (shieldItem != null)
            shieldItem.isEquipped = false;

        Destroy(this);
    }
}
