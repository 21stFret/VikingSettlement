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
    
    [Header("Smoothness")]
    [SerializeField] private float smoothness = 0.1f; // Lower = more erratic
    
    private Light2D torchLight;
    private float baseIntensity;
    private float baseRadius;
    private float intensityVelocity;
    private float radiusVelocity;
    private float targetIntensity;
    private float targetRadius;
    private float noiseOffset;
    
    private void Awake()
    {
        torchLight = GetComponent<Light2D>();
        baseIntensity = torchLight.intensity;
        baseRadius = torchLight.pointLightOuterRadius;
        
        // Random offset so multiple torches don't flicker in sync
        noiseOffset = Random.Range(0f, 100f);
    }
    
    private void Update()
    {
        FlickerLight();
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
    }
}
