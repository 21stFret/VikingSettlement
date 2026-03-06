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

    public void Setup()
    {
        if (SeasonManager.Instance != null)
        {
            SetFireState(SeasonManager.Instance.IsSettlementWarm());
            SeasonManager.Instance.OnWarmthChanged += SetFireState;
        }
    }

    private void OnDisable()
    {
        if (SeasonManager.Instance != null)
            SeasonManager.Instance.OnWarmthChanged -= SetFireState;
    }

    private void SetFireState(bool warm)
    {
        if (fire != null) fire.SetActive(warm);
        if (fireOut != null) fireOut.SetActive(!warm);
    }
}
