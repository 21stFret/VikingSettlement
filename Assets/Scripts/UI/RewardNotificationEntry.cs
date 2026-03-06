using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to the notification entry prefab.
///
/// Prefab structure:
///   Root (RectTransform — managed by Layout Group, attach this script here)
///     └── Content (RectTransform — assign to contentRect, this is what slides)
///           ├── Icon (Image)
///           └── Amount (TextMeshProUGUI)
///
/// Call Setup() to initialise and start the show-then-hide animation.
/// </summary>
public class RewardNotificationEntry : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The inner panel that slides — keep separate from the layout-group root.")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [Tooltip("How far off-screen (in pixels) the content starts before sliding in.")]
    [SerializeField] private float slideOffsetX = 120f;
    [SerializeField] private float slideInDuration = 0.25f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float slideOutDuration = 0.2f;
    [SerializeField] private Ease slideInEase = Ease.OutBack;
    [SerializeField] private Ease slideOutEase = Ease.InQuad;

    private Action _onComplete;
    private Sequence _sequence;

    public void Setup(Sprite icon, string label, float amount, Action onComplete)
    {
        _onComplete = onComplete;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        string amountStr = (amount % 1f == 0f) ? ((int)amount).ToString() : amount.ToString("F1");
        amountText.text = $"+{amountStr}";

        // Reset content to off-screen start position
        contentRect.anchoredPosition = new Vector2(slideOffsetX, 0f);
        canvasGroup.alpha = 0f;

        _sequence?.Kill();
        _sequence = DOTween.Sequence();
        _sequence.Append(canvasGroup.DOFade(1f, slideInDuration * 0.5f));
        _sequence.Join(contentRect.DOAnchorPosX(0f, slideInDuration).SetEase(slideInEase));
        _sequence.AppendInterval(displayDuration);
        _sequence.Append(canvasGroup.DOFade(0f, slideOutDuration));
        _sequence.Join(contentRect.DOAnchorPosX(slideOffsetX, slideOutDuration).SetEase(slideOutEase));
        _sequence.OnComplete(() => _onComplete?.Invoke());
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
    }
}
