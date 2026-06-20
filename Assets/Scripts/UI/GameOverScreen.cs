using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game Over screen controller.
///
/// Sequence:
///   1. Blackout image fades in.
///   2. _BurnRadius on the game-over text material animates from 1.9 → 6.7 over 2s.
///   3. Buttons become interactable.
///
/// Setup:
///   - Place this on the root GameObject of your game-over canvas (start disabled).
///   - Assign blackoutImage, gameOverTextRenderer, and the two buttons.
///   - Call GameOverScreen.Instance.Show() from wherever game-over is triggered.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [Tooltip("The SpriteRenderer using the burn shader. An instance material is created at runtime so the shared asset is never modified.")]
    [SerializeField] private Image gameOverTextRenderer;
    [SerializeField] private Button loadLastSaveButton;
    [SerializeField] private Button quitToMenuButton;

    [Header("Blackout")]
    [SerializeField] private float blackoutFadeDuration = 1.2f;

    [Header("Burn Effect")]
    [SerializeField] private float burnStartValue = 1.9f;
    [SerializeField] private float burnEndValue   = 6.7f;
    [SerializeField] private float burnDuration   = 2f;
    [SerializeField] private Ease  burnEase       = Ease.OutQuad;

    private static readonly int BurnRadiusId = Shader.PropertyToID("_BurnRadius");

    // Runtime instance — created from the renderer so the shared asset stays clean
    private Material _instanceMaterial;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        loadLastSaveButton.onClick.AddListener(OnLoadLastSave);
        quitToMenuButton.onClick.AddListener(OnQuitToMenu);

        // .material creates an instance automatically; store it so we can destroy it later
        if (gameOverTextRenderer != null)
            _instanceMaterial = gameOverTextRenderer.material;

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        SetButtonsInteractable(false);

        if (_instanceMaterial != null)
            _instanceMaterial.SetFloat(BurnRadiusId, burnStartValue);

        rootCanvasGroup.DOFade(1f, blackoutFadeDuration)
            .OnComplete(PlayBurnEffect);
    }

    private void PlayBurnEffect()
    {
        if (_instanceMaterial == null)
        {
            SetButtonsInteractable(true);
            return;
        }

        DOTween.To(
            () => _instanceMaterial.GetFloat(BurnRadiusId),
            v  => _instanceMaterial.SetFloat(BurnRadiusId, v),
            burnEndValue,
            burnDuration
        )
        .SetEase(burnEase)
        .OnComplete(() => SetButtonsInteractable(true));
    }

    private void SetButtonsInteractable(bool interactable)
    {
        loadLastSaveButton.interactable = interactable;
        quitToMenuButton.interactable   = interactable;
    }

    private void OnLoadLastSave()
    {
        if (GameManager.Instance == null) return;

        if (SaveManager.Instance != null && SaveManager.Instance.HasAutosave())
            GameManager.Instance.LoadAutosave();
        else
            GameManager.Instance.LoadSlot(GameManager.Instance.CurrentSlot);
    }

    private void OnQuitToMenu()
    {
        GameManager.Instance?.ReturnToMainMenu();
    }

    private void OnDestroy()
    {
        DOTween.Kill(_instanceMaterial);
    }
}
