using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Attach to a campfire GameObject. Disables the fire animation and light
/// when the settlement runs out of wood (warmth goes cold), re-enables when warm.
/// Wire up fireAnimator and/or fireLight in the inspector.
/// </summary>
public class FireController : MonoBehaviour
{
    [SerializeField] public GameObject fire;
    [SerializeField] public GameObject fireOut;
    private SettlementManager settlementManager;

    public void Setup()
    {
        if (SettlementManager.Instance != null)
        {
            settlementManager = SettlementManager.Instance;
            SetFireState(settlementManager.IsSettlementWarm());
            settlementManager.OnWarmthChanged += SetFireState;
        }
    }

    private void OnDisable()
    {
        if (settlementManager != null)
            settlementManager.OnWarmthChanged -= SetFireState;
    }

    private void SetFireState(bool warm)
    {
        if (fire != null) fire.SetActive(warm);
        if (fireOut != null) fireOut.SetActive(!warm);
    }
}
