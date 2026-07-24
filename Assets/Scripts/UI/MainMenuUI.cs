using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main menu UI controller.
/// New Game: Select an empty slot (or delete a full one)
/// Load Game: Select a slot to load, or load from autosave
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Main Buttons Panel")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;

    [Header("Slot Selection Panel")]
    [SerializeField] private GameObject slotSelectionPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private List<GameObject> slotItemPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TextMeshProUGUI panelTitleText;

    [Header("Delete Confirmation")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private Button deleteConfirmYes;
    [SerializeField] private Button deleteConfirmNo;
    [SerializeField] private TextMeshProUGUI deleteConfirmText;

    [Header("Clan Name Entry")]
    [SerializeField] private GameObject clanNameEntryPanel;
    [SerializeField] private TMP_InputField clanNameInputField;
    [SerializeField] private Button clanNameConfirmButton;
    [SerializeField] private Button clanNameCancelButton;
    [SerializeField] private Button clanNameRandomButton;
    private const int MaxClanNameLength = 24;

    private bool isNewGameMode;
    private int selectedSlot;
    private SaveSlotInfo[] cachedSlots;
    private int currentslotCount;

    private void Start()
    {
        EnsureManagersExist();
        SetupButtonListeners();
        ShowMainButtons();
    }

    private void EnsureManagersExist()
    {
        if (SaveManager.Instance == null)
        {
            GameObject obj = new GameObject("SaveManager");
            obj.AddComponent<SaveManager>();
        }

        if (GameManager.Instance == null)
        {
            GameObject obj = new GameObject("GameManager");
            obj.AddComponent<GameManager>();
        }
    }

    private void SetupButtonListeners()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OnLoadGameClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        if (loadButton != null) loadButton.onClick.AddListener(OnLoadButtonClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        if (deleteConfirmYes != null) deleteConfirmYes.onClick.AddListener(OnDeleteConfirmed);
        if (deleteConfirmNo != null) deleteConfirmNo.onClick.AddListener(OnDeleteCancelled);
        if (clanNameConfirmButton != null) clanNameConfirmButton.onClick.AddListener(OnClanNameConfirmClicked);
        if (clanNameCancelButton != null) clanNameCancelButton.onClick.AddListener(OnClanNameCancelClicked);
        if (clanNameRandomButton != null) clanNameRandomButton.onClick.AddListener(OnClanNameRandomClicked);
        slotItemPrefab.Clear();
        var items = slotContainer.GetComponentsInChildren<SaveSlotItemUI>(true);
        foreach (var item in items)
        {            
            slotItemPrefab.Add(item.gameObject);
        }

    }

    private void ShowMainButtons()
    {
        SetPanelActive(mainButtonsPanel, true);
        SetPanelActive(slotSelectionPanel, false);
        SetPanelActive(deleteConfirmPanel, false);
        SetPanelActive(clanNameEntryPanel, false);
        UpdateLoadButtonState();
        UpdateDeleteButtonState();
        UpdateContinueButtonState();
        bool continueAvailable = continueButton != null && continueButton.interactable;
        UIFocus.Set(continueAvailable ? continueButton.gameObject : newGameButton?.gameObject);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void UpdateLoadButtonState()
    {
        if (loadGameButton == null || SaveManager.Instance == null) return;

        bool hasAnySave = SaveManager.Instance.HasAutosave();
        if (!hasAnySave)
        {
            var slots = SaveManager.Instance.GetAllSlots();
            foreach (var slot in slots)
            {
                if (slot.exists) { hasAnySave = true; break; }
            }
        }
        loadGameButton.interactable = hasAnySave;
    }

    private void UpdateDeleteButtonState()
    {
        if (deleteButton == null || SaveManager.Instance == null) return;

        bool slotHasSave = SaveManager.Instance.SlotHasSave(selectedSlot);
        deleteButton.interactable = slotHasSave;
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null || SaveManager.Instance == null) return;

        bool hasAutosave = SaveManager.Instance.HasAutosave();
        int lastSlot = SaveManager.GetLastPlayedSlot();
        bool hasLastSlotSave = lastSlot > 0 && SaveManager.Instance.SlotHasSave(lastSlot);
        continueButton.interactable = hasAutosave || hasLastSlotSave;
    }

    private void OnContinueClicked()
    {
        if (SaveManager.Instance == null) return;

        // Continue should resume wherever the player actually left off — that can be either
        // the shared autosave or a manual save in the last-played slot, whichever is newer.
        SaveSlotInfo autosaveInfo = SaveManager.Instance.GetAutosaveInfo();
        int lastSlot = SaveManager.GetLastPlayedSlot();
        SaveSlotInfo slotInfo = lastSlot > 0 ? SaveManager.Instance.GetSaveInfo(SaveManager.GetSlotName(lastSlot)) : null;

        bool slotIsNewer = slotInfo != null && slotInfo.exists
            && SaveManager.GetTimestamp(slotInfo) > SaveManager.GetTimestamp(autosaveInfo);

        if (slotIsNewer)
            GameManager.Instance?.LoadSlot(lastSlot);
        else if (autosaveInfo.exists)
            GameManager.Instance?.LoadAutosave();
        else if (slotInfo != null && slotInfo.exists)
            GameManager.Instance?.LoadSlot(lastSlot);
    }

    private void OnNewGameClicked()
    {
        isNewGameMode = true;
        ShowSlotSelection("New Game - Select Slot");
        UIFocus.Set(backButton?.gameObject);
    }

    private void OnLoadGameClicked()
    {
        isNewGameMode = false;
        ShowSlotSelection("Load Game");
    }

    private void ShowSlotSelection(string title)
    {
        SetPanelActive(mainButtonsPanel, false);
        SetPanelActive(slotSelectionPanel, true);
        if (panelTitleText != null) panelTitleText.text = title;
        RefreshSlotList();
    }

    private void RefreshSlotList()
    {
        currentslotCount = 0;

        if (slotContainer != null)
        {
            foreach (GameObject child in slotItemPrefab)
            {
                child.SetActive(false);
            }
        }

        cachedSlots = SaveManager.Instance?.GetAllSlots();
        if (cachedSlots == null) return;
                
        // In load mode, also show autosave if it exists
        if (!isNewGameMode && SaveManager.Instance.HasAutosave())
        {
            CreateAutosaveButton();
        }

        // For new game mode, show all slots
        // For load mode, show only slots with saves + autosave
        foreach (var slot in cachedSlots)
        {
            if (!isNewGameMode && !slot.exists) continue;
            CreateSlotButton(slot);
        }

        FocusFirstSlot();
    }

    private void FocusFirstSlot()
    {
        foreach (var itemObj in slotItemPrefab)
        {
            if (!itemObj.activeSelf) continue;
            var slot = itemObj.GetComponent<SaveSlotItemUI>();
            if (slot != null)
            {
                UIFocus.Set(slot.GetButton()?.gameObject);
                return;
            }
        }
        UIFocus.Set(backButton?.gameObject);
    }

    private void CreateSlotButton(SaveSlotInfo slot)
    {
        if (slotContainer == null || slotItemPrefab == null) return;

        GameObject itemObj = slotItemPrefab[currentslotCount];
        currentslotCount ++;
        itemObj.SetActive(true);
        SaveSlotItemUI item = itemObj.GetComponent<SaveSlotItemUI>();

        if (item != null)
        {
            item.Setup(slot, _ => OnSlotClicked(slot.slotNumber));
        }
    }

    private void CreateAutosaveButton()
    {
        if (slotContainer == null || slotItemPrefab == null) return;

        var autosaveInfo = SaveManager.Instance.GetAutosaveInfo();
        GameObject itemObj = slotItemPrefab[currentslotCount];
        itemObj.SetActive(true);
        currentslotCount ++;
        SaveSlotItemUI item = itemObj.GetComponent<SaveSlotItemUI>();

        if (item != null)
        {
            item.Setup(autosaveInfo, _ => OnSlotClicked(-1)); 
        }
    }

    private void OnSlotClicked(int slotNumber)
    {
        selectedSlot = slotNumber;
        UpdateLoadButtonState();
        UpdateDeleteButtonState();
        foreach (var itemObj in slotItemPrefab)
        {
            var item = itemObj.GetComponent<SaveSlotItemUI>();
            if (item != null)
            {
                item.SetSelected(false);
            }
        }
        UIFocus.Set(loadButton?.gameObject);
    }

    private void OnLoadButtonClicked()
    {
        if (isNewGameMode)
        {
            bool slotHasSave = SaveManager.Instance?.SlotHasSave(selectedSlot) ?? false;
            if (slotHasSave)
            {
                ShowOverwriteConfirmation($"Slot {selectedSlot} has a save.\nDelete and start new game?");
            }
            else
            {
                ShowClanNameEntry();
            }
        }
        else
        {
            if(selectedSlot == -1)
            {
                GameManager.Instance?.LoadAutosave();
                return;
            }
            GameManager.Instance?.LoadSlot(selectedSlot);
        }
    }

    private void OnDeleteButtonClicked()
    {
        bool slotHasSave = SaveManager.Instance?.SlotHasSave(selectedSlot) ?? false;
        if (slotHasSave)
        {
            ShowDeleteConfirmation($"Are you sure you want to delete the save in Slot {selectedSlot}?");
        }
    }

    public void OnApplicationQuit()
    {
        Application.Quit();
    }

    private void OnBackClicked()
    {
        ShowMainButtons();
    }

    private System.Action pendingAction;

    private void ShowDeleteConfirmation(string message)
    {
        SetPanelActive(deleteConfirmPanel, true);
        if (deleteConfirmText != null) deleteConfirmText.text = message;
        UIFocus.Push(deleteConfirmNo?.gameObject);

        pendingAction = () =>
        {
            SaveManager.Instance?.DeleteSlot(selectedSlot);
        };
    }

    private void ShowOverwriteConfirmation(string message)
    {
        SetPanelActive(deleteConfirmPanel, true);
        if (deleteConfirmText != null) deleteConfirmText.text = message;
        UIFocus.Push(deleteConfirmNo?.gameObject);

        pendingAction = () =>
        {
            SaveManager.Instance?.DeleteSlot(selectedSlot);
            ShowClanNameEntry();
        };
    }

    private void ShowClanNameEntry()
    {
        SetPanelActive(slotSelectionPanel, false);
        SetPanelActive(clanNameEntryPanel, true);
        if (clanNameInputField != null)
        {
            clanNameInputField.characterLimit = MaxClanNameLength;
            clanNameInputField.text = "";
        }
        UIFocus.Set(clanNameInputField != null ? clanNameInputField.gameObject : clanNameConfirmButton?.gameObject);
    }

    private void OnClanNameRandomClicked()
    {
        if (clanNameInputField != null)
        {
            clanNameInputField.text = VillagerNameGenerator.GenerateClanName();
        }
    }

    private void OnClanNameConfirmClicked()
    {
        string clanName = clanNameInputField != null ? clanNameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(clanName))
        {
            clanName = VillagerNameGenerator.GenerateClanName();
        }

        SetPanelActive(clanNameEntryPanel, false);
        GameManager.Instance?.StartNewGame(selectedSlot, clanName);
    }

    private void OnClanNameCancelClicked()
    {
        SetPanelActive(clanNameEntryPanel, false);
        SetPanelActive(slotSelectionPanel, true);
        UIFocus.Set(loadButton?.gameObject);
    }

    private void OnDeleteConfirmed()
    {
        SetPanelActive(deleteConfirmPanel, false);
        pendingAction?.Invoke();
        pendingAction = null;

        // The overwrite-confirm path routes into clan name entry instead of back to the slot
        // list; skip the refresh so it doesn't steal focus from the (now hidden) slot panel.
        if (clanNameEntryPanel == null || !clanNameEntryPanel.activeSelf)
        {
            RefreshSlotList();
            UpdateDeleteButtonState();
        }
    }

    private void OnDeleteCancelled()
    {
        SetPanelActive(deleteConfirmPanel, false);
        pendingAction = null;
        UIFocus.Pop();
    }
}
