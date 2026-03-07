using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the Jarl's attack cooldown as a filling sprite.
/// Fades in when an attack fires and fades out once the attack is ready again.
///
/// Quick setup:
///   1. Add a Canvas (World Space or Screen Space) with a child Image set to Filled mode.
///   2. Attach this component to any active GameObject.
///   3. Assign the Image and CanvasGroup in the Inspector.
/// </summary>
public class AttackCooldownUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Filled Image whose fillAmount represents cooldown progress (0 = just fired, 1 = ready).")]
    [SerializeField] private Image fillImage;
    [Tooltip("CanvasGroup used for fading the indicator in and out.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Darker backing image shown behind the fill. Both backing and fill get the weapon sprite.")]
    [SerializeField] private Image backingImage;
    [Tooltip("Fallback sprite shown when no weapon is equipped.")]
    [SerializeField] private Sprite defaultWeaponSprite;

    [Header("Timing")]
    [Tooltip("Seconds after the attack is ready before the indicator fades out.")]
    [SerializeField] private float fadeOutDelay = 0.3f;
    [Tooltip("Duration of the fade-in and fade-out transitions.")]
    [SerializeField] private float fadeDuration = 0.15f;

    [SerializeField] private bool doFade;

    private CharacterController _trackedController;
    private EquipableItem _trackedWeapon;
    private bool _wasReady = true;
    private Tweener _fadeTween;

    private void Update()
    {
        // Always follow whoever the player is currently controlling (handles Jarl succession)
        var currentCC = PlayerController.Instance != null
            ? PlayerController.Instance.GetController()
            : null;

        if (currentCC != _trackedController)
            _trackedController = currentCC;

        if (_trackedController == null) return;

        // Update weapon sprite on both images when the equipped weapon changes
        var currentWeapon = _trackedController.weapon;
        if (currentWeapon != _trackedWeapon)
        {
            _trackedWeapon = currentWeapon;
            var sprite = currentWeapon != null && currentWeapon.itemSpriteRenderer != null
                ? currentWeapon.itemSpriteRenderer.sprite
                : defaultWeaponSprite;
            if (fillImage != null)    fillImage.sprite = sprite;
            if (backingImage != null) backingImage.sprite = sprite;
        }

        float progress = _trackedController.GetAttackCooldownProgress();
        if (fillImage != null)
            fillImage.fillAmount = progress;

        bool isReady = progress >= 1f;

        if (!isReady && _wasReady)
        {
            if (doFade)
            {
                FadeTo(1f);
            }

        }
        else if (isReady && !_wasReady)
        {
            if (doFade)
            {
                // Cooldown just finished — start fade-out timer
                CancelInvoke(nameof(FadeOut));
                Invoke(nameof(FadeOut), fadeOutDelay);
            }            
        }

        _wasReady = isReady;
    }

    private void FadeOut() => FadeTo(0f);

    private void FadeTo(float target)
    {
        if (canvasGroup == null) return;
        _fadeTween?.Kill();
        _fadeTween = canvasGroup.DOFade(target, fadeDuration);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        CancelInvoke(nameof(FadeOut));
    }
}
