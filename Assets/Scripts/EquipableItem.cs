using DG.Tweening;
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
    public string itemID;
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
    public bool IsWeapon => itemType == ItemType.Sword || itemType == ItemType.Spear || itemType == ItemType.Axe || itemType == ItemType.Hammer;

    public bool IsBroken => maxDurability > 0 && currentDurability <= 0;
    public int CurrentDurability => currentDurability;

    public event System.Action OnBroken;
    public event System.Action OnDurabilityChanged;
    public event System.Action OnUnequipped;

    public void NotifyUnequipped() => OnUnequipped?.Invoke();

    public void Init(bool fromLoad = false)
    {

        if (maxDurability > 0 && !fromLoad)
            currentDurability = maxDurability;
        if(!fromLoad)
            itemID = System.Guid.NewGuid().ToString();
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
        OnDurabilityChanged?.Invoke();
        var villager = GetComponentInParent<Villager>();
        if (villager != null)
        {
            villager.skills.ImproveSkill(JobType.Warrior);
            var cc = GetComponentInParent<CharacterBase>();
            bool bonusXP = (cc != null && cc.isParrying) ||
                           (RaidManager.Instance != null && RaidManager.Instance.IsOnRaid);
            if (bonusXP) villager.skills.ImproveSkill(JobType.Warrior);
        }

        if(IsShield)
        {
            StopCoroutine(nameof(ShakeCoroutine));
            StartCoroutine(nameof(ShakeCoroutine));
            if (villager.isJarl) { Camera.main.DOShakePosition(0.1f, 0.1f, 10, 90, false); }

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

        if (currentDurability <= 0)
        {
            Debug.Log($"{itemName} has shattered!");
            OnBroken?.Invoke();
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

    public void SetDurability(float amount)
    {
        currentDurability = (int)amount;
        if (itemDamageSprites.Length > 0 && itemSpriteRenderer != null)
        {
            int damageLevel = Mathf.FloorToInt(((float)(maxDurability - currentDurability) / maxDurability) * itemDamageSprites.Length);
            damageLevel = Mathf.Clamp(damageLevel, 0, itemDamageSprites.Length - 1);
            itemSpriteRenderer.sprite = itemDamageSprites[damageLevel];
        }
    }
}
