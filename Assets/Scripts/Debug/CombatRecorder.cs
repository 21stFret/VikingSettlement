using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Runtime combat screen recorder for playtesting.
///
/// Auto-starts when the scene loads, captures frames as compressed JPEG byte arrays,
/// and stops automatically when all enemies are dead.
///
/// Quick setup:
///   1. Add an empty GameObject to your combat scene.
///   2. Attach CombatRecorder to it.
///   3. Tune inspector fields if desired (captureFps, resolution, quality).
///   4. Hit Play — recording begins immediately.
/// </summary>
public class CombatRecorder : MonoBehaviour
{
    public static CombatRecorder Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Capture Settings")]
    [Tooltip("Frames captured per second (wall-clock time).")]
    [SerializeField] private int captureFps = 10;
    [SerializeField] private int captureWidth = 640;
    [SerializeField] private int captureHeight = 360;
    [Range(1, 100)]
    [SerializeField] private int jpegQuality = 60;

    [Header("Capture Camera")]
    [Tooltip("Child Camera of the Main Camera. The recorder sets its targetTexture and calls Render() manually — leave it disabled in the Inspector.")]
    [SerializeField] private Camera captureCamera;

    [Header("Combat-End Detection")]
    [Tooltip("Seconds to wait for enemies to appear before giving up.")]
    [SerializeField] private float enemyWaitTimeout = 30f;
    [Tooltip("Seconds between each 'are enemies dead yet?' check.")]
    [SerializeField] private float pollInterval = 0.5f;

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>True once <see cref="StopRecording"/> has been called and the frame list is ready.</summary>
    public bool HasRecording { get; private set; }
    public int FrameCount => _frames.Count;
    public int CaptureFps => captureFps;

    /// <summary>Fired on the frame recording is stopped (all enemies dead or manual stop).</summary>
    public event Action OnRecordingStopped;

    // ── Private state ──────────────────────────────────────────────────────────

    private readonly List<byte[]> _frames = new List<byte[]>();
    private Coroutine _captureCoroutine;
    private Coroutine _watchCoroutine;

    // Reused GPU/CPU texture resources — allocated once, kept for the session.
    private RenderTexture _renderTex;
    private Texture2D _readbackTex;
    // Reused decode texture — resized automatically by LoadImage.
    private Texture2D _decodeTex;

    private bool _isRecording;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        AllocateTextures();
        StartRecording();
        _watchCoroutine = StartCoroutine(WatchForCombatEnd());
    }

    private void OnDestroy()
    {
        if (captureCamera != null) captureCamera.targetTexture = null;
        if (_renderTex != null) _renderTex.Release();
        // Texture2D objects will be GC'd; explicit Destroy keeps memory tidy.
        if (_readbackTex != null) Destroy(_readbackTex);
        if (_decodeTex   != null) Destroy(_decodeTex);
        if (Instance == this)    Instance = null;
    }

    // ── Setup ──────────────────────────────────────────────────────────────────

    private void AllocateTextures()
    {
        // RenderTextureReadWrite.sRGB ensures Graphics.Blit round-trips the screenshot
        // through sRGB→linear→sRGB, preserving the original display colours.
        // Without this, URP's linear pipeline leaves linear data in the RT which
        // reads back as washed-out/overexposed when displayed via RawImage.
        _renderTex   = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        _readbackTex = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
        _decodeTex   = new Texture2D(2, 2, TextureFormat.RGB24, false); // LoadImage resizes automatically

        if (captureCamera != null)
        {
            captureCamera.targetTexture = _renderTex;
            captureCamera.enabled = false; // driven manually via Render()
        }
        else
        {
            Debug.LogError("[CombatRecorder] captureCamera not assigned — recording disabled.");
        }
    }

    // ── Recording control ──────────────────────────────────────────────────────

    public void StartRecording()
    {
        if (_isRecording) return;
        if (captureCamera == null) return;
        _isRecording = true;
        _frames.Clear();
        HasRecording = false;
        _captureCoroutine = StartCoroutine(CaptureLoop());
        Debug.Log("[CombatRecorder] Recording started.");
    }

    public void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;

        if (_captureCoroutine != null)
        {
            StopCoroutine(_captureCoroutine);
            _captureCoroutine = null;
        }

        HasRecording = true;
        Debug.Log($"[CombatRecorder] Recording stopped. {_frames.Count} frames captured.");
        OnRecordingStopped?.Invoke();
    }

    // ── Capture loop ───────────────────────────────────────────────────────────

    /// <summary>
    /// Captures one frame per interval at real wall-clock rate regardless of
    /// <c>Time.timeScale</c>.
    /// </summary>
    private IEnumerator CaptureLoop()
    {
        float interval = 1f / Mathf.Max(captureFps, 1);
        float nextCaptureTime = Time.realtimeSinceStartup;

        while (_isRecording)
        {
            // Wait for end of frame before capturing (Unity requirement).
            yield return new WaitForEndOfFrame();

            if (Time.realtimeSinceStartup < nextCaptureTime)
                continue;

            // Reset from now — prevents catch-up bursts after slow frames (e.g. scene init).
            nextCaptureTime = Time.realtimeSinceStartup + interval;

            // Render scene to _renderTex at capture resolution (GPU-side, no CPU stall).
            captureCamera.Render();

            // Queue async readback — callback fires on the main thread 1-2 frames later.
            AsyncGPUReadback.Request(_renderTex, 0, TextureFormat.RGBA32, OnReadbackComplete);
        }
    }

    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        if (request.hasError) return;
        NativeArray<byte> data = request.GetData<byte>();
        _readbackTex.LoadRawTextureData(data);
        _readbackTex.Apply(false);
        _frames.Add(_readbackTex.EncodeToJPG(jpegQuality));
    }

    // ── Combat-end watcher ─────────────────────────────────────────────────────

    /// <summary>
    /// Phase 1: waits up to <see cref="enemyWaitTimeout"/> seconds for any enemy to appear.
    /// Phase 2: waits until every enemy reports <c>IsDead() == true</c>.
    /// Then calls <see cref="StopRecording"/>.
    /// </summary>
    private IEnumerator WatchForCombatEnd()
    {
        // ── Phase 1: wait for enemies to exist ──
        float elapsed = 0f;
        Enemy[] enemies;

        while (true)
        {
            enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            if (enemies.Length > 0) break;

            elapsed += pollInterval;
            if (elapsed >= enemyWaitTimeout)
            {
                Debug.LogWarning("[CombatRecorder] No enemies found within timeout. Recording will continue until manually stopped.");
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }

        Debug.Log($"[CombatRecorder] Detected {enemies.Length} enemies. Watching for combat end.");

        // ── Phase 2: wait until all enemies are dead ──
        while (true)
        {
            yield return new WaitForSeconds(pollInterval);

            // Refresh list each poll — new enemies may have spawned.
            enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            if (enemies.Length == 0)
                break; // All destroyed (shouldn't normally happen, but safe)

            bool allDead = true;
            foreach (var e in enemies)
            {
                if (e != null && !e.IsDead())
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead) break;
        }

        yield return new WaitForSeconds(1f); // Optional buffer to capture post-death animations/effects

        StopRecording();
    }

    // ── Save to disk ───────────────────────────────────────────────────────────

    /// <summary>
    /// Writes each captured frame as a JPEG into a timestamped folder under
    /// <see cref="Application.persistentDataPath"/>. Logs the folder path on completion.
    /// </summary>
    public void SaveRecording()
    {
        if (!HasRecording || _frames.Count == 0)
        {
            Debug.LogWarning("[CombatRecorder] Nothing to save.");
            return;
        }

        string folder = Path.Combine(
            Application.persistentDataPath,
            $"CombatRecording_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
        Directory.CreateDirectory(folder);

        for (int i = 0; i < _frames.Count; i++)
            File.WriteAllBytes(Path.Combine(folder, $"frame_{i:D4}.jpg"), _frames[i]);

        Debug.Log($"[CombatRecorder] Saved {_frames.Count} frames to:\n{folder}");
    }

    // ── Frame access ───────────────────────────────────────────────────────────

    /// <summary>
    /// Decodes the JPEG at <paramref name="index"/> into a reused <see cref="Texture2D"/>.
    /// The returned reference is valid until the next call to this method.
    /// </summary>
    public Texture2D GetFrameTexture(int index)
    {
        if (index < 0 || index >= _frames.Count)
            return null;

        _decodeTex.LoadImage(_frames[index]); // Resizes automatically; no new allocation.
        return _decodeTex;
    }
}
