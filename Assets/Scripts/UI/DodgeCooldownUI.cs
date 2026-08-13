using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the Jarl's dodge roll cooldown as a filling sprite.
/// Fades in when a roll fires and fades out once the roll is ready again.
///
/// Quick setup:
///   1. Add a Canvas (World Space or Screen Space) with a child Image set to Filled mode.
///   2. Attach this component to any active GameObject.
///   3. Assign the Image and CanvasGroup in the Inspector.
/// </summary>
public class DodgeCooldownUI : MonoBehaviour
{
    public static DodgeCooldownUI Instance;
    [Header("References")]
    [Tooltip("Filled Image whose fillAmount represents cooldown progress (0 = just rolled, 1 = ready).")]
    [SerializeField] private Image fillImage;
    [Tooltip("CanvasGroup used for fading the indicator in and out.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [Tooltip("Seconds after the roll is ready before the indicator fades out.")]
    [SerializeField] private float fadeOutDelay = 0.3f;
    [Tooltip("Duration of the fade-in and fade-out transitions.")]
    [SerializeField] private float fadeDuration = 0.15f;

    [SerializeField] private bool doFade;

    private CharacterBase _trackedController;
    private bool _wasReady = true;
    private Tweener _fadeTween;

    private CharacterBase currentCC;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple DodgeCooldownUI instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    public void Init()
    {
        currentCC = PlayerController.Instance != null
            ? PlayerController.Instance.GetController()
            : null;
        if (currentCC != null)
        {
            _trackedController = currentCC;
        }
        // Start fully transparent if fading is enabled
        if (doFade && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (currentCC != _trackedController)
            _trackedController = currentCC;

        if (_trackedController == null) return;

        float progress = _trackedController.GetRollCooldownProgress();
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
