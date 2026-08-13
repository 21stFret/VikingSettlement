using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI component for an individual save slot button.
/// </summary>
public class SaveSlotItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.5f, 0.7f, 0.9f);
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    public string SlotName { get; private set; }
    public bool HasSave { get; private set; }
    public Button GetButton() => button;

    private Action<SaveSlotItemUI> onSelected;
    private bool isSelected;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    public void Setup(SaveSlotInfo info, Action<SaveSlotItemUI> onSelectedCallback)
    {
        SlotName = info.slotName;
        HasSave = info.exists;
        onSelected = onSelectedCallback;

        // Manual slots show the player's clan name once they have a save; autosave and
        // empty slots fall back to the generic "Slot N" / "Auto Save" label.
        string displayName;
        if (info.slotName != SaveManager.AUTOSAVE_NAME && info.exists && !string.IsNullOrEmpty(info.clanName))
            displayName = info.clanName;
        else
            displayName = FormatSlotName(info.slotName);

        if (slotNameText != null) slotNameText.text = displayName;

        // Show timestamp or empty indicator
        if (info.exists)
        {
            if (timestampText != null)
            {
                timestampText.text = info.saveTimestamp;
                timestampText.gameObject.SetActive(true);
            }
            if (emptyIndicator != null) emptyIndicator.SetActive(false);
        }
        else
        {
            if (timestampText != null) timestampText.gameObject.SetActive(false);
            if (emptyIndicator != null) emptyIndicator.SetActive(true);
        }

        SetSelected(false);
        //UpdateBackgroundColor();
    }

    private string FormatSlotName(string slotName)
    {
        // Handle autosave
        if (slotName == SaveManager.AUTOSAVE_NAME)
        {
            return "Auto Save";
        }

        // Format "slot1" -> "Slot 1"
        if (slotName.StartsWith("slot"))
        {
            string num = slotName.Substring(4);
            return $"Slot {num}";
        }

        // Already formatted (e.g., "Slot 1")
        return slotName;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedIndicator != null) selectedIndicator.SetActive(selected);
        //UpdateBackgroundColor();
    }

    private void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;

        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (!HasSave)
        {
            backgroundImage.color = emptyColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }

    private void OnButtonClicked()
    {
        onSelected?.Invoke(this);
        SetSelected(true);
    }
}
