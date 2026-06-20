using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public FoodUI foodUI;
    public HeatUI heatUI;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Init()
    {
        foodUI.Init();
        heatUI.Init();
    }
}
