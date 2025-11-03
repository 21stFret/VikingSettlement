using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class EnemyPersonalUI : MonoBehaviour
{
    public TMP_Text speechText;
    public GameObject healthBar;
    public Image healthFill;
    public GameObject moraleBar;
    public Image moraleFill;
    private Enemy _enemy;
    private CanvasGroup _canvasGroup;
    private bool _enabled = true;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>(true);
        _enemy.personalUI = this;
        transform.parent = null; // Detach from enemy to avoid scaling issues
        _canvasGroup = GetComponent<CanvasGroup>();
        enabled = true;
    }

    void Start()
    {
        UpdateBars(true, true);
    }

    void Update()
    {
        if (!_enabled)
        {
            return;
        }
        transform.position = _enemy.transform.position + Vector3.up * 2.0f;
    }

    public void UpdateBars(bool health, bool morale)
    {
        healthBar.SetActive(false);
        if (_canvasGroup.alpha == 0)
        {
            _canvasGroup.DOFade(1, 0.2f).OnComplete(() =>
            {
                _canvasGroup.alpha = 1;
            });
        }
        if (_enemy != null)
        {      
            // Update health bar
            if (health)
            {
                if (healthBar != null && healthFill != null)
                {
                    healthBar.SetActive(true);
                    healthFill.fillAmount = _enemy.currentHealth / _enemy.maxHealth;
                }
            }
            StopAllCoroutines();
            StartCoroutine(HideAfterDelay(3f));
        }
    }

    public void ShowSpeech(string message, float duration = 2.0f)
    {
        if (_canvasGroup.alpha == 0)
        {
            _canvasGroup.DOFade(1, 0.2f).OnComplete(() =>
            {
                _canvasGroup.alpha = 1;
            });
        }
        if (speechText != null)
        {
            speechText.text = message;
            speechText.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideAfterDelay(duration));
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_canvasGroup != null)
        {
            _canvasGroup.DOFade(0, 0.5f).OnComplete(() =>
            {
                speechText.gameObject.SetActive(false);
                healthBar.SetActive(false);
                moraleBar.SetActive(false);
                _canvasGroup.alpha = 0;
            });
        }

    }

    public void Hide()
    {
        _enabled = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.DOFade(0, 0.5f).OnComplete(() =>
            {
                speechText.gameObject.SetActive(false);
                healthBar.SetActive(false);
                moraleBar.SetActive(false);
                _canvasGroup.alpha = 0;
            });
        }
    }
}
