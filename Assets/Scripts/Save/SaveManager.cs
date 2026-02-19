using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton managing save/load operations.
/// - 3 manual save slots (slot1, slot2, slot3)
/// - 1 global autosave that saves the current slot periodically
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Auto-Save")]
    [SerializeField] private float autoSaveIntervalMinutes = 5f;
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private string gameSceneName = "Demo Scene";

    [Header("Slots")]
    public const int MAX_SLOTS = 3;
    public const string AUTOSAVE_NAME = "autosave";

    /// <summary>
    /// The currently active slot number (1-3).
    /// </summary>
    public int CurrentSlot { get; private set; } = 0;

    private string saveFolderPath;
    private Coroutine autoSaveCoroutine;
    private bool isInGameScene;

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

        saveFolderPath = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        CheckCurrentScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopAutoSave();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckCurrentScene();
    }

    private void CheckCurrentScene()
    {
        isInGameScene = SceneManager.GetActiveScene().name == gameSceneName;

        if (isInGameScene && autoSaveEnabled && autoSaveCoroutine == null && CurrentSlot > 0)
        {
            StartAutoSave();
        }
        else if (!isInGameScene)
        {
            StopAutoSave();
        }
    }

    private void StartAutoSave()
    {
        if (autoSaveCoroutine == null && CurrentSlot > 0)
        {
            autoSaveCoroutine = StartCoroutine(AutoSaveLoop());
            Debug.Log($"Auto-save started for slot {CurrentSlot}");
        }
    }

    private void StopAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
            Debug.Log("Auto-save stopped");
        }
    }

    #region Public API

    /// <summary>
    /// Set the current active slot.
    /// </summary>
    public void SetCurrentSlot(int slotNumber)
    {
        CurrentSlot = slotNumber;
        Debug.Log($"Current slot set to {slotNumber}");
    }

    /// <summary>
    /// Get the file name for a slot (e.g., "slot1", "slot2").
    /// </summary>
    public static string GetSlotName(int slotNumber)
    {
        return $"slot{slotNumber}";
    }

    /// <summary>
    /// Save to a specific file.
    /// </summary>
    private void SaveGame(string saveFileName)
    {
        SaveData data = GatherSaveData();
        data.saveName = saveFileName;
        data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.slotNumber = CurrentSlot;

        string json = JsonUtility.ToJson(data, true);
        string filePath = GetSaveFilePath(saveFileName);

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"Game saved to '{saveFileName}'");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }

    /// <summary>
    /// Save to the current slot's manual save.
    /// </summary>
    public void SaveToCurrentSlot()
    {
        if (CurrentSlot > 0)
        {
            SaveGame(GetSlotName(CurrentSlot));
        }
    }

    /// <summary>
    /// Save to the autosave file.
    /// </summary>
    public void SaveAuto()
    {
        if (CurrentSlot > 0)
        {
            SaveGame(AUTOSAVE_NAME);
        }
    }

    /// <summary>
    /// Load from a specific file.
    /// </summary>
    public void LoadGame(string saveFileName)
    {
        string filePath = GetSaveFilePath(saveFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"No save found: '{saveFileName}'");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError("Failed to deserialize save data.");
                return;
            }

            ApplySaveData(data);
            Debug.Log($"Game loaded from '{saveFileName}'");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
        }
    }

    /// <summary>
    /// Check if a save file exists.
    /// </summary>
    public bool HasSave(string saveFileName)
    {
        return File.Exists(GetSaveFilePath(saveFileName));
    }

    /// <summary>
    /// Check if a slot has a save.
    /// </summary>
    public bool SlotHasSave(int slotNumber)
    {
        return HasSave(GetSlotName(slotNumber));
    }

    /// <summary>
    /// Check if autosave exists.
    /// </summary>
    public bool HasAutosave()
    {
        return HasSave(AUTOSAVE_NAME);
    }

    /// <summary>
    /// Delete a save file.
    /// </summary>
    public void DeleteSave(string saveFileName)
    {
        string filePath = GetSaveFilePath(saveFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Save deleted: '{saveFileName}'");
        }
    }

    /// <summary>
    /// Delete a slot's save.
    /// </summary>
    public void DeleteSlot(int slotNumber)
    {
        DeleteSave(GetSlotName(slotNumber));
    }

    /// <summary>
    /// Get info about a save file.
    /// </summary>
    public SaveSlotInfo GetSaveInfo(string saveFileName)
    {
        SaveSlotInfo info = new SaveSlotInfo
        {
            slotName = saveFileName,
            exists = false
        };

        string filePath = GetSaveFilePath(saveFileName);
        if (File.Exists(filePath))
        {
            info.exists = true;
            try
            {
                string json = File.ReadAllText(filePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data != null)
                {
                    info.saveName = data.saveName;
                    info.saveTimestamp = data.saveTimestamp;
                    info.slotNumber = data.slotNumber;
                }
            }
            catch
            {
                info.saveName = "Corrupted Save";
            }
        }

        return info;
    }

    /// <summary>
    /// Get info about all manual slots.
    /// </summary>
    public SaveSlotInfo[] GetAllSlots()
    {
        var slots = new SaveSlotInfo[MAX_SLOTS];
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            slots[i] = GetSaveInfo(GetSlotName(i + 1));
            slots[i].slotNumber = i + 1;
        }
        return slots;
    }

    /// <summary>
    /// Get autosave info.
    /// </summary>
    public SaveSlotInfo GetAutosaveInfo()
    {
        return GetSaveInfo(AUTOSAVE_NAME);
    }

    #endregion

    #region Gather Save Data

    private SaveData GatherSaveData()
    {
        SaveData data = new SaveData();

        if (DayNightManager.Instance != null && DayNightManager.Instance is ISaveable dayNight)
            dayNight.PopulateSaveData(data);

        if (SeasonManager.Instance != null && SeasonManager.Instance is ISaveable season)
            season.PopulateSaveData(data);

        if (ResourceManager.Instance != null && ResourceManager.Instance is ISaveable resource)
            resource.PopulateSaveData(data);

        if (SettlementManager.Instance != null && SettlementManager.Instance is ISaveable settlement)
            settlement.PopulateSaveData(data);

        if (MissionManager.Instance != null && MissionManager.Instance is ISaveable mission)
            mission.PopulateSaveData(data);

        if (JarlManager.Instance != null && JarlManager.Instance is ISaveable jarl)
            jarl.PopulateSaveData(data);

        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance is ISaveable skillTree)
            skillTree.PopulateSaveData(data);

        if (RunestoneManager.Instance != null && RunestoneManager.Instance is ISaveable runestone)
            runestone.PopulateSaveData(data);

        if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance is ISaveable deathBuff)
            deathBuff.PopulateSaveData(data);

        return data;
    }

    #endregion

    #region Apply Save Data

    private void ApplySaveData(SaveData data)
    {
        // Load stat modifiers first so villager ApplySkillBonuses() sees correct values
        // when SettlementManager creates villagers below.
        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance is ISaveable skillTree)
            skillTree.LoadSaveData(data);

        if (RunestoneManager.Instance != null && RunestoneManager.Instance is ISaveable runestone)
            runestone.LoadSaveData(data);

        if (DeathTypeBuff.Instance != null && DeathTypeBuff.Instance is ISaveable deathBuff)
            deathBuff.LoadSaveData(data);

        // World / settlement state
        if (DayNightManager.Instance != null && DayNightManager.Instance is ISaveable dayNight)
            dayNight.LoadSaveData(data);

        if (SeasonManager.Instance != null && SeasonManager.Instance is ISaveable season)
            season.LoadSaveData(data);

        if (ResourceManager.Instance != null && ResourceManager.Instance is ISaveable resource)
            resource.LoadSaveData(data);

        if (SettlementManager.Instance != null && SettlementManager.Instance is ISaveable settlement)
            settlement.LoadSaveData(data);

        if (MissionManager.Instance != null && MissionManager.Instance is ISaveable mission)
            mission.LoadSaveData(data);

        if (JarlManager.Instance != null && JarlManager.Instance is ISaveable jarl)
            jarl.LoadSaveData(data);
    }

    #endregion

    #region Auto-Save

    private IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveIntervalMinutes * 60f);
            SaveAuto();
            Debug.Log($"Auto-save completed for slot {CurrentSlot}");
        }
    }

    #endregion

    #region Helpers

    private string GetSaveFilePath(string saveFileName)
    {
        return Path.Combine(saveFolderPath, saveFileName + ".json");
    }

    #endregion
}
