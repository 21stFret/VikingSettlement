using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages seasonal changes and their visual effects
/// </summary>
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    [Header("Season Settings")]
    [Tooltip("Number of days before the season changes")]
    public int daysPerSeason = 15;

    [Tooltip("Current season")]
    [SerializeField] private Season currentSeason = Season.Summer;

    [Tooltip("Days remaining in current season")]
    [SerializeField] private int daysUntilSeasonChange;

    [Header("Summer Effects")]
    [Tooltip("Parent object containing all sun beam lights")]
    public GameObject summerSunBeamsParent;
    public ParticleSystem sunDustParticleSystem;
    public ParticleSystem fireFliesParticleSystem;

    [Tooltip("Individual sun beam lights (will auto-populate from parent if empty)")]
    public List<Light2D> summerSunBeams = new List<Light2D>();

    [Tooltip("Intensity of sun beams during summer")]
    public float sunBeamIntensity = 0.8f;

    [Tooltip("Should sun beams flicker/animate?")]
    public bool animateSunBeams = true;

    [Tooltip("Speed of sun beam animation")]
    public float sunBeamAnimationSpeed = 2f;

    [Header("Winter Effects")]
    [Tooltip("Snow particle system")]
    public ParticleSystem snowParticleSystem;

    [Tooltip("Should snow intensity vary with time of day?")]
    public bool varySnowWithTimeOfDay = true;

    [Tooltip("Base emission rate for snow particles")]
    public float baseSnowEmissionRate = 50f;

    [Tooltip("Maximum emission rate for snow particles")]
    public float maxSnowEmissionRate = 100f;

    [Header("Season Transition")]
    [Tooltip("How long the transition between seasons takes (in seconds)")]
    public float transitionDuration = 5f;

    [SerializeField] private bool isTransitioning = false;
    [SerializeField] private float transitionProgress = 0f;

    [Header("Lighting Adjustments")]
    [Tooltip("Multiplier for ambient light during summer")]
    public float summerAmbientMultiplier = 1.2f;

    [Tooltip("Multiplier for ambient light during winter")]
    public float winterAmbientMultiplier = 0.8f;

    [Tooltip("Sun color tint during winter")]
    public Color winterSunTint = new Color(0.9f, 0.95f, 1f);

    [Tooltip("Sun color tint during summer")]
    public Color summerSunTint = new Color(1f, 1f, 0.95f);

    // Events
    public event Action<Season> OnSeasonChanged;

    // Private variables for animation
    private float sunBeamAnimationTime = 0f;
    private ParticleSystem.EmissionModule snowEmission;

    private int currentSolarYear = 1;

    public enum Season
    {
        Summer,
        Winter
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize days until season change
        daysUntilSeasonChange = daysPerSeason;

        // Auto-populate sun beams from parent if not manually assigned
        if (summerSunBeamsParent != null && summerSunBeams.Count == 0)
        {
            Light2D[] beams = summerSunBeamsParent.GetComponentsInChildren<Light2D>();
            summerSunBeams.AddRange(beams);
        }

        // Get snow emission module if particle system exists
        if (snowParticleSystem != null)
        {
            snowEmission = snowParticleSystem.emission;
        }
    }

    private void Start()
    {
        // Subscribe to day change events
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNewDay += OnNewDay;
        }
        else
        {
            Debug.LogWarning("SeasonManager: DayNightManager not found!");
        }

        // Subscribe to game tick for updates
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnFastUpdate += FastUpdate;
        }

        // Initialize the current season's effects
        ApplySeasonEffects(currentSeason);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNewDay -= OnNewDay;
        }

        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnFastUpdate -= FastUpdate;
        }
    }

    private void OnNewDay()
    {
        daysUntilSeasonChange--;

        Debug.Log($"Days until season change: {daysUntilSeasonChange}");

        // Check if it's time to change seasons
        if (daysUntilSeasonChange <= 0)
        {
            ChangeSeason();
        }
    }

    private void FastUpdate()
    {
        // Handle season transition
        if (isTransitioning)
        {
            transitionProgress += Time.deltaTime / transitionDuration;

            if (transitionProgress >= 1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
            }
        }

        // Update current season effects
        UpdateSeasonEffects();
    }

    private void UpdateSeasonEffects()
    {
        var sunTint = Color.white;
        switch (currentSeason)
        {
            case Season.Summer:
                UpdateSummerEffects();
                sunTint = summerSunTint;
                break;
            case Season.Winter:
                UpdateWinterEffects();
                sunTint = winterSunTint;
                break;
        }

        DayNightManager.Instance.sunColor = Color.Lerp(
        DayNightManager.Instance.sunColor, 
        sunTint, 
        0.5f * Time.deltaTime);
    }

    private void UpdateSummerEffects()
    {
        if (summerSunBeams.Count == 0) return;

        // Animate sun beams if enabled
        if (animateSunBeams)
        {
            sunBeamAnimationTime += Time.deltaTime * sunBeamAnimationSpeed;

            for (int i = 0; i < summerSunBeams.Count; i++)
            {
                if (summerSunBeams[i] == null) continue;

                // Create a slightly different animation offset for each beam
                float offset = i * 0.5f;
                float animatedIntensity = sunBeamIntensity * 
                    (0.7f + 0.3f * Mathf.Sin(sunBeamAnimationTime + offset));

                // Only show sun beams during daytime
                if (DayNightManager.Instance != null)
                {
                    float timeOfDay = DayNightManager.Instance.GetTimeOfDay();
                    bool isDaytime = timeOfDay >= 0.25f && timeOfDay <= 0.75f;

                    if (isDaytime)
                    {
                        summerSunBeams[i].intensity = animatedIntensity;
                        if(!sunDustParticleSystem.isPlaying)
                        {
                            sunDustParticleSystem.Play();
                        }
                        fireFliesParticleSystem.Stop();
                    }
                    else
                    {
                        summerSunBeams[i].intensity = 0f;
                        sunDustParticleSystem.Stop();
                        if(!fireFliesParticleSystem.isPlaying)
                        {
                            fireFliesParticleSystem.Play();
                        }
                    }
                }
                else
                {
                    summerSunBeams[i].intensity = animatedIntensity;
                }
            }
        }
    }

    private void UpdateWinterEffects()
    {
        if (snowParticleSystem == null) return;

        // Vary snow intensity with time of day if enabled
        if (varySnowWithTimeOfDay && DayNightManager.Instance != null)
        {
            float timeOfDay = DayNightManager.Instance.GetTimeOfDay();
            
            // Snow falls heavier at night
            float snowIntensity;
            if (timeOfDay < 0.5f)
            {
                // Morning to noon - lighter snow
                snowIntensity = Mathf.Lerp(maxSnowEmissionRate, baseSnowEmissionRate, timeOfDay * 2f);
            }
            else
            {
                // Noon to night - heavier snow
                snowIntensity = Mathf.Lerp(baseSnowEmissionRate, maxSnowEmissionRate, (timeOfDay - 0.5f) * 2f);
            }

            snowEmission.rateOverTime = snowIntensity;
        }
    }

    private void ChangeSeason()
    {
        // Toggle between summer and winter
        Season newSeason = currentSeason == Season.Summer ? Season.Winter : Season.Summer;

        Debug.Log($"Season changing from {currentSeason} to {newSeason}");

        // Start transition
        isTransitioning = true;
        transitionProgress = 0f;

        // Apply new season effects
        ApplySeasonEffects(newSeason);

        currentSeason = newSeason;
        daysUntilSeasonChange = daysPerSeason;

        // Trigger event
        OnSeasonChanged?.Invoke(currentSeason);

        // Increment solar year if transitioning from Winter to Summer
        if (currentSeason == Season.Summer)
        {            
            currentSolarYear++;
        }
    }

    private void ApplySeasonEffects(Season season)
    {
        switch (season)
        {
            case Season.Summer:
                EnableSummerEffects(true);
                EnableWinterEffects(false);
                ApplySummerLighting();
                break;
            case Season.Winter:
                EnableWinterEffects(true);
                EnableSummerEffects(false);
                ApplyWinterLighting();
                break;
        }
    }

    private void EnableSummerEffects(bool value)
    {
        // Enable sun beams
        if (summerSunBeamsParent != null)
        {
            summerSunBeamsParent.SetActive(value);
        }

        foreach (var beam in summerSunBeams)
        {
            if (beam != null)
            {
                beam.enabled = value;
            }
        }

        if (sunDustParticleSystem != null)
        {
            if (value && !sunDustParticleSystem.isPlaying)
            {
                sunDustParticleSystem.Play();
            }
            else if (!value && sunDustParticleSystem.isPlaying)
            {
                sunDustParticleSystem.Stop();
            }
        }

        if (fireFliesParticleSystem != null)
        {
            if (!value && !fireFliesParticleSystem.isPlaying)
            {
                fireFliesParticleSystem.Play();
            }
            else if (value && fireFliesParticleSystem.isPlaying)
            {
                fireFliesParticleSystem.Stop();
            }
        }

        Debug.Log("Summer effects enabled");
    }

    private void EnableWinterEffects(bool value)
    {
        // Enable snow particles
        if (snowParticleSystem != null)
        {
            snowParticleSystem.gameObject.SetActive(value);
            if (value && !snowParticleSystem.isPlaying)
            {
                snowParticleSystem.Play();
            }
            else if (!value && snowParticleSystem.isPlaying)
            {
                snowParticleSystem.Stop();
            }
            snowEmission.rateOverTime = baseSnowEmissionRate;
        }

        Debug.Log("Winter effects enabled");
    }

    private void ApplySummerLighting()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.eveningMultiplier = summerAmbientMultiplier;
            
        }
    }

    private void ApplyWinterLighting()
    {
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.eveningMultiplier = winterAmbientMultiplier;
        }
    }

    // Public API Methods

    /// <summary>
    /// Get the current season
    /// </summary>
    public Season GetCurrentSeason()
    {
        return currentSeason;
    }

    /// <summary>
    /// Get days remaining until next season change
    /// </summary>
    public int GetDaysUntilSeasonChange()
    {
        return daysUntilSeasonChange;
    }

    /// <summary>
    /// Force a season change (for debugging/testing)
    /// </summary>
    public void ForceSeasonChange()
    {
        ChangeSeason();
    }

    /// <summary>
    /// Set a specific season (for debugging/testing)
    /// </summary>
    public void SetSeason(Season season)
    {
        if (season != currentSeason)
        {
            currentSeason = season;
            ApplySeasonEffects(season);
            daysUntilSeasonChange = daysPerSeason;
        }
    }

    /// <summary>
    /// Check if currently transitioning between seasons
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// Get the current transition progress (0-1)
    /// </summary>
    public float GetTransitionProgress()
    {
        return transitionProgress;
    }

    /// <summary>
    /// Get the current solar year
    /// </summary>    
    public int GetCurrentSolarYear()
    {
        return currentSolarYear;
    }
}