using UnityEngine;

public class CuttableGrass : HittableObject
{
    [Header("Cuttable Grass Settings")]
    private SpriteRenderer _spriteRenderer;
    public Sprite cutGrassSprite, longGrassSprite;
    private bool isCut = false;
    private float growBackTimer = 5f;
    public ParticleSystem cutEffect;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void Die()
    {
        if (isDead) return;
        base.Die();
        CutGrass();
    }

    private void CutGrass()
    {
        if (_spriteRenderer != null && cutGrassSprite != null)
        {
            _spriteRenderer.sprite = cutGrassSprite;
        }
        isCut = true;

        if (cutEffect != null)
        {
            cutEffect.Play();
        }
    }

    private void Update()
    {
        if (isCut)
        {
            growBackTimer -= Time.deltaTime;
            if (growBackTimer <= 0f)
            {
                GrowBack();
            }
        }
    }
    
    private void GrowBack()
    {
        if (_spriteRenderer != null && longGrassSprite != null)
        {
            _spriteRenderer.sprite = longGrassSprite;
        }
        isCut = false;
        growBackTimer = 5f; // Reset timer
        isDead = false; // Reset health state
        currentHealth = maxHealth; // Reset health
    }
}
