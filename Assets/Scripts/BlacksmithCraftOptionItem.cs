using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One entry in an equipment-crafting building's menu (e.g. Blacksmith's Shield / Iron Sword options).
/// Just an icon - clicking it selects this recipe in BuildingInfoPanel, which displays the shared
/// name/cost text and shared Queue button for whatever's currently selected (keeps the menu compact).
/// Mirrors VillagerWorkerItem's Setup()-then-direct-callback convention.
/// </summary>
public class BlacksmithCraftOptionItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;
    [Tooltip("Optional - shown while this option is the selected one.")]
    [SerializeField] private GameObject selectedHighlight;

    public EquipmentRecipe Recipe { get; private set; }
    private BuildingInfoPanel buildingPanel;

    /// <summary>
    /// Setup this craft-menu option.
    /// </summary>
    public void Setup(EquipmentRecipe recipe, BuildingInfoPanel panel)
    {
        Recipe = recipe;
        buildingPanel = panel;

        if (iconImage != null)
        {
            EquipableItem template = WeaponDatabase.Instance != null ? WeaponDatabase.Instance.GetItemByName(recipe.itemName) : null;
            Sprite icon = template != null && template.itemSpriteRenderer != null ? template.itemSpriteRenderer.sprite : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }

        SetSelected(false);
    }

    /// <summary>
    /// Toggle the selected-state highlight. Called by BuildingInfoPanel after a selection changes.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    /// <summary>
    /// Called when this option's icon is clicked.
    /// </summary>
    private void OnSelectClicked()
    {
        if (buildingPanel != null && Recipe != null)
        {
            buildingPanel.SelectCraftOption(Recipe);
        }
    }
}
