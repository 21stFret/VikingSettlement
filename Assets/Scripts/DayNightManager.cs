using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using Unity.VisualScripting;

/// <summary>
/// Manages the day/night cycle and triggers daily events like meal time
/// </summary>
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Light References")]
    [Tooltip("Spotlight that acts as the sun, moves across the sky and casts shadows")]
    public Light2D sunLight;

    [Tooltip("Global light for ambient lighting at night")]
    public Light2D ambientLight;

    public GameObject dayNightDial;

    [Header("Day/Night Settings")]
    [Tooltip("Length of a full day in seconds (real time)")]
    public float dayLengthInSeconds = 120f; // 2 minutes per day by default
    public bool clockwise = true;

    [Tooltip("Time of day when meals are consumed (0-1, where 0.5 is noon)")]
    [Range(0f, 1f)]
    public float mealTime = 0.5f; // Noon by default

    [Header("Sun Light Settings")]
    [Tooltip("Sun light intensity at noon")]
    public float sunIntensity = 1f;

    [Tooltip("Sun light color at noon")]
    public Color sunColor = new Color(1f, 0.95f, 0.9f);

    [Tooltip("Sun light color at dawn/dusk")]
    public Color dawnDuskColor = new Color(1f, 0.7f, 0.5f);

    [Header("Ambient Light Settings")]
    [Tooltip("Ambient light intensity during day")]
    public float ambientDayIntensity = 0.4f;

    [Tooltip("Ambient light intensity during night")]
    public float ambientNightIntensity = 0.15f;

    [Tooltip("Ambient light color during day")]
    public Color ambientDayColor = new Color(0.8f, 0.9f, 1f);

    [Tooltip("Ambient light color during night")]
    public Color ambientNightColor = new Color(0.2f, 0.3f, 0.5f);

    [Header("Sun Movement")]
    [Tooltip("Radius of the sun's arc across the sky")]
    public float sunArcRadius = 20f;

    [Tooltip("X position offset for the sun's arc center")]
    public float sunArcCenterX = 0f;

    [Tooltip("X position offset for the sun's start point on the circle")]
    public float sunArcOffsetX = 0f;

    [Tooltip("Y position offset for the sun's arc center")]
    public float sunArcCenterY = 0f;

    public float sunHeightTarget = 10f;

    [Header("Sunrise/Sunset Times")]
    [Tooltip("Time of day when the sun rises (0-1, where 0 is midnight and 1 is the next midnight)")]
    public float sunriseTime = 0.25f; // Sunrise at 6 AM

    [Tooltip("Time of day when the sun sets (0-1, where 0 is midnight and 1 is the next midnight)")]
    public float sunsetTime = 0.75f;  // Sunset at 6 PM

    [Header("Status (Read-only)")]
    [SerializeField] private float currentTimeOfDay = 0f; // 0 to 1
    [SerializeField] private int currentDay = 1;
    [SerializeField] private bool hasConsumedMealToday = false;

    // Events
    public event Action OnMealTime;
    public event Action OnNewDay;

    public float eveningMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Try to find the lights if not assigned
        if (sunLight == null)
        {
            GameObject sunObj = GameObject.Find("Sun Light");
            if (sunObj != null)
            {
                sunLight = sunObj.GetComponent<Light2D>();
            }
        }

        if (ambientLight == null)
        {
            GameObject ambientObj = GameObject.Find("Ambient Light");
            if (ambientObj == null)
            {
                ambientObj = GameObject.Find("Global Light 2D");
            }
            if (ambientObj != null)
            {
                ambientLight = ambientObj.GetComponent<Light2D>();
            }
        }
    }

    private void Start()
    {
        if (sunLight == null)
        {
            Debug.LogWarning("DayNightManager: No Sun Light assigned or found!");
        }
        if (ambientLight == null)
        {
            Debug.LogWarning("DayNightManager: No Ambient Light assigned or found!");
        }
    }

    private void Update()
    {
        // Advance time
        float timeIncrement = Time.deltaTime / dayLengthInSeconds;
        currentTimeOfDay += timeIncrement;

        if (dayNightDial != null)
        {
            float dialRotation = currentTimeOfDay * 360f;
            dayNightDial.transform.rotation = Quaternion.Euler(0f, 0f, -dialRotation);
        }

        // Check if we've passed meal time
        if (!hasConsumedMealToday && currentTimeOfDay >= mealTime)
        {
            TriggerMealTime();
            hasConsumedMealToday = true;
        }

        // Handle day rollover
        if (currentTimeOfDay >= 1f)
        {
            currentTimeOfDay = 0f;
            currentDay++;
            hasConsumedMealToday = false;
            OnNewDay?.Invoke();
            Debug.Log($"Day {currentDay} has begun!");
        }

        // Update lighting
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        // Calculate if sun is above horizon (between sunrise and sunset)
        bool isSunUp = currentTimeOfDay >= sunriseTime && currentTimeOfDay <= sunsetTime;

        // Update sun light
        if (sunLight != null)
        {
            if (isSunUp)
            {
                // Sun is up - enable and update
                RiseSetSun(true);
                UpdateSunLight();
            }
            else
            {
                // Sun is down - disable
                RiseSetSun(false);                
            }
        }

        // Update ambient light
        if (ambientLight != null)
        {
            UpdateAmbientLight(currentTimeOfDay);
        }
    }

    private void RiseSetSun(bool rise)
    {
        if(rise)
        {
            sunLight.intensity += 0.1f;
            if (sunLight.intensity > sunIntensity)
            {
                sunLight.intensity = sunIntensity;
            }
        }
        else
        {
            sunLight.intensity -= 0.1f;
            if (sunLight.intensity < 0f)
            {
                sunLight.intensity = 0f;
            }
        }
    }

    private void UpdateSunLight()
    {
        // Calculate sun position on its arc (from sunrise to sunset)
        float dayProgress = (currentTimeOfDay - sunriseTime) / (sunsetTime - sunriseTime);

        // Sun moves from left (sunrise) to right (sunset) in an arc
        // Angle goes from 180 degrees (left) to 0 degrees (right)
        float angle = Mathf.Lerp(-180f, 0f, dayProgress);
        float angleRad = angle * Mathf.Deg2Rad;

        float xValue = sunArcCenterX + Mathf.Cos(angleRad) * sunArcRadius;
        float zValue = Mathf.Abs(Mathf.Sin(angleRad) * sunHeightTarget);
        float yValue = sunArcCenterY + Mathf.Sin(angleRad) * sunArcRadius;


        if(!clockwise)
        {
            xValue = sunArcCenterX - Mathf.Cos(angleRad) * sunArcRadius;
            zValue = Mathf.Sin(angleRad) * sunHeightTarget;
            yValue = sunArcCenterY + Mathf.Sin(angleRad) * sunArcRadius;
        }
        // Calculate sun position
        Vector3 sunPosition = new Vector3(
            sunArcCenterX + xValue,
            sunArcCenterY + yValue,
            zValue
        );
        
        sunLight.transform.position = sunPosition;

        // Calculate sun intensity (brightest at noon, dimmer at dawn/dusk)
        float intensityCurve = Mathf.Sin(dayProgress * Mathf.PI);
        sunLight.intensity = sunIntensity * intensityCurve;

        // Calculate sun color (more orange/red at dawn/dusk, yellow/white at noon)
        float colorBlend = Mathf.Pow(intensityCurve, 0.5f); // Less aggressive transition
        sunLight.color = Color.Lerp(dawnDuskColor, sunColor, colorBlend);
    }

    private void UpdateAmbientLight(float timeOfDay)
    {
        if (timeOfDay < 0.5f)
        {
            ambientLight.intensity = Mathf.Lerp(ambientNightIntensity, ambientDayIntensity,
            Mathf.Clamp01(timeOfDay)+eveningMultiplier);
            ambientLight.color = Color.Lerp(ambientNightColor, ambientDayColor,
            Mathf.Clamp01(timeOfDay )+eveningMultiplier);
        }
        else
        {
            ambientLight.intensity = Mathf.Lerp(ambientDayIntensity, ambientNightIntensity,
            Mathf.Clamp01((timeOfDay - 0.5f) )+eveningMultiplier);
            ambientLight.color = Color.Lerp(ambientDayColor, ambientNightColor,
            Mathf.Clamp01((timeOfDay - 0.5f))+eveningMultiplier);  
        }
     
    }

    private void TriggerMealTime()
    {
        Debug.Log($"Meal time on Day {currentDay}!");

        // Trigger event for SettlementManager and other systems
        OnMealTime?.Invoke();
    }

    /// <summary>
    /// Get the current time of day (0-1)
    /// </summary>
    public float GetTimeOfDay()
    {
        return currentTimeOfDay;
    }

    /// <summary>
    /// Get the current day number
    /// </summary>
    public int GetCurrentDay()
    {
        return currentDay;
    }

    /// <summary>
    /// Get a formatted time string (e.g., "8:30 AM")
    /// </summary>
    public string GetFormattedTime()
    {
        int hours = Mathf.FloorToInt(currentTimeOfDay * 24f);
        int minutes = Mathf.FloorToInt((currentTimeOfDay * 24f - hours) * 60f);
        string period = hours >= 12 ? "PM" : "AM";
        int displayHours = hours % 12;
        if (displayHours == 0) displayHours = 12;

        return $"{displayHours}:{minutes:D2} {period}";
    }

    /// <summary>
    /// Set time of day directly (for debugging/testing)
    /// </summary>
    public void SetTimeOfDay(float time)
    {
        currentTimeOfDay = Mathf.Clamp01(time);
        hasConsumedMealToday = currentTimeOfDay > mealTime;
    }
}
