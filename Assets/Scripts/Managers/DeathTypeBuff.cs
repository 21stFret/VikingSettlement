using System;
using UnityEngine;

public enum DeathBuffType
{
    None,
    ValkyrieFavour,
    GefjonBlessing
}

public class DeathTypeBuff : MonoBehaviour, ISaveable
{
    public static DeathTypeBuff Instance { get; private set; }

    [Header("State")]
    [SerializeField] private DeathBuffType currentBuff = DeathBuffType.None;
    [SerializeField] private float remainingTime = 0f;

    private const float BUFF_DURATION = 600f; // 10 minutes in seconds

    public DeathBuffType CurrentBuff => currentBuff;
    public float RemainingTime => remainingTime;
    public bool IsActive => currentBuff != DeathBuffType.None && remainingTime > 0f;

    public event Action<DeathBuffType> OnBuffChanged;

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
    }

    private void Update()
    {
        if (!IsActive) return;

        // Pause timer when game is paused
        if (PauseManager.Instance != null && PauseManager.Instance.IsGamePaused)
            return;

        remainingTime -= Time.unscaledDeltaTime;

        if (remainingTime <= 0f)
        {
            ExpireBuff();
        }
    }

    public void ApplyBuff(DeathCause cause)
    {
        if (cause == DeathCause.Combat)
        {
            currentBuff = DeathBuffType.ValkyrieFavour;
        }
        else
        {
            currentBuff = DeathBuffType.GefjonBlessing;
        }

        remainingTime = BUFF_DURATION;
        Debug.Log($"Death buff applied: {currentBuff} for {BUFF_DURATION}s");
        OnBuffChanged?.Invoke(currentBuff);
    }

    private void ExpireBuff()
    {
        currentBuff = DeathBuffType.None;
        remainingTime = 0f;
        Debug.Log("Death buff expired.");
        OnBuffChanged?.Invoke(currentBuff);
    }

    // --- Query Methods ---

    /// <summary>Valkyrie's Favour: +5% warrior damage</summary>
    public float GetWarriorDamagePercent()
    {
        return currentBuff == DeathBuffType.ValkyrieFavour ? 5f : 0f;
    }

    /// <summary>Valkyrie's Favour: +5% warrior defense</summary>
    public float GetWarriorDefensePercent()
    {
        return currentBuff == DeathBuffType.ValkyrieFavour ? 5f : 0f;
    }

    /// <summary>Valkyrie's Favour: -10% Jarl cooldowns</summary>
    public float GetJarlCooldownPercent()
    {
        return currentBuff == DeathBuffType.ValkyrieFavour ? 10f : 0f;
    }

    /// <summary>Gefjon's Blessing: +10% food production</summary>
    public float GetFoodProductionPercent()
    {
        return currentBuff == DeathBuffType.GefjonBlessing ? 10f : 0f;
    }

    /// <summary>Gefjon's Blessing: +10% birth rate</summary>
    public float GetBirthRatePercent()
    {
        return currentBuff == DeathBuffType.GefjonBlessing ? 10f : 0f;
    }

    /// <summary>Gefjon's Blessing: +15% heal speed</summary>
    public float GetHealSpeedPercent()
    {
        return currentBuff == DeathBuffType.GefjonBlessing ? 15f : 0f;
    }

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        data.deathBuffData = new DeathTypeBuffSaveData
        {
            currentBuffType = (int)currentBuff,
            remainingTime = remainingTime
        };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.deathBuffData == null) return;

        currentBuff = (DeathBuffType)data.deathBuffData.currentBuffType;
        remainingTime = data.deathBuffData.remainingTime;

        if (IsActive)
        {
            Debug.Log($"Loaded death buff: {currentBuff} with {remainingTime:F0}s remaining");
        }
    }

    #endregion
}
