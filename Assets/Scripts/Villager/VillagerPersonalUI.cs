using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
public enum VillagerStatusEffect
{
    Love,
    Injured,
    Cold,
    Happy,
    Angry,
    None
}

public class VillagerPersonalUI : MonoBehaviour
{
    public TMP_Text speechText;
    public CanvasGroup textCanvas;
    public CanvasGroup healthBar;
    public Image healthFill;
    public CanvasGroup moraleBar;
    public Image moraleFill;
    private Villager _villager;
    public CanvasGroup statusImageCanvas;
    public Image statusEffectIcon;
    public Sprite[] satusEffectIcons;
    public CanvasGroup blockCounterCanvas;
    public GameObject[] blockCounters;
    private Coroutine speechCoroutine;
    private Coroutine statusEffectCoroutine;
    private Coroutine healthbarCoroutine;
    private Coroutine moraleCoroutine;
    private Coroutine blockCounterCoroutine;
    private CharacterAI _ai;

    private void Awake()
    {
        _villager = GetComponentInParent<Villager>(true);
        _villager.personalUI = this;
        _ai = GetComponentInParent<CharacterAI>(true);
        if (_ai != null)
            _ai.OnBlockChargesChanged += UpdateBlockCounter;
        enabled = true;
    }

    private void OnDestroy()
    {
        if (_ai != null)
            _ai.OnBlockChargesChanged -= UpdateBlockCounter;
    }

    void Start()
    {
        UpdateBars(true, true);
        statusImageCanvas.alpha = 0;
        textCanvas.alpha = 0;
        if (blockCounterCanvas != null)
            blockCounterCanvas.alpha = 0;
    }

    public void UpdateBars(bool health, bool morale)
    {
        if (health)
        {
            UpdateHealthBar();
        }
        if (morale)
        {
            UpdateMoraleBar();
        }
    }

    public void UpdateHealthBar()
    {
        if (healthBar != null && healthFill != null)
        {
            healthFill.fillAmount = _villager.currentHealth / _villager.maxHealth;
            if(healthbarCoroutine != null)
            {
                StopCoroutine(healthbarCoroutine);
            }
            healthbarCoroutine = StartCoroutine(HideAfterDelay(3f, healthBar));
        }
    }

    public void UpdateMoraleBar()
    {
        if (moraleBar != null && moraleFill != null)
        {
            moraleFill.fillAmount = _villager.morale / _villager.maxMorale;
            if (moraleCoroutine != null)
            {
                StopCoroutine(moraleCoroutine);
            }
            moraleCoroutine = StartCoroutine(HideAfterDelay(3f, moraleBar));
        }
    }

    public void ShowSpeech(string message, float duration = 2.0f)
    {
        if (speechText != null)
        {
            if (speechCoroutine != null)
            {
                StopCoroutine(speechCoroutine);
            }
            speechText.text = message;
            speechText.gameObject.SetActive(true);
            speechCoroutine = StartCoroutine(HideAfterDelay(duration, textCanvas));
        }
    }

    public void UpdateStatusEffectIcon(VillagerStatusEffect effect)
    {
        Sprite newIcon = null;
        switch (effect)
        {
            case VillagerStatusEffect.Love:
                newIcon = satusEffectIcons[0];
                break;
            case VillagerStatusEffect.Injured:
                newIcon = satusEffectIcons[1];
                break;
            case VillagerStatusEffect.Cold:
                newIcon = satusEffectIcons[2];
                break;
            case VillagerStatusEffect.Happy:
                newIcon = satusEffectIcons[3];
                break;
            case VillagerStatusEffect.Angry:
                newIcon = satusEffectIcons[4];
                break;
            case VillagerStatusEffect.None:
            default:
                newIcon = null;
                break;
        }

        if (statusEffectIcon != null)
        {
            if (newIcon != null)
            {
                statusEffectIcon.sprite = newIcon;
                statusEffectIcon.gameObject.SetActive(true);
            }
            else
            {
                statusEffectIcon.gameObject.SetActive(false);
            }
            if (statusEffectCoroutine != null)
            {
                StopCoroutine(statusEffectCoroutine);
            }
            statusEffectCoroutine = StartCoroutine(HideAfterDelay(2f, statusImageCanvas));
        }
    }

    public void UpdateBlockCounter(int amount)
    {
        if (blockCounterCanvas == null || blockCounters == null) return;

        for (int i = 0; i < blockCounters.Length; i++)
            blockCounters[i].SetActive(i < amount);

        if (blockCounterCoroutine != null)
        {
            StopCoroutine(blockCounterCoroutine);
        }
        blockCounterCoroutine = StartCoroutine(HideAfterDelay(3f, blockCounterCanvas));
    }

    private IEnumerator HideAfterDelay(float delay, CanvasGroup _canvasGroup)
    {
        _canvasGroup.DOFade(1, 0.1f);
        yield return new WaitForSeconds(delay);
        _canvasGroup.DOFade(0, 0.5f);
    }

    public void Hide()
    {
        textCanvas.DOFade(0, 0.5f);
        healthBar.DOFade(0, 0.5f);
        moraleBar.DOFade(0, 0.5f);
        statusImageCanvas.DOFade(0, 0.5f);
        if (blockCounterCanvas != null)
            blockCounterCanvas.DOFade(0, 0.5f);
    }

}
