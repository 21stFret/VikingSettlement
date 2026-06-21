using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VillagerUIManager : MonoBehaviour
{
    public VillagerListUI villagerListUI;
    public GameObject villagerButtonGo;
    public Button villagerButton;
    public Button closeBtn;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        villagerButton.onClick.AddListener(OnClick);
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
        if(villagerButtonGo.activeSelf)
        {
            villagerButtonGo.SetActive(false);
            villagerListUI.gameObject.SetActive(true);
            villagerListUI.OnOpen();
            PlayerController.Instance?.SetInputEnabled(false);
        }
        else
        {
            villagerButtonGo.SetActive(true);
            villagerListUI.OnClose();
            villagerListUI.gameObject.SetActive(false);
            PlayerController.Instance?.SetInputEnabled(true);
        }

    }
}
