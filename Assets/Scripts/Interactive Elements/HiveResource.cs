using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class HiveResource : HarvestableResource
{
    public ParticleSystem beeEmitter;

    private void Start()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayNightChanged += SetEmitters;
            SetEmitters(DayNightManager.Instance.IsDaytime());
        }
        else
        {
            Debug.LogWarning("Beehive: DayNightManager not found during Initialize!");
        }
    }

    private void OnDestroy()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayNightChanged -= SetEmitters;
        }
    }

    private void SetEmitters(bool isDay)
    {
        if (beeEmitter == null) return;

        if (isDay && !pendingRespawn)
            beeEmitter.Play();
        else
            beeEmitter.Stop();
    }

    protected override void Deplete()
    {
        base.Deplete();
        SetEmitters(false);
    }

    protected override void Respawn()
    {
        base.Respawn();
        SetEmitters(DayNightManager.Instance != null && DayNightManager.Instance.IsDaytime());
    }
}
