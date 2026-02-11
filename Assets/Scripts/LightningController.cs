using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class LightningController : MonoBehaviour
{
    [Header("Lightning Settings")]
    [SerializeField] private Light2D directionalLight;
    [SerializeField] private float minInterval = 5f;
    [SerializeField] private float maxInterval = 15f;
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField] private float maxIntensity = 3f;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] thunderSounds;
    [SerializeField] private float soundIntensityMultiplier = 2f; // Affects flash intensity based on sound volume

    [Header("Flash Patterns")]
    [SerializeField] private FlashPattern[] flashPatterns;

    private float nextLightningTime;

    public bool active;

    [System.Serializable]
    public class FlashPattern
    {
        public string patternName;
        public Flash[] sequence;
        [Range(0, 1)] public float probability = 1f;
    }

    [System.Serializable]
    public class Flash
    {
        public float duration = 0.1f;
        [Range(0, 1)] public float intensityMultiplier = 1f;
        public float delay = 0f;
    }

    private void Start()
    {
        // Don't override active state - let WeatherManager control it
        if (directionalLight == null)
        {
            Debug.LogWarning("LightningController: No directional light assigned!");
        }

        if (flashPatterns == null || flashPatterns.Length == 0)
        {
            SetupDefaultPattern();
        }

        SetNextLightningTime();
    }

    private void SetupDefaultPattern()
    {
        flashPatterns = new FlashPattern[]
        {
            new FlashPattern
            {
                patternName = "Single Flash",
                probability = 1f,
                sequence = new Flash[]
                {
                    new Flash { duration = 0.1f, intensityMultiplier = 1f }
                }
            }
        };
    }

    private void Update()
    {
        if(!active)
        {
            return;
        }
        if (Time.time >= nextLightningTime)
        {
            StartCoroutine(TriggerLightningSequence());
            SetNextLightningTime();
        }
    }

    private void SetNextLightningTime()
    {
        nextLightningTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private FlashPattern SelectRandomPattern()
    {
        float totalProbability = 0f;
        foreach (var pattern in flashPatterns)
        {
            totalProbability += pattern.probability;
        }

        float random = Random.Range(0f, totalProbability);
        float currentSum = 0f;

        foreach (var pattern in flashPatterns)
        {
            currentSum += pattern.probability;
            if (random <= currentSum)
            {
                return pattern;
            }
        }

        return flashPatterns[0];
    }

    private IEnumerator TriggerLightningSequence()
    {
        yield return new WaitForEndOfFrame();
        if(!active)
        {
            yield break;
        }
        FlashPattern selectedPattern = SelectRandomPattern();

        AudioClip selectedSound = null;
        float soundIntensity = 1f;
        if(thunderSounds != null && thunderSounds.Length > 0)
        {
            // Select and analyze random thunder sound
            selectedSound = thunderSounds[Random.Range(0, thunderSounds.Length)];
            soundIntensity = AnalyzeAudioClipIntensity(selectedSound);

            // Play thunder with slight delay to simulate distance
            float thunderDelay = Random.Range(0.05f, 0.2f);
            StartCoroutine(PlayThunderWithDelay(selectedSound, thunderDelay));
        }

        // Execute flash pattern
        foreach (Flash flash in selectedPattern.sequence)
        {
            if (flash.delay > 0)
                yield return new WaitForSeconds(flash.delay);

            float adjustedIntensity = maxIntensity * flash.intensityMultiplier * soundIntensity * soundIntensityMultiplier;
            directionalLight.intensity = Mathf.Clamp(adjustedIntensity, baseIntensity, maxIntensity);

            yield return new WaitForSeconds(flash.duration);
            directionalLight.intensity = baseIntensity;
        }
    }

    private IEnumerator PlayThunderWithDelay(AudioClip thunder, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.clip = thunder;
        audioSource.Play();
    }

    private float AnalyzeAudioClipIntensity(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        float maxAmplitude = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float absoluteValue = Mathf.Abs(samples[i]);
            if (absoluteValue > maxAmplitude)
            {
                maxAmplitude = absoluteValue;
            }
        }

        return Mathf.Clamp01(maxAmplitude);
    }
}