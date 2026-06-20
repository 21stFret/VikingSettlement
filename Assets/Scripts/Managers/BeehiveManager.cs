using UnityEngine;

public class BeehiveManager : MonoBehaviour
{
    [Tooltip("All beehive particle systems to toggle with day/night")]
    public ParticleSystem[] beehiveEmitters;

    public void Initialize()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayNightChanged += SetEmitters;
            SetEmitters(DayNightManager.Instance.IsDaytime());
        }
        else
        {
            Debug.LogWarning("BeehiveManager: DayNightManager not found during Initialize!");
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
        foreach (var emitter in beehiveEmitters)
        {
            if (emitter == null) continue;

            if (isDay)
                emitter.Play();
            else
                emitter.Stop();
        }
    }
}
