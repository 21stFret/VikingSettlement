using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlash2D : MonoBehaviour
{
    private Light2D light2D;
    public float flashDuration = 0.5f;
    public float flashIntensity = 1f;
    private float originalIntensity;
    public bool isLooping = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        light2D = GetComponent<Light2D>();
        if(light2D == null)
        {
            Debug.LogError("LightFlash2D requires a Light2D component on the same GameObject.");
            return;
        }
        originalIntensity = light2D.intensity;
    }

    public void TriggerFlash()
    {
        if (light2D == null) return;
        StopAllCoroutines();
        StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        float flashTimer = flashDuration;
        while (flashTimer > 0)
        {
            light2D.intensity = flashIntensity;
            flashTimer -= Time.deltaTime;
            yield return null;
        }
        light2D.intensity = originalIntensity;
    }   
}
