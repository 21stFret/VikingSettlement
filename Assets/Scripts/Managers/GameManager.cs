using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that persists across scenes and handles game initialization.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Demo Scene";

    /// <summary>
    /// The slot number (1-3) for the current playthrough.
    /// </summary>
    public int CurrentSlot { get; private set; }

    /// <summary>
    /// The save file to load (slot name or "autosave").
    /// </summary>
    public string SaveFileToLoad { get; private set; }

    /// <summary>
    /// Whether we should load from save (true) or start fresh (false).
    /// </summary>
    public bool ShouldLoadSave { get; private set; }

    /// <summary>
    /// True after the game scene has been initialized.
    /// </summary>
    public bool GameInitialized { get; private set; }

    /// <summary>
    /// The designated settlement/game scene name — used by GameSceneBootstrap to tell whether
    /// the scene that just loaded is the real game scene (vs. the raid scene, which also has a
    /// GameSceneBootstrap component but must never trigger save/spawn logic).
    /// </summary>
    public string GameSceneName => gameSceneName;

    public bool IsGameActive;

    private GameSceneBootstrap GSB;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Start a new game in the specified slot under the given clan name.
    /// </summary>
    public void StartNewGame(int slotNumber, string clanName)
    {
        CurrentSlot = slotNumber;
        ShouldLoadSave = false;
        GameInitialized = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSlot(slotNumber);
            SaveManager.Instance.SetCurrentSlot(slotNumber);
            SaveManager.Instance.SetClanName(clanName);
        }

        Debug.Log($"Starting new game in slot {slotNumber} for clan '{clanName}'");
        LoadScene(gameSceneName);
    }

    /// <summary>
    /// Load from a slot's manual save.
    /// </summary>
    public void LoadSlot(int slotNumber)
    {
        CurrentSlot = slotNumber;
        SaveFileToLoad = SaveManager.GetSlotName(slotNumber);
        ShouldLoadSave = true;
        GameInitialized = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrentSlot(slotNumber);
        }

        Debug.Log($"Loading slot {slotNumber}");
        LoadScene(gameSceneName);
    }

    /// <summary>
    /// Load from the autosave (continues the last played slot).
    /// </summary>
    public void LoadAutosave()
    {
        SaveFileToLoad = SaveManager.AUTOSAVE_NAME;
        ShouldLoadSave = true;
        GameInitialized = false;

        // The autosave is a single shared file — it isn't per-slot. Its slot number is
        // embedded in the save data itself, so recover it here rather than leaving
        // CurrentSlot at its stale/default value (B2: previously left CurrentSlot at 0,
        // silently breaking QuickSave and auto-save resumption for the rest of the session).
        if (SaveManager.Instance != null)
        {
            int slot = SaveManager.Instance.GetAutosaveInfo().slotNumber;
            CurrentSlot = slot;
            SaveManager.Instance.SetCurrentSlot(slot);
        }

        Debug.Log($"Loading autosave (slot {CurrentSlot})");
        LoadScene(gameSceneName);
    }

    /// <summary>
    /// Prepare for a raid
    /// </summary>
    public void PrepareRaid()
    {
        GameInitialized = false;
        Debug.Log("GameManager: Prepared for raid ");
    }

    /// <summary>
    /// Prepare for returning from a raid — loads the pre-raid autosave on next scene load.
    /// </summary>
    public void PrepareRaidReturn()
    {
        ShouldLoadSave = true;
        SaveFileToLoad = SaveManager.AUTOSAVE_NAME;
        GameInitialized = false;
        Debug.Log("GameManager: Prepared for raid return — will load autosave");
    }

    public void SetShouldLoad() { ShouldLoadSave = true; }

    /// <summary>
    /// Return to the main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        GameInitialized = false;
        CurrentSlot = 0;
        LoadScene(mainMenuSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameInitialized)
        {
            GSB = FindAnyObjectByType<GameSceneBootstrap>();
            if (GSB==null)
            {
                return;
            }
            StartCoroutine(InitializeGameAfterDelay(scene.name));
        }
    }

    private System.Collections.IEnumerator InitializeGameAfterDelay(string loadedSceneName)
    {
        yield return null;
        yield return null;

        bool isGameScene = loadedSceneName == gameSceneName;

        bool didLoadSave = false;
        if (isGameScene)
        {
            if (ShouldLoadSave && SaveManager.Instance != null && !string.IsNullOrEmpty(SaveFileToLoad))
            {
                Debug.Log($"Applying save data from: {SaveFileToLoad}");
                SaveManager.Instance.LoadGame(SaveFileToLoad);
                didLoadSave = true;
            }
            else if (!ShouldLoadSave && SaveManager.Instance != null)
            {
                // New game - create initial save after a short delay
                yield return new WaitForSeconds(0.2f);
                SaveManager.Instance.SaveToCurrentSlot();
                Debug.Log("Initial save created");
            }
        }

        if (isGameScene)
        {
            // Runs after LoadGame (continuing) or the placeholder SaveToCurrentSlot (new game)
            // above, so InfoPopupUI's history is either fully loaded or confirmed empty —
            // its id-based dedupe relies on that ordering to avoid re-showing intro messages.
            InfoPopupUI.Instance?.TryShowIntroSequence();
        }

        GameInitialized = true;

        if(GSB != null)
        {
            GSB.Init();
        }

        // Apply pending raid results AFTER Bootstrap has initialized UI and managers —
        // Jarl-casualty succession fires events that require JarlManager, cameras, and
        // succession UI to be ready (B37).
        if (isGameScene && didLoadSave)
        {
            RaidManager.Instance?.ApplyPendingResults();
        }
    }

    private void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneDeferred(sceneName));
    }

    /// <summary>
    /// See RaidManager.LoadSceneDeferred for why this defers a frame and uses an async fallback:
    /// callers here are also invoked synchronously from inside a UI Button's click-processing
    /// call stack, and LoadingScreenManager.Instance is null unless Play started from MainMenu.
    /// </summary>
    private System.Collections.IEnumerator LoadSceneDeferred(string sceneName)
    {
        yield return null;

        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("GameManager.LoadScene: LoadingScreenManager.Instance is null (it only " +
                "exists in the MainMenu scene) — falling back to an async load with no loading screen.");
            SceneManager.LoadSceneAsync(sceneName);
        }
    }

    /// <summary>
    /// Manual save to current slot.
    /// </summary>
    public void QuickSave()
    {
        if (SaveManager.Instance != null && CurrentSlot > 0)
        {
            SaveManager.Instance.SaveToCurrentSlot();
        }
    }
}
