using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VillagerUIManager : MonoBehaviour
{
    public VillagerListUI villagerListUI;
    public VillagerInfoPanel villagerInfoPanel;
    public Button closeBtn;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        closeBtn.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.VillagerPanel.performed += GetAdvanceInput;
    }

    private void OnDisable()
    {
        inputActions.Player.VillagerPanel.performed -= GetAdvanceInput;
        inputActions.Disable();
    }

    public void GetAdvanceInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnClick();
        }
    }

    public void OnClick()
    {

    }

    public void Open()
    {
        if (villagerListUI.gameObject.activeSelf) return;

        villagerListUI.gameObject.SetActive(true);
        villagerListUI.OnOpen();
        villagerInfoPanel.ShowBlocker();
        PlayerController.Instance?.SetInputEnabled(false);
        GameTickManager.Instance?.PushUIPause();
    }

    public void Close()
    {
        if (!villagerListUI.gameObject.activeSelf) return;

        villagerListUI.OnClose();
        villagerListUI.gameObject.SetActive(false);
        villagerInfoPanel.Hide();
        PlayerController.Instance?.SetInputEnabled(true);
        GameTickManager.Instance?.PopUIPause();
    }
}
