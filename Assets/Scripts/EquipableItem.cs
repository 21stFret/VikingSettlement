using System.Collections;
using UnityEngine;

public class EquipableItem : MonoBehaviour
{
    public enum ItemType
    {
        Sword,
        Spear,
        Axe,
        Hammer,
        Shield,
        Armor,
        Accessory,
        Torch
    }

    [Header("Item Info")]
    public ItemType itemType;
    public string itemName;
    public int strength;
    public ItemAttachment.AttachmentPoint attachPoint;
    public bool isEquipped = false;

    [Header("Combat")]
    [Tooltip("Time between attacks in seconds. Lower = faster attacks.")]
    public float attackSpeed = 0.5f;
    public ParticleSystem sheildSparkEffect;
    [Tooltip("Played once when the shield shatters and is destroyed.")]
    public ParticleSystem shatterEffect;

    [Header("Shield Hit Shake")]
    [SerializeField] private float shakeMagnitude = 0.06f;
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private int shakeOscillations = 3;

    [Header("Durability")]
    [Tooltip("Max durability. 0 = no durability tracking (unbreakable).")]
    public int maxDurability = 0;
    [SerializeField] private int currentDurability;

    public Sprite[] itemDamageSprites;
    public SpriteRenderer itemSpriteRenderer;

    public bool IsShield => itemType == ItemType.Shield;
    public bool IsBroken => maxDurability > 0 && currentDurability <= 0;
    public int CurrentDurability => currentDurability;

    public event System.Action OnBroken;

    private void Awake()
    {
        if (maxDurability > 0)
            currentDurability = maxDurability;
        if (itemSpriteRenderer == null)
        {
            itemSpriteRenderer = GetComponent<SpriteRenderer>();
            if (itemSpriteRenderer == null)
            {
                Debug.LogWarning("EquipableItem: No SpriteRenderer found on the item or assigned in the inspector.");
            }
        }
    }

    /// <summary>
    /// Reduce durability by amount. Fires OnBroken when it hits 0.
    /// </summary>
    public void TakeDurabilityDamage(int amount)
    {
        if (maxDurability <= 0 || IsBroken) return;
        currentDurability = Mathf.Max(0, currentDurability - amount);
        if(itemDamageSprites.Length > 0 && itemSpriteRenderer != null)
        {
            int damageLevel = Mathf.FloorToInt(((float)(maxDurability - currentDurability) / maxDurability) * itemDamageSprites.Length);
            damageLevel = Mathf.Clamp(damageLevel, 0, itemDamageSprites.Length - 1);
            itemSpriteRenderer.sprite = itemDamageSprites[damageLevel];
        }
        if (currentDurability <= 0)
        {
            Debug.Log($"{itemName} has shattered!");
            OnBroken?.Invoke();
            return;
        }
        if(IsShield)
        {
            StopCoroutine(nameof(ShakeCoroutine));
            StartCoroutine(nameof(ShakeCoroutine));

            if (sheildSparkEffect == null)
            {
                Debug.LogWarning("Shield spark effect is not assigned!");
                return;
            }
            if(sheildSparkEffect.isPlaying)
            {
                return; // Don't play again if it's already playing
            }
            Vector2 sparkPosition = (Vector2)transform.position + (Random.insideUnitCircle * 0.3f);
            sheildSparkEffect.transform.position = sparkPosition;
            sheildSparkEffect.Play();
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        Vector3 origin = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            // Oscillate back and forth, fading out toward the end
            float offset = Mathf.Sin(progress * shakeOscillations * Mathf.PI * 2f) * shakeMagnitude * (1f - progress);
            transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = origin;
    }

    public void RestoreDurability(int amount)
    {
        if (maxDurability <= 0) return;
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
    }
}
