using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class VillagerPersonalUI : MonoBehaviour
{
    public TMP_Text speechText;
    public GameObject healthBar;
    public Image healthFill;
    public GameObject moraleBar;
    public Image moraleFill;
    private Villager _villager;
    private CanvasGroup _canvasGroup;
    private bool _enabled = true;

    private void Awake()
    {
        _villager = GetComponentInParent<Villager>(true);
        _villager.personalUI = this;
        transform.parent = null; // Detach from villager to avoid scaling issues
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
        transform.position = _villager.transform.position + Vector3.up * 2.0f;
    }

    public void UpdateBars(bool health, bool morale)
    {
        healthBar.SetActive(false);
        moraleBar.SetActive(false);
        if (_canvasGroup.alpha == 0)
        {
            _canvasGroup.DOFade(1, 0.2f).OnComplete(() =>
            {
                _canvasGroup.alpha = 1;
            });
        }
        if (_villager != null)
        {      
            // Update health bar
            if (health)
            {
                if (healthBar != null && healthFill != null)
                {
                    healthBar.SetActive(true);
                    healthFill.fillAmount = _villager.health / _villager.maxHealth;
                }
            }
            if (morale)
            {
                // Update morale bar
                if (moraleBar != null && moraleFill != null)
                {
                    moraleBar.SetActive(true);
                    moraleFill.fillAmount = _villager.morale / _villager.maxMorale;
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
