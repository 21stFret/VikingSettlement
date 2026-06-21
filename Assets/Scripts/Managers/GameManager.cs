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

    public bool IsGameActive;

    private GameSceneBootstrap GSB;

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
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Start a new game in the specified slot.
    /// </summary>
    public void StartNewGame(int slotNumber)
    {
        CurrentSlot = slotNumber;
        ShouldLoadSave = false;
        GameInitialized = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSlot(slotNumber);
            SaveManager.Instance.SetCurrentSlot(slotNumber);
        }

        Debug.Log($"Starting new game in slot {slotNumber}");
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

        Debug.Log($"Loading autosave (slot {CurrentSlot})");
        LoadScene(gameSceneName);
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
        if (scene.name == gameSceneName && !GameInitialized)
        {
            if(GSB == null)
            {
                GSB = FindAnyObjectByType<GameSceneBootstrap>();
            }
            StartCoroutine(InitializeGameAfterDelay());
        }
    }

    private System.Collections.IEnumerator InitializeGameAfterDelay()
    {
        yield return null;
        yield return null;

        if (ShouldLoadSave && SaveManager.Instance != null && !string.IsNullOrEmpty(SaveFileToLoad))
        {
            Debug.Log($"Applying save data from: {SaveFileToLoad}");
            SaveManager.Instance.LoadGame(SaveFileToLoad);
            RaidManager.Instance?.ApplyPendingResults();
        }
        else if (!ShouldLoadSave && SaveManager.Instance != null)
        {
            // New game - create initial save after a short delay
            yield return new WaitForSeconds(0.5f);
            SaveManager.Instance.SaveToCurrentSlot();
            Debug.Log("Initial save created");
        }

        GameInitialized = true;

        if(GSB != null)
        {
            GSB.Init();
        }
    }

    private void LoadScene(string sceneName)
    {
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
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
