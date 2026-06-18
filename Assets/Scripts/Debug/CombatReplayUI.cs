using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replay UI panel for <see cref="CombatRecorder"/>.
///
/// Wire up the serialized fields in the Inspector, then hit Play.
/// If <see cref="openButton"/> is left unassigned, a fallback "Watch Replay" button
/// is drawn in the top-right corner via OnGUI once a recording is ready.
///
/// Quick setup:
///   1. Attach this component to any active GameObject (Canvas root works well).
///   2. Assign panel, videoDisplay, playPauseButton, timelineSlider.
///   3. Optionally assign timeLabel and openButton.
///   4. Leave openButton unassigned to use the OnGUI fallback.
/// </summary>
public class CombatReplayUI : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("Root GameObject of the entire replay panel. Hidden at start.")]
    [SerializeField] private GameObject panel;

    [Header("Playback Controls")]
    [Tooltip("RawImage used to display decoded frames.")]
    [SerializeField] private RawImage videoDisplay;
    [Tooltip("Button that toggles play/pause. Must have a TMP_Text child for the label.")]
    [SerializeField] private Button playPauseButton;
    [Tooltip("Slider used as a timeline scrubber.")]
    [SerializeField] private Slider timelineSlider;

    [Header("Optional")]
    [Tooltip("Shows current frame / total frames. Leave unassigned to skip.")]
    [SerializeField] private TMP_Text timeLabel;
    [Tooltip("Button that opens the replay panel. Leave unassigned to use the OnGUI fallback.")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Playback Speed")]
    [Tooltip("1.0 = normal speed; 0.5 = half speed; 2.0 = double speed.")]
    [SerializeField] private float playbackSpeedMultiplier = 1f;

    // ── Private state ──────────────────────────────────────────────────────────

    private CombatRecorder _recorder;
    private TMP_Text _playPauseLabel;
    private Coroutine _subscribeCoroutine;

    private bool _isPlaying;
    private int _currentFrame;
    private bool _recordingReady;

    private float _playbackStartRealtime;
    private int   _playbackStartFrame;

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        // Cache TMP_Text child of play/pause button.
        if (playPauseButton != null)
            _playPauseLabel = playPauseButton.GetComponentInChildren<TMP_Text>();

        // Wire up buttons.
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(OnPlayPauseClicked);

        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        if(closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // Wire up slider (dragging pauses and seeks).
        if (timelineSlider != null)
            timelineSlider.onValueChanged.AddListener(OnSliderDragged);

        // Try to subscribe to the recorder; retry if not ready yet.
        if (CombatRecorder.Instance != null)
            SubscribeToRecorder();
        else
            _subscribeCoroutine = StartCoroutine(DeferredSubscribe());
    }

    private void OnDestroy()
    {
        if (_recorder != null)
            _recorder.OnRecordingStopped -= OnRecordingStopped;

        if (playPauseButton != null)
            playPauseButton.onClick.RemoveListener(OnPlayPauseClicked);

        if (openButton != null)
            openButton.onClick.RemoveListener(OpenPanel);

        if (timelineSlider != null)
            timelineSlider.onValueChanged.RemoveListener(OnSliderDragged);
    }

    // ── Subscription ───────────────────────────────────────────────────────────

    private void SubscribeToRecorder()
    {
        _recorder = CombatRecorder.Instance;
        _recorder.OnRecordingStopped += OnRecordingStopped;

        // If the recording already finished before we subscribed (unlikely but safe).
        if (_recorder.HasRecording)
            OnRecordingStopped();
    }

    /// <summary>
    /// Retries for up to 2 seconds if <see cref="CombatRecorder.Instance"/> is null at Start
    /// (script execution order edge case).
    /// </summary>
    private IEnumerator DeferredSubscribe()
    {
        float waited = 0f;
        while (waited < 2f)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            waited += 0.1f;

            if (CombatRecorder.Instance != null)
            {
                SubscribeToRecorder();
                yield break;
            }
        }

        Debug.LogWarning("[CombatReplayUI] CombatRecorder not found after 2 seconds. Replay unavailable.");
    }

    // ── Recording-ready handler ────────────────────────────────────────────────

    private void OnRecordingStopped()
    {
        _recordingReady = true;
        _currentFrame = 0;

        if (timelineSlider != null)
        {
            timelineSlider.minValue = 0;
            timelineSlider.maxValue = Mathf.Max(_recorder.FrameCount - 1, 0);
            SetSliderSilently(0);
        }

        UpdateTimeLabel();
        UpdatePlayPauseLabel();
    }

    // ── Panel open/close ───────────────────────────────────────────────────────

    public void OpenPanel()
    {
        if (!_recordingReady || _recorder == null) return;
        if (panel != null) panel.SetActive(true);

        // Show first frame immediately.
        ShowFrame(0);
    }

    public void ClosePanel()
    {
        PausePlayback();
        if (panel != null) panel.SetActive(false);
    }

    public void SaveRecording()
    {
        StartCoroutine(SaveRoutine());
    }

    private IEnumerator SaveRoutine()
    {
        // Show "Saving…" for one frame so Unity can repaint before blocking.
        if (timeLabel != null) timeLabel.text = "Saving\u2026";
        yield return null;

        _recorder?.SaveAsMP4();

        // Restore label.
        UpdateTimeLabel();
    }

    // ── Playback ───────────────────────────────────────────────────────────────

    private void OnPlayPauseClicked()
    {
        if (_isPlaying)
            PausePlayback();
        else
            StartPlayback();
    }

    private void StartPlayback()
    {
        if (!_recordingReady || _recorder == null || _recorder.FrameCount == 0) return;
        _isPlaying = true;
        _playbackStartRealtime = Time.realtimeSinceStartup;
        _playbackStartFrame = _currentFrame;
        UpdatePlayPauseLabel();
    }

    private void PausePlayback()
    {
        _isPlaying = false;
        UpdatePlayPauseLabel();
    }

    // ── Update-driven playback ─────────────────────────────────────────────────

    private void Update()
    {
        if (!_isPlaying || _recorder == null || _recorder.FrameCount == 0) return;

        float elapsed = (Time.realtimeSinceStartup - _playbackStartRealtime) * playbackSpeedMultiplier;
        int targetFrame = _playbackStartFrame + Mathf.FloorToInt(elapsed * _recorder.CaptureFps);
        targetFrame = targetFrame % _recorder.FrameCount; // loop

        if (targetFrame != _currentFrame)
            ShowFrame(targetFrame);
    }

    // ── Slider ─────────────────────────────────────────────────────────────────

    private void OnSliderDragged(float value)
    {
        PausePlayback();
        int targetFrame = Mathf.RoundToInt(value);
        ShowFrame(targetFrame);
        // Reset anchor so pressing Play resumes from here at correct speed.
        _playbackStartFrame = targetFrame;
        _playbackStartRealtime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// Sets the slider value without triggering <see cref="OnSliderDragged"/>,
    /// preventing the feedback loop when playback advances the slider programmatically.
    /// </summary>
    private void SetSliderSilently(float value)
    {
        if (timelineSlider == null) return;
        timelineSlider.onValueChanged.RemoveListener(OnSliderDragged);
        timelineSlider.value = value;
        timelineSlider.onValueChanged.AddListener(OnSliderDragged);
    }

    // ── Frame display ──────────────────────────────────────────────────────────

    private void ShowFrame(int index)
    {
        if (_recorder == null) return;
        index = Mathf.Clamp(index, 0, Mathf.Max(_recorder.FrameCount - 1, 0));
        _currentFrame = index;

        Texture2D tex = _recorder.GetFrameTexture(index);
        if (tex != null && videoDisplay != null)
            videoDisplay.texture = tex;

        SetSliderSilently(index);
        UpdateTimeLabel();
    }

    // ── Label helpers ──────────────────────────────────────────────────────────

    private void UpdatePlayPauseLabel()
    {
        if (_playPauseLabel != null)
            _playPauseLabel.text = _isPlaying ? "Pause" : "Play";
    }

    private void UpdateTimeLabel()
    {
        if (timeLabel != null && _recorder != null)
            timeLabel.text = $"{_currentFrame} / {Mathf.Max(_recorder.FrameCount - 1, 0)}";
    }

    // ── OnGUI fallback ─────────────────────────────────────────────────────────

    private void OnGUI()
    {
        // Only show when: recording is done, no openButton assigned, panel is closed.
        if (!_recordingReady) return;
        if (openButton != null) return;
        if (panel != null && panel.activeSelf) return;

        float btnW = 140f;
        float btnH = 36f;
        float margin = 10f;
        var rect = new Rect(Screen.width - btnW - margin, margin, btnW, btnH);

        if (GUI.Button(rect, "Watch Replay"))
            OpenPanel();
    }
}
