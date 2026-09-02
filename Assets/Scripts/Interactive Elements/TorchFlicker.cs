using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes a 2D light flicker like a torch or fire
/// </summary>
[RequireComponent(typeof(Light2D))]
public class TorchFlicker : MonoBehaviour
{
    [Header("Intensity Flicker")]
    [SerializeField] private float minIntensity = 0.8f;
    [SerializeField] private float maxIntensity = 1.2f;
    [SerializeField] private float flickerSpeed = 5f;
    
    [Header("Radius Flicker (Optional)")]
    [SerializeField] private bool flickerRadius = true;
    [SerializeField] private float minRadius = 0.9f;
    [SerializeField] private float maxRadius = 1.1f;

    [Header("Position Flicker (Optional)")]
    [SerializeField] private bool flickerPosition = true;
    [SerializeField] private float positionAmount = 0.05f; // Max distance from center
    [SerializeField] private float positionSpeed = 3f;

    [Header("Smoothness")]
    [SerializeField] private float smoothness = 0.1f; // Lower = more erratic

    private Light2D torchLight;
    private float baseIntensity;
    private float baseRadius;
    private Vector3 baseLocalPosition;
    private float intensityVelocity;
    private float radiusVelocity;
    private Vector2 positionVelocity;
    private float targetIntensity;
    private float targetRadius;
    private Vector2 targetPositionOffset;
    private float noiseOffset;
    private bool isEnabled = true;
    public ParticleSystem flameParticels;

    private void Awake()
    {
        torchLight = GetComponent<Light2D>();
        baseIntensity = torchLight.intensity;
        baseRadius = torchLight.pointLightOuterRadius;
        baseLocalPosition = transform.localPosition;

        // Random offset so multiple torches don't flicker in sync
        noiseOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        if (flameParticels != null)
        {
            DayNightManager.Instance.OnDayNightChanged += ToggleLights;
            ToggleLights(DayNightManager.Instance.IsDaytime());
        }
    }

    private void Update()
    {
        if (!isEnabled) return;
        FlickerLight();
    }

    private void ToggleLights(bool IsDay)
    {
        StartCoroutine(FadeLights(!IsDay));
    }

    private IEnumerator FadeLights(bool on)
    {
        float noiseFracation = noiseOffset / 100f;
        yield return new WaitForSeconds(noiseFracation); // Small delay to ensure the light is initialized
        if (flameParticels != null)
        {
            if (on)
            {
                flameParticels.Play();
            }
            else
            {
                flameParticels.Stop();
            }
        }
        isEnabled = on;
        float duration = 1f + (1f * noiseFracation); // Duration of the fade
        float elapsed = 0f;
        float startIntensity = torchLight.intensity;
        float targetIntensity = on ? baseIntensity : 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            torchLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }
        torchLight.intensity = targetIntensity;
    }
    
    private void FlickerLight()
    {
        // Use Perlin noise for smooth random values
        float intensityNoise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
        targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, intensityNoise) * baseIntensity;
        
        // Smooth damp to target for realistic flicker
        torchLight.intensity = Mathf.SmoothDamp(
            torchLight.intensity, 
            targetIntensity, 
            ref intensityVelocity, 
            smoothness
        );
        
        // Flicker radius if enabled
        if (flickerRadius)
        {
            float radiusNoise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset + 50f, 0f);
            targetRadius = Mathf.Lerp(minRadius, maxRadius, radiusNoise) * baseRadius;

            torchLight.pointLightOuterRadius = Mathf.SmoothDamp(
                torchLight.pointLightOuterRadius,
                targetRadius,
                ref radiusVelocity,
                smoothness
            );
        }

        // Flicker position if enabled
        if (flickerPosition)
        {
            float xNoise = Mathf.PerlinNoise(Time.time * positionSpeed + noiseOffset + 100f, 0f);
            float yNoise = Mathf.PerlinNoise(Time.time * positionSpeed + noiseOffset + 150f, 0f);

            // Convert from 0-1 to -1 to 1 range
            targetPositionOffset = new Vector2(
                (xNoise - 0.5f) * 2f * positionAmount,
                (yNoise - 0.5f) * 2f * positionAmount
            );

            Vector2 currentOffset = new Vector2(
                transform.localPosition.x - baseLocalPosition.x,
                transform.localPosition.y - baseLocalPosition.y
            );

            Vector2 newOffset = Vector2.SmoothDamp(
                currentOffset,
                targetPositionOffset,
                ref positionVelocity,
                smoothness
            );

            transform.localPosition = new Vector3(
                baseLocalPosition.x + newOffset.x,
                baseLocalPosition.y + newOffset.y,
                baseLocalPosition.z
            );
        }
    }
}
