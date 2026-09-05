using UnityEngine;

public enum WheatStage
{
    Cut,
    Sprout,
    Growing,
    Harvestable
}

[RequireComponent(typeof(Collider2D))]
public class HarvestableWheat : TargetHealth
{
    [Header("Growth Sprites (random variant picked on Start)")]
    public Sprite[] cutSprites;
    public Sprite[] sproutSprites;
    public Sprite[] growingSprites;
    public Sprite[] harvestableSprites;

    [Header("Growth Settings")]
    [Tooltip("Full grow cycle in in-game days (Cut to Harvestable)")]
    public float growCycleDays = 1f;

    [Header("Harvest Settings")]
    public ResourceType resourceType = ResourceType.Wheat;
    public int yieldAmount = 2;

    [Header("Visual Feedback")]
    public ParticleSystem cutEffect;
    public bool shakeOnHit = true;
    public float shakeIntensity = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private WheatStage currentStage = WheatStage.Harvestable;
    private float growthTimer = 0f;
    private float stageInterval; // 1/3 of a day per stage
    private int variantIndex = 0;

    public WheatStage CurrentStage => currentStage;

    private bool IsWinter()
    {
        return SeasonManager.Instance != null &&
               SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
    }

    public override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // Pick a random variant that all stages will use
        int maxVariants = Mathf.Max(1, cutSprites.Length, sproutSprites.Length, growingSprites.Length, harvestableSprites.Length);
        variantIndex = Random.Range(0, maxVariants);

        UpdateStageInterval();
        SetStage(WheatStage.Harvestable);

        if (GameTickManager.Instance != null)
            GameTickManager.Instance.OnGameTick += UpdateFromTickManager;
    }

    private void UpdateFromTickManager(float deltaTime)
    {
        // Grow through stages (not in winter)
        if (currentStage != WheatStage.Harvestable && !IsWinter())
        {
            growthTimer += deltaTime;
            if (growthTimer >= stageInterval)
            {
                growthTimer -= stageInterval;
                AdvanceStage();
            }
        }
    }

    private void UpdateStageInterval()
    {
        // 3 transitions (Cut->Sprout->Growing->Harvestable) over growCycleDays
        float dayLength = DayNightManager.Instance != null ? DayNightManager.Instance.dayLengthInSeconds : 120f;
        stageInterval = (dayLength * growCycleDays) / 3f;
    }

    private void AdvanceStage()
    {
        switch (currentStage)
        {
            case WheatStage.Cut:
                SetStage(WheatStage.Sprout);
                break;
            case WheatStage.Sprout:
                SetStage(WheatStage.Growing);
                break;
            case WheatStage.Growing:
                SetStage(WheatStage.Harvestable);
                break;
        }
    }

    private void SetStage(WheatStage stage)
    {
        currentStage = stage;

        Sprite[] sprites = stage switch
        {
            WheatStage.Cut => cutSprites,
            WheatStage.Sprout => sproutSprites,
            WheatStage.Growing => growingSprites,
            WheatStage.Harvestable => harvestableSprites,
            _ => harvestableSprites
        };

        if (spriteRenderer != null && sprites != null && sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[variantIndex % sprites.Length];
        }

        // Only harvestable wheat can be hit
        bool canBeHit = (stage == WheatStage.Harvestable);
        if (col != null) col.enabled = canBeHit;
        isDead = !canBeHit;
    }

    public override void TakeDamage(float damage, EquipableItem weapon = null, bool trueDamage = false, Vector2 attackerPos = default)
    {
        if (currentStage != WheatStage.Harvestable) return;

        base.TakeDamage(damage, weapon, trueDamage, attackerPos);
    }

    public override void Die()
    {
        if (currentStage != WheatStage.Harvestable) return;

        // Yield resources
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(resourceType, yieldAmount);
            Debug.Log($"Harvested {yieldAmount} {resourceType} from {gameObject.name}");
        }

        if (cutEffect != null)
        {
            cutEffect.Play();
        }

        // Reset to cut stage and start regrowing
        currentHealth = maxHealth;
        isDead = false;
        growthTimer = 0f;
        UpdateStageInterval();
        SetStage(WheatStage.Cut);
    }

    private void OnDestroy()
    {
        if (GameTickManager.Instance != null)
            GameTickManager.Instance.OnGameTick -= UpdateFromTickManager;
    }
}
