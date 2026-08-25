using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages weather effects across all game scenes.
/// Persists between scenes and parents effects to the camera.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Effect Prefabs")]
    [Tooltip("Rain particle system prefab")]
    public GameObject rainPrefab;
    [Tooltip("Snow particle system prefab")]
    public GameObject snowPrefab;
    [Tooltip("Sun beams prefab (contains Light2D components)")]
    public GameObject sunBeamsPrefab;
    [Tooltip("Sun dust particles prefab")]
    public GameObject sunDustPrefab;
    [Tooltip("Fireflies particles prefab")]
    public GameObject firefliesPrefab;

    [Header("Lightning")]
    [Tooltip("Lightning light prefab")]
    public GameObject lightningPrefab;
    [Tooltip("Thunder audio clips")]
    public AudioClip[] thunderSounds;

    [Header("Scene Settings")]
    [Tooltip("Scenes where weather should NOT be active (e.g., Main Menu)")]
    public List<string> excludedScenes = new List<string> { "MainMenu", "Main Menu" };

    [Header("Sun Beams Settings")]
    [Tooltip("Speed at which sun beams drift in world space")]
    public float sunBeamDriftSpeed = 0.5f;
    [Tooltip("How far sun beams drift before resetting")]
    public float sunBeamDriftRange = 5f;
    [Tooltip("Base intensity of sun beams")]
    public float sunBeamIntensity = 0.8f;
    [Tooltip("Animation speed for sun beam flickering")]
    public float sunBeamAnimationSpeed = 2f;
    [Tooltip("Duration for sun beams to fade in/out")]
    public float sunBeamFadeDuration = 2f;
    [Tooltip("Time of day when sun beams start appearing (0-1, 0.5 = noon)")]
    [Range(0f, 0.5f)]
    public float sunBeamStartTime = 0.4f;
    [Tooltip("Time of day when sun beams stop appearing (0-1, 0.5 = noon)")]
    [Range(0.5f, 1f)]
    public float sunBeamEndTime = 0.6f;

    [Header("Current Weather State")]
    [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
    [SerializeField] private bool isStormActive = false;
    [SerializeField] private float weatherIntensity = 1f;

    [Header("Weather Duration (Auto Mode)")]
    [Tooltip("Minimum weather duration in days (0.5 = half day)")]
    public float minWeatherDuration = 0.5f;
    [Tooltip("Maximum weather duration in days (1.0 = full day)")]
    public float maxWeatherDuration = 1f;
    [SerializeField] private float currentWeatherDuration = 0f;

    [Header("Rain/Storm Effects")]
    [Tooltip("Sun intensity multiplier during rain/storm (0.5 = 50% darker)")]
    [Range(0f, 1f)]
    public float stormSunIntensityMultiplier = 0.5f;
    private float originalSunIntensity = 1f;
    private bool hasDimmedSun = false;

    [Header("Winter Snow")]
    [Tooltip("Snow emission rate on calm winter days (ambient snowfall)")]
    [SerializeField] private float ambientWinterSnowRate = 25f;
    [Tooltip("Snow emission rate during winter storms (blizzard)")]
    [SerializeField] private float stormSnowRate = 100f;

    [Header("Testing")]
    [Tooltip("Select weather type to test")]
    public WeatherType testWeatherType = WeatherType.Clear;
    [Tooltip("Intensity for rain/snow (emission rate)")]
    public float testIntensity = 50f;
    [InspectorButton("SetTestWeather", ButtonWidth = 150)]
    public bool applyWeatherButton;
    [InspectorButton("StopAllWeather", ButtonWidth = 150)]
    public bool stopWeatherButton;

    // Runtime instances (parented to camera)
    private GameObject rainInstance;
    private GameObject snowInstance;
    private GameObject sunBeamsInstance;
    private GameObject sunDustInstance;
    private GameObject firefliesInstance;
    private GameObject lightningInstance;

    private ParticleSystem rainParticles;
    private ParticleSystem snowParticles;
    private ParticleSystem sunDustParticles;
    private ParticleSystem firefliesParticles;
    private List<Light2D> sunBeamLights = new List<Light2D>();
    private LightningController lightningController;
    private AudioSource thunderAudioSource;

    private Camera mainCamera;
    private Transform weatherContainer;
    private Vector3 sunBeamStartPosition;
    private float sunBeamAnimationTime;
    private float sunBeamCurrentIntensity = 0f;
    private float sunBeamTargetIntensity = 0f;
    private bool sunBeamsFadingOut = false;
    private bool sunBeamsWereActive = false;

    private bool isInitialized = false;

    public enum WeatherType
    {
        Clear,      // No effects
        Sunny,      // Sun beams + sun dust
        Rain,       // Rain particles
        Snow,       // Snow particles
        Storm       // Rain + Lightning
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.OnDayNightChanged -= OnDayNightChanged;
                DayNightManager.Instance.OnNewDay -= OnWeatherNewDay;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if this scene should have weather
        if (excludedScenes.Contains(scene.name))
        {
            DisableAllEffects();
            isInitialized = false;
            return;
        }
    }

    public void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("WeatherManager: No main camera found in scene!");
        }

        SetupWeatherContainer();
        SpawnWeatherEffects();
        isInitialized = true;

        // Subscribe to day/night changes
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnDayNightChanged += OnDayNightChanged;
            DayNightManager.Instance.OnDawnEveningChanged += OnDawnEveningChanged;
            DayNightManager.Instance.OnNewDay += OnWeatherNewDay;
        }

        // Disabled — weather now driven by CalendarManager via ApplyWeatherForDay()
        // if (autoWeather)
        // {
        //     SetRandomWeather();
        // }

        // Apply correct weather immediately so it's right on scene load, not after the first day tick
        bool initIsStorm = StormScheduler.Instance != null && StormScheduler.Instance.GetCurrentDayWoodMultiplier() > 1f;
        bool initIsWinter = SeasonManager.Instance != null && SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
        int coldLevel = (int)CalendarManager.Instance?.GetCurrentDayData().coldDayType;
        ApplyWeatherForDay(initIsStorm, initIsWinter, coldLevel);
    }

    private void OnDayNightChanged(bool isDaytime)
    {
        /*
        // Sun effects should only show during the day
        if (currentWeather == WeatherType.Sunny)
        {
            EnableSunBeams(isDaytime);
            EnableSunDust(isDaytime);
        }

        // Fireflies only at night (when Clear weather)
        if (currentWeather == WeatherType.Clear)
        {
            EnableFireflies(!isDaytime);
        }
        */
    }

    private void OnDawnEveningChanged(bool isDawn)
    {
        // Sun beams/dust now handled by UpdateSunBeamTiming based on time window

        // Fireflies only at night (when Clear weather)
        if (currentWeather == WeatherType.Clear && SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Summer)
        {
            EnableFireflies(!isDawn);
        }
    }

    private void SetupWeatherContainer()
    {
        // Create a container that follows the camera
        if (weatherContainer == null)
        {
            GameObject container = new GameObject("WeatherEffects");
            container.transform.SetParent(transform);
            weatherContainer = container.transform;
        }

        // Parent to camera
        weatherContainer.SetParent(mainCamera.transform);
        weatherContainer.localPosition = Vector3.zero;
        weatherContainer.localRotation = Quaternion.identity;
    }

    private void SpawnWeatherEffects()
    {
        // Clean up old instances
        CleanupInstances();

        // Spawn rain
        if (rainPrefab != null)
        {
            rainInstance = Instantiate(rainPrefab, weatherContainer);
            rainInstance.name = "Rain";
            rainInstance.transform.localPosition = new Vector3(0, 12.5f, 10); // Above camera
            rainParticles = rainInstance.GetComponent<ParticleSystem>();
            rainInstance.SetActive(false);
        }

        // Spawn snow
        if (snowPrefab != null)
        {
            snowInstance = Instantiate(snowPrefab, weatherContainer);
            snowInstance.name = "Snow";
            snowInstance.transform.localPosition = new Vector3(0, 12.5f, 10); // Above camera
            snowParticles = snowInstance.GetComponent<ParticleSystem>();
            snowInstance.SetActive(false);
        }

        // Spawn sun dust
        if (sunDustPrefab != null)
        {
            sunDustInstance = Instantiate(sunDustPrefab, weatherContainer);
            sunDustInstance.name = "SunDust";
            sunDustInstance.transform.localPosition = new Vector3(0, 0, 10);
            sunDustParticles = sunDustInstance.GetComponent<ParticleSystem>();
            sunDustInstance.SetActive(false);
        }

        // Spawn fireflies
        if (firefliesPrefab != null)
        {
            firefliesInstance = Instantiate(firefliesPrefab, weatherContainer);
            firefliesInstance.name = "Fireflies";
            firefliesInstance.transform.localPosition = new Vector3(0, 0, 10);
            firefliesParticles = firefliesInstance.GetComponent<ParticleSystem>();
            firefliesInstance.SetActive(false);
        }

        // Spawn sun beams (these drift in world space, so parent differently)
        if (sunBeamsPrefab != null)
        {
            sunBeamsInstance = Instantiate(sunBeamsPrefab, transform); // Parent to WeatherManager, not camera
            sunBeamsInstance.name = "SunBeams";
            sunBeamLights.Clear();
            sunBeamLights.AddRange(sunBeamsInstance.GetComponentsInChildren<Light2D>());
            sunBeamStartPosition = sunBeamsInstance.transform.position;
            sunBeamsInstance.SetActive(false);
        }

        // Spawn lightning
        if (lightningPrefab != null)
        {
            lightningInstance = Instantiate(lightningPrefab, weatherContainer);
            lightningInstance.name = "Lightning";
            lightningController = lightningInstance.GetComponent<LightningController>();

            // Setup audio source for thunder
            thunderAudioSource = lightningInstance.GetComponent<AudioSource>();
            if (thunderAudioSource == null)
            {
                thunderAudioSource = lightningInstance.AddComponent<AudioSource>();
            }

            lightningInstance.SetActive(false);
        }
    }

    private void CleanupInstances()
    {
        if (rainInstance != null) Destroy(rainInstance);
        if (snowInstance != null) Destroy(snowInstance);
        if (sunBeamsInstance != null) Destroy(sunBeamsInstance);
        if (sunDustInstance != null) Destroy(sunDustInstance);
        if (firefliesInstance != null) Destroy(firefliesInstance);
        if (lightningInstance != null) Destroy(lightningInstance);

        sunBeamLights.Clear();
    }

    private void Update()
    {
        if (!isInitialized || mainCamera == null) return;

        UpdateSunBeamTiming();
        UpdateSunBeams();
        UpdateSunDimming();
    }

    private void UpdateSunBeamTiming()
    {
        // Only manage sun beams automatically for Sunny weather
        if (currentWeather != WeatherType.Sunny) return;
        if (DayNightManager.Instance == null) return;

        float timeOfDay = DayNightManager.Instance.GetTimeOfDay();
        bool shouldBeActive = timeOfDay >= sunBeamStartTime && timeOfDay <= sunBeamEndTime;

        // Check for state change
        if (shouldBeActive != sunBeamsWereActive)
        {
            sunBeamsWereActive = shouldBeActive;
            EnableSunBeams(shouldBeActive);
            EnableSunDust(shouldBeActive);
        }
    }

    private void UpdateSunDimming()
    {
        if (DayNightManager.Instance == null || DayNightManager.Instance.sunLight == null) return;

        bool shouldDim = currentWeather == WeatherType.Rain || currentWeather == WeatherType.Storm;

        if (shouldDim && !hasDimmedSun)
        {
            // Store original and dim the sun
            originalSunIntensity = DayNightManager.Instance.sunIntensity;
            DayNightManager.Instance.sunIntensity = originalSunIntensity * stormSunIntensityMultiplier;
            hasDimmedSun = true;
        }
        else if (!shouldDim && hasDimmedSun)
        {
            // Restore sun intensity
            DayNightManager.Instance.sunIntensity = originalSunIntensity;
            hasDimmedSun = false;
        }
    }

    private void UpdateSunBeams()
    {
        if (sunBeamsInstance == null) return;

        // Handle fade in/out
        float fadeSpeed = sunBeamFadeDuration > 0f ? (1f / sunBeamFadeDuration) : 10f;
        sunBeamCurrentIntensity = Mathf.MoveTowards(sunBeamCurrentIntensity, sunBeamTargetIntensity, fadeSpeed * Time.deltaTime);

        // If fading out and reached zero, deactivate
        if (sunBeamsFadingOut && sunBeamCurrentIntensity <= 0f)
        {
            sunBeamsInstance.SetActive(false);
            sunBeamsFadingOut = false;
            return;
        }

        // Don't update if not active
        if (!sunBeamsInstance.activeInHierarchy) return;

        // Drift sun beams slowly in world space
        Vector3 currentPos = sunBeamsInstance.transform.position;
        currentPos.x += sunBeamDriftSpeed * Time.deltaTime;

        // Check if we've drifted too far from camera
        float distanceFromCamera = currentPos.x - mainCamera.transform.position.x;
        if (Mathf.Abs(distanceFromCamera) > sunBeamDriftRange)
        {
            // Reset position to other side
            currentPos.x = mainCamera.transform.position.x - sunBeamDriftRange * Mathf.Sign(distanceFromCamera);
        }

        sunBeamsInstance.transform.position = currentPos;

        // Animate sun beam intensity with fade multiplier
        sunBeamAnimationTime += Time.deltaTime * sunBeamAnimationSpeed;
        for (int i = 0; i < sunBeamLights.Count; i++)
        {
            if (sunBeamLights[i] == null) continue;

            float offset = i * 0.5f;
            float animatedIntensity = sunBeamIntensity * (0.7f + 0.3f * Mathf.Sin(sunBeamAnimationTime + offset));
            sunBeamLights[i].intensity = animatedIntensity * weatherIntensity * sunBeamCurrentIntensity;
        }
    }

    private void ApplyWeatherState()
    {
        DisableAllEffects();

        bool isDaytime = DayNightManager.Instance != null && DayNightManager.Instance.IsDaytime();

        switch (currentWeather)
        {
            case WeatherType.Clear:
                // Fireflies only at night during clear weather
                if (SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Summer)
                {
                    EnableFireflies(!isDaytime);
                }
                break;

            case WeatherType.Sunny:
                // Sun effects based on time window around midday
                float timeOfDay = DayNightManager.Instance != null ? DayNightManager.Instance.GetTimeOfDay() : 0.5f;
                bool inSunBeamWindow = timeOfDay >= sunBeamStartTime && timeOfDay <= sunBeamEndTime;
                sunBeamsWereActive = inSunBeamWindow;
                EnableSunBeams(inSunBeamWindow);
                EnableSunDust(inSunBeamWindow);
                break;

            case WeatherType.Rain:
                EnableRain(true);
                break;

            case WeatherType.Snow:
                EnableSnow(true);
                break;

            case WeatherType.Storm:
                EnableRain(true);
                EnableLightning(true);
                break;
        }
    }

    private void DisableAllEffects()
    {
        EnableRain(false);
        EnableSnow(false);
        EnableLightning(false);
        EnableSunBeams(false);
        EnableSunDust(false);
        EnableFireflies(false);
    }

    /// <summary>
    /// Helper method to enable/disable particle effects with gradual fade out
    /// </summary>
    private void SetParticleEffectActive(GameObject instance, ParticleSystem particles, bool enabled)
    {
        if (instance == null || particles == null) return;

        if (enabled)
        {
            instance.SetActive(true);
            // isPlaying stays true during StopEmitting drain, so also check isEmitting
            // to catch the case where DisableAllEffects() and re-enable happen in the same call.
            if (particles.isStopped || !particles.isEmitting)
                particles.Play();
        }
        else
        {
            // Stop emitting but let existing particles fade out
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // ── Calendar-driven weather ───────────────────────────────────────────────

    private void OnWeatherNewDay()
    {
        bool isStorm = StormScheduler.Instance != null && StormScheduler.Instance.GetCurrentDayWoodMultiplier() > 1f;
        bool isWinter = SeasonManager.Instance != null && SeasonManager.Instance.GetCurrentSeason() == SeasonManager.Season.Winter;
        int coldLevel = (int)CalendarManager.Instance?.GetCurrentDayData().coldDayType;
        ApplyWeatherForDay(isStorm, isWinter, coldLevel);
    }

    /// <summary>
    /// Sets weather for the day based on storm/season state.
    /// Called by OnWeatherNewDay and during Initialize() for correct scene-load state.
    /// CalendarManager and StormScheduler are the source of truth; this method only executes.
    /// </summary>
    public void ApplyWeatherForDay(bool isStormDay, bool isWinter, int coldLevel)
    {
        if (isStormDay)
        {
            SetWeather(WeatherType.Storm);
            return;
        }

        if (isWinter)
        {
            if (coldLevel > 0)
            {
                SetSnowIntensity(coldLevel > 1 ? stormSnowRate : ambientWinterSnowRate);
                SetWeather(WeatherType.Snow);
                return;
            }
        }
        else
        {
            if (coldLevel < 0)
            {
                SetWeather(WeatherType.Sunny);  // sun beams + fireflies
                return;
            }

        }
        SetWeather(WeatherType.Clear);

    }

    #region Public API

    /// <summary>
    /// Set the current weather type
    /// </summary>
    public void SetWeather(WeatherType weather, float intensity = 1f)
    {
        currentWeather = weather;
        weatherIntensity = Mathf.Clamp01(intensity);

        if (isInitialized)
        {
            ApplyWeatherState();
        }
    }

    public void SetTestWeather()
    {
        currentWeather = testWeatherType;
        weatherIntensity = Mathf.Clamp01(testIntensity);

        if (isInitialized)
        {
            ApplyWeatherState();
        }
    }
    /// <summary>
    /// Get the current weather type
    /// </summary>
    public WeatherType GetCurrentWeather()
    {
        return currentWeather;
    }

    /// <summary>
    /// Enable/disable rain effect
    /// </summary>
    public void EnableRain(bool enabled)
    {
        SetParticleEffectActive(rainInstance, rainParticles, enabled);
    }

    /// <summary>
    /// Enable/disable snow effect
    /// </summary>
    public void EnableSnow(bool enabled)
    {
        SetParticleEffectActive(snowInstance, snowParticles, enabled);
    }

    /// <summary>
    /// Set snow emission rate
    /// </summary>
    public void SetSnowIntensity(float emissionRate)
    {
        if (snowParticles != null)
        {
            var emission = snowParticles.emission;
            emission.rateOverTime = emissionRate;
        }
    }

    /// <summary>
    /// Set rain emission rate
    /// </summary>
    public void SetRainIntensity(float emissionRate)
    {
        if (rainParticles != null)
        {
            var emission = rainParticles.emission;
            emission.rateOverTime = emissionRate;
        }
    }

    /// <summary>
    /// Enable/disable lightning/storm effect
    /// </summary>
    public void EnableLightning(bool enabled)
    {
        isStormActive = enabled;
        if (lightningInstance != null)
        {
            lightningInstance.SetActive(enabled);
            if (lightningController != null)
            {
                lightningController.active = enabled;
            }
        }
    }

    /// <summary>
    /// Enable/disable sun beams
    /// </summary>
    public void EnableSunBeams(bool enabled)
    {
        if (sunBeamsInstance == null) return;

        if (enabled)
        {
            // Fade in
            sunBeamTargetIntensity = 1f;
            sunBeamsFadingOut = false;

            if (!sunBeamsInstance.activeInHierarchy)
            {
                // Starting fresh - reset position and activate
                sunBeamsInstance.transform.position = sunBeamStartPosition;
                sunBeamsInstance.SetActive(true);
            }
        }
        else
        {
            // Fade out (don't deactivate yet - UpdateSunBeams will do it when fade completes)
            sunBeamTargetIntensity = 0f;
            sunBeamsFadingOut = true;
        }
    }

    /// <summary>
    /// Enable/disable sun dust particles
    /// </summary>
    public void EnableSunDust(bool enabled)
    {
        SetParticleEffectActive(sunDustInstance, sunDustParticles, enabled);
    }

    /// <summary>
    /// Enable/disable fireflies
    /// </summary>
    public void EnableFireflies(bool enabled)
    {
        SetParticleEffectActive(firefliesInstance, firefliesParticles, enabled);
    }

    /// <summary>
    /// Enable summer daytime effects (sun beams + sun dust)
    /// </summary>
    public void EnableSummerDayEffects(bool enabled)
    {
        EnableSunBeams(enabled);
        EnableSunDust(enabled);
        if (enabled)
        {
            EnableFireflies(false);
        }
    }

    /// <summary>
    /// Enable summer nighttime effects (fireflies)
    /// </summary>
    public void EnableSummerNightEffects(bool enabled)
    {
        EnableFireflies(enabled);
        if (enabled)
        {
            EnableSunBeams(false);
            EnableSunDust(false);
        }
    }

    /// <summary>
    /// Check if a storm is currently active
    /// </summary>
    public bool IsStormActive()
    {
        return isStormActive;
    }

    /// <summary>
    /// Enable manual weather control (for cutscenes). Weather won't change until ResumeAutoWeather is called.
    /// </summary>
    public void SetManualWeather(WeatherType weather, float intensity = 1f)
    {
        SetWeather(weather, intensity);
    }

    /// <summary>
    /// Set the intensity of weather effects (0-1)
    /// </summary>
    public void SetWeatherIntensity(float intensity)
    {
        weatherIntensity = Mathf.Clamp01(intensity);
    }

    /// <summary>
    /// Get the rain particle system for external control
    /// </summary>
    public ParticleSystem GetRainParticles()
    {
        return rainParticles;
    }

    /// <summary>
    /// Get the snow particle system for external control
    /// </summary>
    public ParticleSystem GetSnowParticles()
    {
        return snowParticles;
    }

    #endregion
}
