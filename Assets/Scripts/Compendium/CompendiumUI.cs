using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;

/// <summary>
/// Main compendium panel UI. Builds the entry list from CompendiumManager and
/// shows discovered/locked state, category filtering, progress, and entry detail.
/// </summary>
public class CompendiumUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject compendiumPanel;
    [SerializeField] private Button closeBtn;

    [Header("Entry Spawning")]
    [SerializeField] private RectTransform entryContainer;
    [SerializeField] private GameObject entryItemPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailCategory;

    [Header("Category Tabs")]
    [SerializeField] private Button allTabButton;
    [SerializeField] private Button resourceTabButton;
    [SerializeField] private Button weaponTabButton;
    [SerializeField] private Button buildingTabButton;
    [SerializeField] private Button enemyTabButton;

    [Header("Progress")]
    [Tooltip("Shows discovered/total for the active tab (or overall when 'All' is selected)")]
    [SerializeField] private TMP_Text progressText;

    private readonly List<CompendiumEntryItemUI> spawnedItems = new List<CompendiumEntryItemUI>();
    private CompendiumCategory? currentFilter = null;
    private bool isInitialized = false;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.ToggleCompendium.performed += OnToggleCompendiumInput;
    }

    private void OnDisable()
    {
        inputActions.Player.ToggleCompendium.performed -= OnToggleCompendiumInput;
        inputActions.Disable();
    }

    private void Start()
    {
        if (compendiumPanel != null)
            compendiumPanel.SetActive(false);

        if (detailPanel != null)
            detailPanel.SetActive(false);

        if (allTabButton != null)
            allTabButton.onClick.AddListener(() => SetFilter(null));
        if (resourceTabButton != null)
            resourceTabButton.onClick.AddListener(() => SetFilter(CompendiumCategory.Resource));
        if (weaponTabButton != null)
            weaponTabButton.onClick.AddListener(() => SetFilter(CompendiumCategory.Weapon));
        if (buildingTabButton != null)
            buildingTabButton.onClick.AddListener(() => SetFilter(CompendiumCategory.Building));
        if (enemyTabButton != null)
            enemyTabButton.onClick.AddListener(() => SetFilter(CompendiumCategory.Enemy));
        if (closeBtn != null)
            closeBtn.onClick.AddListener(ToggleCompendium);

        if (CompendiumManager.Instance != null)
        {
            CompendiumManager.Instance.OnEntryDiscovered += OnEntryDiscovered;
            CompendiumManager.Instance.OnCompendiumLoaded += OnCompendiumLoaded;
        }
    }

    private void OnDestroy()
    {
        if (CompendiumManager.Instance != null)
        {
            CompendiumManager.Instance.OnEntryDiscovered -= OnEntryDiscovered;
            CompendiumManager.Instance.OnCompendiumLoaded -= OnCompendiumLoaded;
        }
    }

    private void OnToggleCompendiumInput(InputAction.CallbackContext _) => ToggleCompendium();

    public void ToggleCompendium()
    {
        if (compendiumPanel == null) return;

        bool show = !compendiumPanel.activeSelf;
        compendiumPanel.SetActive(show);

        if (show)
        {
            isInitialized = true;
            BuildList();
            UpdateProgressDisplay();

            PlayerController.Instance?.SetInputEnabled(false);
            GameTickManager.Instance?.PushUIPause();
        }
        else
        {
            HideDetail();
            GameTickManager.Instance?.PopUIPause();
            PlayerController.Instance?.SetInputEnabled(true);
        }
    }

    #region List Building

    private void BuildList()
    {
        if (CompendiumManager.Instance == null || entryItemPrefab == null || entryContainer == null)
            return;

        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        foreach (var entry in GetEntriesForCurrentFilter())
        {
            CreateItem(entry);
        }
    }

    private List<CompendiumEntrySO> GetEntriesForCurrentFilter()
    {
        if (currentFilter.HasValue)
            return CompendiumManager.Instance.GetEntriesByCategory(currentFilter.Value);

        var all = new List<CompendiumEntrySO>();
        foreach (CompendiumCategory category in Enum.GetValues(typeof(CompendiumCategory)))
        {
            all.AddRange(CompendiumManager.Instance.GetEntriesByCategory(category));
        }
        return all;
    }

    private void CreateItem(CompendiumEntrySO entry)
    {
        GameObject itemObj = Instantiate(entryItemPrefab, entryContainer);
        CompendiumEntryItemUI itemUI = itemObj.GetComponent<CompendiumEntryItemUI>();

        if (itemUI != null)
        {
            bool discovered = CompendiumManager.Instance.IsDiscovered(entry.id);
            itemUI.Initialize(entry, discovered, this);
            spawnedItems.Add(itemUI);
        }
    }

    #endregion

    #region Detail Panel

    public void ShowDetail(CompendiumEntrySO entry)
    {
        if (detailPanel == null || entry == null) return;

        detailPanel.SetActive(true);

        if (detailIcon != null)
            detailIcon.sprite = entry.icon;

        if (detailName != null)
            detailName.text = entry.displayName;

        if (detailDescription != null)
            detailDescription.text = entry.description;

        if (detailCategory != null)
            detailCategory.text = entry.category.ToString();
    }

    public void HideDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    #endregion

    #region Filters & Progress

    private void SetFilter(CompendiumCategory? category)
    {
        currentFilter = category;
        HideDetail();
        BuildList();
        UpdateProgressDisplay();
    }

    private void UpdateProgressDisplay()
    {
        if (progressText == null || CompendiumManager.Instance == null) return;

        var (discovered, total) = currentFilter.HasValue
            ? CompendiumManager.Instance.GetProgress(currentFilter.Value)
            : CompendiumManager.Instance.GetOverallProgress();

        progressText.text = $"{discovered}/{total}";
    }

    #endregion

    #region Event Handlers

    private void OnEntryDiscovered(CompendiumEntrySO entry)
    {
        if (!isInitialized || compendiumPanel == null || !compendiumPanel.activeSelf) return;

        BuildList();
        UpdateProgressDisplay();
    }

    private void OnCompendiumLoaded()
    {
        if (!isInitialized || compendiumPanel == null || !compendiumPanel.activeSelf) return;

        BuildList();
        UpdateProgressDisplay();
    }

    public void Open()
    {
        if (compendiumPanel == null) return;

        compendiumPanel.SetActive(true);
        isInitialized = true;
        BuildList();
        UpdateProgressDisplay();

        PlayerController.Instance?.SetInputEnabled(false);
        GameTickManager.Instance?.PushUIPause();

    }

    public void Close()
    {
        if (compendiumPanel == null) return;

        compendiumPanel.SetActive(false);
        HideDetail();

        GameTickManager.Instance?.PopUIPause();
        PlayerController.Instance?.SetInputEnabled(true);

    }

    #endregion
}
