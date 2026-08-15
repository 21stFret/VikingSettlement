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
        Torch, 
        Bow
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
    private SpriteSorting spriteSorting;
    public int rightSortingOffset;
    public int leftSortingOffset;

    public bool IsShield => itemType == ItemType.Shield;
    public bool IsWeapon => itemType == ItemType.Sword || itemType == ItemType.Spear || itemType == ItemType.Axe || itemType == ItemType.Hammer || itemType == ItemType.Bow;

    public bool IsBroken => maxDurability > 0 && currentDurability <= 0;
    public int CurrentDurability => currentDurability;

    public event System.Action OnBroken;
    public event System.Action OnDurabilityChanged;
    public event System.Action OnUnequipped;

    public void NotifyUnequipped() => OnUnequipped?.Invoke();

    public Sprite shieldBack;
    private Sprite shieldFront;

    private bool initilazied;

    private void Start()
    {
        Init();
    }

    public void Init(bool fromLoad = false)
    {
        if (initilazied) return;
        initilazied = true;
        if (maxDurability > 0 && !fromLoad)
            currentDurability = maxDurability;
        if(!fromLoad)
            itemID = System.Guid.NewGuid().ToString();
        if (itemSpriteRenderer == null)
        {
            itemSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (itemSpriteRenderer == null)
            {
                Debug.LogWarning("EquipableItem: No SpriteRenderer found on the item or assigned in the inspector.");
                return;
            }
        }
        shieldFront = itemSpriteRenderer.sprite;
        spriteSorting = itemSpriteRenderer.gameObject.GetComponent<SpriteSorting>();
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
            shieldFront = itemSpriteRenderer.sprite;
        }
        OnDurabilityChanged?.Invoke();
        var villager = GetComponentInParent<Villager>();
        if (villager != null)
        {
            villager.skills.ImproveJob(JobType.Warrior);
            var cc = GetComponentInParent<CharacterBase>();
            bool bonusXP = (cc != null && cc.isParrying) ||
                           (RaidManager.Instance != null && RaidManager.Instance.IsOnRaid);
            if (bonusXP) villager.skills.ImproveJob(JobType.Warrior);
        }

        if (IsShield)
        {
            StopCoroutine(nameof(ShakeCoroutine));
            StartCoroutine(nameof(ShakeCoroutine));
            if (villager != null)
            { 
                if (villager.isJarl) { Camera.main.DOShakePosition(0.1f, 0.1f, 10, 90, false); }
            }

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
        AttackCooldownUI.Instance?.OnWeaponDurability();
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

    public void OnCharacterFacingChange(FacingDirection dir)
    {
        if(IsShield)
        {
            switch (dir)
            {
                case FacingDirection.East: itemSpriteRenderer.sprite = shieldBack; spriteSorting.offset = rightSortingOffset; break;
                case FacingDirection.West: itemSpriteRenderer.sprite = shieldFront; spriteSorting.offset =  leftSortingOffset; break;
                case FacingDirection.North: itemSpriteRenderer.sprite = shieldBack; spriteSorting.offset = rightSortingOffset; break;
                case FacingDirection.South: itemSpriteRenderer.sprite = shieldFront; spriteSorting.offset = leftSortingOffset; break;

            }
        }
        else
        {
            switch (dir)
            {
                case FacingDirection.East: spriteSorting.offset = leftSortingOffset; break;
                case FacingDirection.West: spriteSorting.offset = rightSortingOffset; break;
                case FacingDirection.North: spriteSorting.offset = leftSortingOffset; break;
                case FacingDirection.South: spriteSorting.offset = rightSortingOffset; break;

            }
        }

    }
}
