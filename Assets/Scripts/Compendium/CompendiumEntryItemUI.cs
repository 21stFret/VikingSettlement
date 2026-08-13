using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for a single entry item in the compendium list.
/// </summary>
public class CompendiumEntryItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Button button;

    [Header("Locked Placeholder")]
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private string lockedDisplayName = "???";

    private CompendiumEntrySO entry;
    private CompendiumUI ownerUI;
    private bool isDiscovered;

    public CompendiumEntrySO Entry => entry;
    public bool IsDiscovered => isDiscovered;

    public void Initialize(CompendiumEntrySO entryData, bool discovered, CompendiumUI owner)
    {
        entry = entryData;
        ownerUI = owner;
        isDiscovered = discovered;
        button.onClick.AddListener(OnClick);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (iconImage != null)
            iconImage.sprite = isDiscovered ? entry.icon : lockedIcon;

        if (nameText != null)
            nameText.text = isDiscovered ? entry.displayName : lockedDisplayName;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isDiscovered);
    }

    public void OnClick()
    {
        if (!isDiscovered || ownerUI == null) return;

        ownerUI.ShowDetail(entry);
    }
}
