using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunestoneUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject runestonePanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Option Items")]
    [SerializeField] private List<RunestoneOptionItem> optionItems = new List<RunestoneOptionItem>();

    [Header("Confirm")]
    [SerializeField] private Button confirmButton;

    private bool isReplacementMode = false;
    private List<RunestoneType> currentOptions;
    private RunestoneType selectedNewRunestone;
    private RunestoneType? pendingSelection;
    private string deadJarlName;

    private void Awake()
    {
        if (runestonePanel != null)
        {
            runestonePanel.SetActive(false);
        }

        foreach (var item in optionItems)
        {
            if (item != null) item.Hide();
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
            confirmButton.interactable = false;
        }
    }

    private void Start()
    {
        if (RunestoneManager.Instance != null)
        {
            RunestoneManager.Instance.OnSelectionStarted += ShowSelectionUI;
        }
    }

    private void OnDestroy()
    {
        if (RunestoneManager.Instance != null)
        {
            RunestoneManager.Instance.OnSelectionStarted -= ShowSelectionUI;
        }
    }

    private void ShowSelectionUI(List<RunestoneType> options, Villager deadJarl)
    {
        currentOptions = options;
        deadJarlName = deadJarl != null ? deadJarl.villagerName : "the Fallen";
        isReplacementMode = false;
        pendingSelection = null;

        if (runestonePanel != null)
        {
            runestonePanel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = $"Honour {deadJarlName}'s Legacy";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Choose a runestone to carve:";
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        PopulateOptions(currentOptions);
    }

    private void ShowReplacementUI()
    {
        isReplacementMode = true;
        pendingSelection = null;

        if (titleText != null)
        {
            titleText.text = "Memorial Full";
        }

        if (subtitleText != null)
        {
            var newInfo = RunestoneDatabase.GetInfo(selectedNewRunestone);
            subtitleText.text = $"Choose a runestone to replace with {newInfo.name}:";
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        var activeList = new List<RunestoneType>(RunestoneManager.Instance.ActiveRunestones);
        PopulateOptions(activeList);
    }

    private void PopulateOptions(List<RunestoneType> types)
    {
        for (int i = 0; i < optionItems.Count; i++)
        {
            if (optionItems[i] == null) continue;

            if (i < types.Count)
            {
                Sprite icon = RunestoneManager.Instance != null
                    ? RunestoneManager.Instance.GetIcon(types[i])
                    : null;

                optionItems[i].Setup(types[i], icon, OnOptionClicked);
            }
            else
            {
                optionItems[i].Hide();
            }
        }
    }

    private void OnOptionClicked(RunestoneType type)
    {
        pendingSelection = type;

        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
    }

    private void OnConfirm()
    {
        if (!pendingSelection.HasValue) return;
        if (RunestoneManager.Instance == null) return;

        RunestoneType chosen = pendingSelection.Value;

        if (!isReplacementMode)
        {
            if (RunestoneManager.Instance.IsAtCapacity)
            {
                selectedNewRunestone = chosen;
                ShowReplacementUI();
            }
            else
            {
                RunestoneManager.Instance.SelectRunestone(chosen);
                HideUI();
            }
        }
        else
        {
            RunestoneManager.Instance.ReplaceRunestone(chosen, selectedNewRunestone);
            HideUI();
        }
    }

    private void HideAll()
    {
        foreach (var item in optionItems)
        {
            if (item != null) item.Hide();
        }
    }

    private void HideUI()
    {
        HideAll();

        if (runestonePanel != null)
        {
            runestonePanel.SetActive(false);
        }

        isReplacementMode = false;
        currentOptions = null;
        pendingSelection = null;

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        PauseManager.Instance?.ExitStrategicPause();
    }
}
