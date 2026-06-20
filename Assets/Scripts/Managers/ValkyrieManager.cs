using UnityEngine;

public class ValkyrieManager : MonoBehaviour
{
    public static ValkyrieManager Instance { get; private set; }

    [SerializeField] private GameObject valkyrieEffectPrefab;
    [SerializeField] private ValkyrieEffect valkyrieEffectRef;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnForVillager(Villager villager)
    {
        if (valkyrieEffectPrefab == null)
        {
            Debug.LogWarning("[ValkyrieManager] No prefab assigned — villager body will not be removed.");
            return;
        }


        var GO = Instantiate(valkyrieEffectPrefab, villager.transform.position, Quaternion.identity);
        var effect = GO.GetComponentInChildren<ValkyrieEffect>();
        effect.Init(villager);
    }
}
