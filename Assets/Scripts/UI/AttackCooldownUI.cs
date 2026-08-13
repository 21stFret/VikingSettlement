using DG.Tweening;
using System.Collections;
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
    public static AttackCooldownUI Instance;
    [Header("References")]
    [Tooltip("Filled Image whose fillAmount represents cooldown progress (0 = just fired, 1 = ready).")]
    [SerializeField] private Image fillImage;
    [Tooltip("CanvasGroup used for fading the indicator in and out.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Darker backing image shown behind the fill. Both backing and fill get the weapon sprite.")]
    [SerializeField] private Image backingImage;
    [Tooltip("Fallback sprite shown when no weapon is equipped.")]
    [SerializeField] private Sprite defaultWeaponSprite;
    private bool lowDurability = false;
    public Image durFill;

    [Header("Timing")]
    [Tooltip("Seconds after the attack is ready before the indicator fades out.")]
    [SerializeField] private float fadeOutDelay = 0.3f;
    [Tooltip("Duration of the fade-in and fade-out transitions.")]
    [SerializeField] private float fadeDuration = 0.15f;

    [SerializeField] private bool doFade;

    private Coroutine _redWarningCoroutine;
    private CharacterBase _trackedController;
    private EquipableItem _trackedWeapon;
    private bool _wasReady = true;
    private Tweener _fadeTween;

    private CharacterBase currentCC;

    public ShieldUI shieldUi;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple AttackCooldownUI instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public void Init()
    {
        // Initialize with the current weapon sprite if possible
        currentCC = PlayerController.Instance != null
            ? PlayerController.Instance.GetController()
            : null;
        if (currentCC != null)
        {
            _trackedController = currentCC;
            _trackedWeapon = currentCC.weapon;
            if(_trackedWeapon != null)
            {
                _trackedWeapon.OnDurabilityChanged += OnWeaponDurability;
            }

            var sprite = _trackedWeapon != null && _trackedWeapon.itemSpriteRenderer != null
                ? _trackedWeapon.itemSpriteRenderer.sprite
                : defaultWeaponSprite;
            if (fillImage != null) fillImage.sprite = sprite;
            if (backingImage != null) backingImage.sprite = sprite;
        }

        // Start fully transparent if fading is enabled
        if (doFade && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        OnChangeWeapon();
        
        if(shieldUi !=null)
        {
            shieldUi.Init();
        }

    }

    public void OnWeaponDurability()
    {
        if (_trackedWeapon == null) return;
        durFill.fillAmount = (float)_trackedWeapon.CurrentDurability / (float)_trackedWeapon.maxDurability;
        if (_trackedWeapon.CurrentDurability <= 5)
        {
            if(lowDurability) return; // Already in low durability state
            lowDurability = true;
            _redWarningCoroutine = StartCoroutine(RedWarning());
            InfoPopupUI.Push("Weapon Durability Low!", $"Your weapon is about to break! Repair it at the <color={InfoPopupUI.CyanHex}>Grindstone</color>.", _trackedWeapon.itemSpriteRenderer.sprite, "weapon_low");
        }
        else
        {
            lowDurability = false;
            if (fillImage != null)
                fillImage.color = Color.white;
        }
    }

    public void OnChangeWeapon()
    {
        if (_trackedController == null) return;
        var currentWeapon = _trackedController.weapon;
        if (currentWeapon != _trackedWeapon)
        {
            _trackedWeapon = currentWeapon;
            var sprite = currentWeapon != null && currentWeapon.itemSpriteRenderer != null
                ? currentWeapon.itemSpriteRenderer.sprite
                : defaultWeaponSprite;
            if (fillImage != null) fillImage.sprite = sprite;
            if (backingImage != null) backingImage.sprite = sprite;
        }
        OnWeaponDurability();
    }

    private void Update()
    {
        if (currentCC != _trackedController)
            _trackedController = currentCC;

        if (_trackedController == null) return;

        if (_trackedWeapon == null) return;

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

    private IEnumerator RedWarning()
    {
        while (lowDurability)
        {
            fillImage.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 2f, 1f));
            yield return null;
        }
        fillImage.color = Color.white; // reset when it exits, otherwise it can freeze mid-lerp
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        if(_redWarningCoroutine != null)
        {
            StopCoroutine(_redWarningCoroutine);
        }
        CancelInvoke(nameof(FadeOut));
    }
}
