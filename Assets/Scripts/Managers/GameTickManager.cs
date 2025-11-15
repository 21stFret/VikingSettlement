using UnityEngine;
using System;

/// <summary>
/// Centralized tick manager that synchronizes all game systems
/// All managers should subscribe to this instead of using Update()
/// </summary>
public class GameTickManager : MonoBehaviour
{
    public static GameTickManager Instance { get; private set; }

    [Header("Tick Settings")]
    [Tooltip("Tick rate for game simulation (ticks per second)")]
    [SerializeField] private float tickRate = 1f; // 1 tick per second by default

    [Tooltip("Enable to pause all game ticks")]
    [SerializeField] private bool isPaused = false;

    [Tooltip("Time scale multiplier for speeding up/slowing down game")]
    [SerializeField] private float timeScale = 1f;

    [Header("Status (Read-only)")]
    [SerializeField] private float tickTimer = 0f;
    [SerializeField] private int totalTicks = 0;
    [SerializeField] private float timeSinceLastTick = 0f;

    // Events for different systems to subscribe to
    public event Action<float> OnGameTick;      // Fires every game tick with deltaTime since last tick
    public event Action OnFastUpdate;           // Fires every frame for smooth updates (lighting, etc.)

    // Properties
    public float TickRate => tickRate;
    public float TimeScale => timeScale;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isPaused) return;

        float scaledDeltaTime = Time.deltaTime * timeScale;

        // Fire fast update every frame for smooth visual updates
        OnFastUpdate?.Invoke();

        // Accumulate time for game tick
        tickTimer += scaledDeltaTime;
        timeSinceLastTick += scaledDeltaTime;

        // Check if it's time for a game tick
        float tickInterval = 1f / tickRate;
        if (tickTimer >= tickInterval)
        {
            // Fire game tick event
            OnGameTick?.Invoke(timeSinceLastTick);
            
            totalTicks++;
            tickTimer -= tickInterval;
            timeSinceLastTick = 0f;
        }
    }

    /// <summary>
    /// Set the tick rate (ticks per second)
    /// </summary>
    public void SetTickRate(float newTickRate)
    {
        tickRate = Mathf.Max(0.1f, newTickRate);
    }

    /// <summary>
    /// Set the time scale (1 = normal, 2 = 2x speed, 0.5 = half speed)
    /// </summary>
    public void SetTimeScale(float newTimeScale)
    {
        timeScale = Mathf.Max(0f, newTimeScale);
    }

    /// <summary>
    /// Pause/unpause the game tick
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    /// <summary>
    /// Reset the tick counter
    /// </summary>
    public void ResetTicks()
    {
        totalTicks = 0;
        tickTimer = 0f;
        timeSinceLastTick = 0f;
    }
}
