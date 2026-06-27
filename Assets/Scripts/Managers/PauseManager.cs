using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages three types of pause:
/// 1. Menu Pause - Full pause with settings/save/quit UI (Time.timeScale = 0)
/// 2. Strategic Pause - Freezes everything including animations (Time.timeScale = 0), but allows camera pan and building clicks
/// 3. Dialogue Pause - Game ticks paused but animations/world keep running (Time.timeScale = 1). Used for dialogue and cutscenes.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public enum PauseState
    {
        Playing,
        DialoguePause,
        StrategicPause,
        MenuPause
    }

    [Header("Current State")]
    [SerializeField] private PauseState currentState = PauseState.Playing;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private CanvasGroup savePopupCanvas;

    [Header("UI Panels")]
    [SerializeField] private GameObject menuPausePanel;
    [SerializeField] private GameObject strategicPauseIndicator;
    [SerializeField] private GameObject savePopup;
    [SerializeField] private TMP_Text savePopupText;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button testSceneButton;
    [SerializeField] private Button holmgangSceneButton;
    [SerializeField] private Button closeMenuButton;


    [Header("Strategic Pause Settings")]
    [SerializeField] private float cameraPanSpeed = 10f;
    [SerializeField] private float cameraEdgePanMargin = 50f;
    [SerializeField] private bool useEdgePanning = true;

    // Events
    public event Action<PauseState> OnPauseStateChanged;

    // Properties
    public PauseState CurrentState => currentState;
    public bool IsGamePaused => currentState != PauseState.Playing;
    public bool IsMenuPaused => currentState == PauseState.MenuPause;
    public bool IsStrategicPaused => currentState == PauseState.StrategicPause;
    public bool IsDialoguePaused => currentState == PauseState.DialoguePause;

    // Input
    private PlayerInputActions inputActions;
    private Vector2 cameraPanInput;

    // Track which state we were in before dialogue (to restore after)
    private PauseState stateBeforeDialogue;

    private Coroutine flashCoroutine;

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

        inputActions = new PlayerInputActions();
        SetupButtons();
    }

    public void Init()
    {
        // Auto-find references if not set
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }
        if (cameraController == null)
        {
            cameraController = CameraController.Instance;
        }

        // Hide UI panels initially
        if (menuPausePanel != null) menuPausePanel.SetActive(false);
        if (strategicPauseIndicator != null) strategicPauseIndicator.SetActive(false);
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Pause.performed += OnPauseInput;
        inputActions.Player.StrategicPause.performed += OnStrategicPauseInput;
    }

    private void OnDisable()
    {
        inputActions.Player.Pause.performed -= OnPauseInput;
        inputActions.Player.StrategicPause.performed -= OnStrategicPauseInput;
        inputActions.Disable();
        ExitMenuPause();

    }

    private void FlashSavedPopup()
    {
        if (savePopup != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashSavedPopupCoroutine());
        }
    }

    private System.Collections.IEnumerator FlashSavedPopupCoroutine()
    {
        savePopupText.text = $"Game saved to slot {SaveManager.Instance.CurrentSlot} successfully!";
        savePopupCanvas.alpha = 0f;
        while (savePopupCanvas.alpha < 1)
        {
            savePopupCanvas.alpha += Time.unscaledDeltaTime * 2f; // Fade in over 0.5 seconds
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1f); // Stay visible for 1 second
        while (savePopupCanvas.alpha > 0)
        {
            savePopupCanvas.alpha -= Time.unscaledDeltaTime * 2f; // Fade in over 0.5 seconds
            yield return null;
        }
        yield break;
    }

    private void SetupButtons()
    {
        if (savePopup != null)
        {
            savePopupCanvas = savePopup.GetComponent<CanvasGroup>();
            savePopupCanvas.alpha = 0f;
        }

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() =>
            {
                SaveManager.Instance.SaveToCurrentSlot();
                FlashSavedPopup();
            });
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() =>
            {
                // Implement settings functionality here
                Debug.Log("Settings button clicked");
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() =>
            {
                GameManager.Instance.ReturnToMainMenu();
            });
        }

        if(testSceneButton != null)
        {
            testSceneButton.onClick.AddListener(() =>
            {
                if (LoadingScreenManager.Instance != null)
                    LoadingScreenManager.Instance.LoadScene("Combat Testing");
                else
                    SceneManager.LoadScene("Combat Testing");
            });
        }

        if(holmgangSceneButton != null)
        {
            holmgangSceneButton.onClick.AddListener(() =>
            {
                if (LoadingScreenManager.Instance != null)
                    LoadingScreenManager.Instance.LoadScene("Holmgang Scene");
            });
        }

        if (closeMenuButton != null)
        {
            closeMenuButton.onClick.AddListener(() =>
            {
                ExitMenuPause();
            });
        }
    }

    private void Update()
    {
        // Handle camera panning during strategic pause
        if (currentState == PauseState.StrategicPause)
        {
            HandleStrategicPauseCameraPan();
        }
    }

    #region Input Handlers

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        // Escape key - toggle menu pause
        if (currentState == PauseState.MenuPause)
        {
            ExitMenuPause();
        }
        else if (currentState == PauseState.DialoguePause)
        {
            // Don't allow menu pause during dialogue
            return;
        }
        else if (currentState == PauseState.StrategicPause)
        {
            // Exit strategic pause first, then open menu
            ExitStrategicPause();
            EnterMenuPause();
        }
        else
        {
            EnterMenuPause();
        }
    }

    private void OnStrategicPauseInput(InputAction.CallbackContext context)
    {
        // Space key - toggle strategic pause
        if (currentState == PauseState.StrategicPause)
        {
            ExitStrategicPause();
        }
        else if (currentState == PauseState.Playing)
        {
            EnterStrategicPause();
        }
        // Don't allow strategic pause from menu pause or dialogue
    }

    #endregion

    #region Menu Pause

    /// <summary>
    /// Enter full menu pause - freezes everything with Time.timeScale
    /// </summary>
    public void EnterMenuPause()
    {
        if (currentState == PauseState.MenuPause) return;

        // If in strategic pause, clean up first
        if (currentState == PauseState.StrategicPause)
        {
            CleanupStrategicPause(false);
        }

        currentState = PauseState.MenuPause;

        // Freeze time completely
        Time.timeScale = 0f;

        // Also pause game tick manager for consistency
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(true);
        }

        // Show menu UI
        if (menuPausePanel != null) menuPausePanel.SetActive(true);
        if (strategicPauseIndicator != null) strategicPauseIndicator.SetActive(false);

        OnPauseStateChanged?.Invoke(currentState);
        UIFocus.Set(settingsButton.gameObject);
        Debug.Log("Entered Menu Pause");
    }

    /// <summary>
    /// Resume game from menu pause
    /// </summary>
    public void ExitMenuPause()
    {
        if (currentState != PauseState.MenuPause) return;

        currentState = PauseState.Playing;

        // Restore time
        Time.timeScale = 1f;

        // Unpause game tick manager
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(false);
        }

        // Hide menu UI
        if (menuPausePanel != null) menuPausePanel.SetActive(false);

        OnPauseStateChanged?.Invoke(currentState);
        Debug.Log("Resumed from Menu Pause");
    }

    #endregion

    #region Strategic Pause

    /// <summary>
    /// Enter strategic pause - freezes everything (Time.timeScale = 0) but allows camera pan and building clicks
    /// </summary>
    public void EnterStrategicPause()
    {
        if (currentState != PauseState.Playing) return;

        currentState = PauseState.StrategicPause;

        // Freeze time completely - animations and all stop
        Time.timeScale = 0f;

        // Pause game simulation
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(true);
        }

        // Enable free camera mode
        if (cameraController != null)
        {
            cameraController.SetFreeCameraMode(true);
        }

        // Disable player movement (but building selection still works)
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        // Show strategic pause indicator
        if (strategicPauseIndicator != null) strategicPauseIndicator.SetActive(true);

        OnPauseStateChanged?.Invoke(currentState);
        Debug.Log("Entered Strategic Pause - Click buildings or pan camera. Press Space to resume.");
    }

    /// <summary>
    /// Exit strategic pause and snap camera back to player
    /// </summary>
    public void ExitStrategicPause()
    {
        if (currentState != PauseState.StrategicPause) return;

        CleanupStrategicPause(true);

        currentState = PauseState.Playing;

        // Restore time
        Time.timeScale = 1f;

        OnPauseStateChanged?.Invoke(currentState);
        Debug.Log("Exited Strategic Pause");
    }

    private void CleanupStrategicPause(bool snapToPlayer)
    {
        // Unpause game simulation
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(false);
        }

        // Disable free camera mode and snap back to player
        if (cameraController != null)
        {
            cameraController.SetFreeCameraMode(false);

            if (snapToPlayer && cameraController.playerTarget != null)
            {
                cameraController.ReturnToPlayerTarget();
                cameraController.SnapToTarget();
            }
        }

        // Re-enable player movement
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }

        // Hide strategic pause indicator
        if (strategicPauseIndicator != null) strategicPauseIndicator.SetActive(false);
    }

    private void HandleStrategicPauseCameraPan()
    {
        if (cameraController == null) return;

        Vector2 panDirection = Vector2.zero;

        // WASD/Arrow key panning - use Time.unscaledDeltaTime since timeScale is 0
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                panDirection.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                panDirection.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                panDirection.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                panDirection.x += 1;
        }

        // Edge panning with mouse
        if (useEdgePanning && Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (mousePos.x < cameraEdgePanMargin)
                panDirection.x -= 1;
            else if (mousePos.x > Screen.width - cameraEdgePanMargin)
                panDirection.x += 1;

            if (mousePos.y < cameraEdgePanMargin)
                panDirection.y -= 1;
            else if (mousePos.y > Screen.height - cameraEdgePanMargin)
                panDirection.y += 1;
        }

        // Apply pan using unscaledDeltaTime since timeScale is 0
        if (panDirection != Vector2.zero)
        {
            cameraController.PanCamera(panDirection.normalized * cameraPanSpeed * Time.unscaledDeltaTime);
        }
    }

    #endregion

    #region Dialogue Pause

    /// <summary>
    /// Enter dialogue pause - game ticks stop but animations and world keep running.
    /// Used for dialogue sequences and cutscenes.
    /// </summary>
    public void EnterDialoguePause()
    {
        if (currentState == PauseState.DialoguePause) return;

        stateBeforeDialogue = currentState;

        // If in strategic pause, restore timeScale first
        if (currentState == PauseState.StrategicPause)
        {
            Time.timeScale = 1f;
            // Keep camera and strategic pause visuals as-is, they'll resolve when dialogue ends
            if (strategicPauseIndicator != null) strategicPauseIndicator.SetActive(false);
        }

        currentState = PauseState.DialoguePause;

        // Pause game simulation only - animations keep playing
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(true);
        }

        // Time.timeScale stays at 1 so animations, particles, etc. keep running

        // Disable player movement during dialogue
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        OnPauseStateChanged?.Invoke(currentState);
        Debug.Log("Entered Dialogue Pause");
    }

    /// <summary>
    /// Exit dialogue pause - restore to playing state
    /// </summary>
    public void ExitDialoguePause()
    {
        if (currentState != PauseState.DialoguePause) return;

        currentState = PauseState.Playing;

        // Unpause game simulation
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.SetPaused(false);
        }

        // Re-enable player movement
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }

        OnPauseStateChanged?.Invoke(currentState);
        Debug.Log("Exited Dialogue Pause");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Force set pause state (useful for external systems like succession UI)
    /// </summary>
    public void SetPauseState(PauseState newState)
    {
        if (currentState == newState) return;

        switch (newState)
        {
            case PauseState.Playing:
                if (currentState == PauseState.MenuPause)
                    ExitMenuPause();
                else if (currentState == PauseState.StrategicPause)
                    ExitStrategicPause();
                else if (currentState == PauseState.DialoguePause)
                    ExitDialoguePause();
                break;
            case PauseState.MenuPause:
                EnterMenuPause();
                break;
            case PauseState.StrategicPause:
                if (currentState == PauseState.MenuPause)
                {
                    ExitMenuPause();
                }
                EnterStrategicPause();
                break;
            case PauseState.DialoguePause:
                EnterDialoguePause();
                break;
        }
    }

    /// <summary>
    /// Check if player input should be processed
    /// </summary>
    public bool ShouldProcessPlayerInput()
    {
        return currentState == PauseState.Playing;
    }

    /// <summary>
    /// Check if building selection should be allowed
    /// </summary>
    public bool ShouldAllowBuildingSelection()
    {
        return currentState == PauseState.Playing || currentState == PauseState.StrategicPause;
    }

    #endregion
}
