using UnityEngine;
using UnityEngine.UI;

public class VillagerUIManager : MonoBehaviour
{
    public VillagerListUI villagerListUI;
    public GameObject villagerButtonGo;
    public Button villagerButton;
    public Button closeBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        villagerButton.onClick.AddListener(OnClick);
        closeBtn.onClick.AddListener(OnClose);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        villagerButtonGo.SetActive(false);
        villagerListUI.gameObject.SetActive(true);
    }

    private void OnClose()
    {
        villagerButtonGo.SetActive(true);
        villagerListUI.gameObject.SetActive(false);
    }
}
