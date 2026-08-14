using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One entry in an equipment-crafting building's current queue (e.g. Blacksmith). Just an icon plus its
/// own cancel button - cancelling the item actually being crafted (index 0) forfeits the materials
/// already spent on it; anything still waiting in line hasn't consumed any resources yet, so cancelling
/// it is free. Mirrors VillagerWorkerItem's Setup()-then-direct-callback convention.
/// </summary>
public class BlacksmithQueueItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button cancelButton;
    [Tooltip("Optional - shown only for the item currently being crafted (queue index 0).")]
    [SerializeField] private GameObject inProgressHighlight;

    private int queueIndex;
    private BuildingInfoPanel buildingPanel;

    /// <summary>
    /// Setup this queue-row item. queueIndex is this item's position in the queue (0 = currently crafting).
    /// </summary>
    public void Setup(string itemName, int queueIndex, BuildingInfoPanel panel)
    {
        this.queueIndex = queueIndex;
        buildingPanel = panel;

        if (iconImage != null)
        {
            EquipableItem template = WeaponDatabase.Instance != null ? WeaponDatabase.Instance.GetItemByName(itemName) : null;
            Sprite icon = template != null && template.itemSpriteRenderer != null ? template.itemSpriteRenderer.sprite : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (inProgressHighlight != null)
            inProgressHighlight.SetActive(queueIndex == 0);

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }

    /// <summary>
    /// Called when the cancel button is clicked.
    /// </summary>
    private void OnCancelClicked()
    {
        if (buildingPanel != null)
        {
            buildingPanel.CancelQueuedCraftItem(queueIndex);
        }
    }
}
