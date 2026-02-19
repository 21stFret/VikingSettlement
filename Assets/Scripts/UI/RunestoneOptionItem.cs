using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunestoneOptionItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image runeImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button button;

    private RunestoneType assignedType;
    private System.Action<RunestoneType> onClickCallback;

    private void Awake()
    {
        if (button == null)
            button = GetComponentInChildren<Button>();

        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    public void Setup(RunestoneType type, Sprite icon, System.Action<RunestoneType> callback)
    {
        assignedType = type;
        onClickCallback = callback;

        var info = RunestoneDatabase.GetInfo(type);

        if (nameText != null)
            nameText.text = info.name;

        if (infoText != null)
            infoText.text = info.description;

        if (runeImage != null)
        {
            runeImage.sprite = icon;
            runeImage.enabled = icon != null;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        onClickCallback = null;
    }

    private void OnClicked()
    {
        onClickCallback?.Invoke(assignedType);
    }
}
