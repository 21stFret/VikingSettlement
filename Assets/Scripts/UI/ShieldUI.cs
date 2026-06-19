using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mirrors the player's equipped shield in the HUD.
/// Swaps damage sprites and shakes using the same logic as the in-world EquipableItem.
///
/// Quick setup:
///   1. Add a UI Image on a Canvas.
///   2. Attach this component to the Image's GameObject (or any active GameObject).
///   3. Assign the Image in the Inspector. Optionally assign a CanvasGroup and a noShieldSprite.
/// </summary>
public class ShieldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image shieldImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Sprite shown when no shield is equipped. Leave empty to hide the UI instead.")]
    [SerializeField] private Sprite noShieldSprite;

    [Header("Shake")]
    [Tooltip("Horizontal shake distance in UI pixels.")]
    [SerializeField] private float shakeMagnitude = 10f;
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private int shakeOscillations = 3;

    private CharacterBase _trackedCC;
    private EquipableItem _trackedShield;
    private RectTransform _rect;
    private Vector2 _originPos;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originPos = _rect.anchoredPosition;
    }

    public void Init()
    {
        if (PlayerController.Instance != null)
        {
            _trackedCC = PlayerController.Instance.GetController();
        }

        var shield = _trackedCC != null ? _trackedCC.shield : null;

        if(shield!=null)
        {
            UnsubscribeFromShield();
        }

        _trackedShield = shield;
        SubscribeToShield();
        UpdateSprite();
    }

    private void SubscribeToShield()
    {
        if (_trackedShield == null) return;
        _trackedShield.OnDurabilityChanged += OnShieldDamaged;
        _trackedShield.OnBroken += OnShieldBroken;
    }

    private void UnsubscribeFromShield()
    {
        if (_trackedShield == null) return;
        _trackedShield.OnDurabilityChanged -= OnShieldDamaged;
        _trackedShield.OnBroken -= OnShieldBroken;
    }

    private void OnShieldDamaged()
    {
        UpdateSprite();
        StopCoroutine(nameof(ShakeCoroutine));
        StartCoroutine(nameof(ShakeCoroutine));
    }

    private void OnShieldBroken()
    {
        if (shieldImage != null)
            shieldImage.sprite = noShieldSprite;
    }

    private void UpdateSprite()
    {
        bool hasShield = _trackedShield != null && _trackedShield.itemSpriteRenderer != null;

        if (!hasShield)
        {
            if (shieldImage != null)
                shieldImage.sprite = noShieldSprite;
            if (canvasGroup != null)
                canvasGroup.alpha = noShieldSprite != null ? 1f : 0f;
            return;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // Mirror the in-world sprite directly — TakeDurabilityDamage updates
        // itemSpriteRenderer.sprite before firing OnDurabilityChanged, so this
        // is always in sync without re-deriving the damage level independently.
        if (shieldImage != null)
            shieldImage.sprite = _trackedShield.itemSpriteRenderer.sprite;
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;
            float offset = Mathf.Sin(progress * shakeOscillations * Mathf.PI * 2f) * shakeMagnitude * (1f - progress);
            _rect.anchoredPosition = _originPos + new Vector2(offset, 0f);
            yield return null;
        }
        _rect.anchoredPosition = _originPos;
    }

    private void OnDestroy()
    {
        UnsubscribeFromShield();
    }
}
