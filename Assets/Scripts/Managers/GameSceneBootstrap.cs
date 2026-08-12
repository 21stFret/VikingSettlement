using UnityEngine;

/// <summary>
/// Central coordinator for game scene initialization.
/// Runs before all other Start() calls, wires manager event subscriptions in
/// dependency order, then hands off to GameManager to apply save data.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameSceneBootstrap : MonoBehaviour
{
    public void Init()
    {
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // Order is determined by dependency: each manager must come after the managers it subscribes to.

        // The raid scene also carries a GameSceneBootstrap (for its own camera/input/UI wiring),
        // but the settlement-only managers below (calendar/season/storm/settlement/mission/HUD)
        // have no GameObject of their own there, so guard them to the real game scene.
        bool isGameScene = GameManager.Instance != null
            && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameManager.Instance.GameSceneName;

        if (isGameScene)
        {
            // Layer 0: no dependencies
            GameTickManager.Instance?.Initialize();

            // Layer 1: needs GameTickManager
            DayNightManager.Instance?.Initialize();

            // Layer 2: needs GameTickManager + DayNightManager
            SeasonManager.Instance?.Initialize();

            // ── Calendar stack (order is strict) ──────────────────────────────────────
            // SeasonManager → CalendarManager → StormScheduler
            //   SeasonManager must be first: CalendarManager reads season state on first refresh.
            //   StormScheduler must be last: it subscribes to both SeasonManager.OnSeasonChanged
            //   and CalendarManager.OnCalendarUpdated, and calls back into both.
            if (SeasonManager.Instance == null)
                Debug.LogError("GameSceneBootstrap: SeasonManager is null before CalendarManager.Initialize() — calendar data will be wrong.");
            if (CalendarManager.Instance == null)
                Debug.LogError("GameSceneBootstrap: CalendarManager is null — ensure it has a scene GameObject.");
            CalendarManager.Instance?.Initialize();
            StormScheduler.Instance?.Initialize();
            // ──────────────────────────────────────────────────────────────────────────

            SettlementManager.Instance?.Initialize();
            WeaponDatabase.Instance.Init();
            MissionManager.Instance?.Initialize();
        }

        WeatherManager.Instance?.Initialize();

        // Layer 3: needs DayNightManager
        FindAnyObjectByType<BeehiveManager>()?.Initialize();

        // Layer 3: spawn villagers for a new game only (save-loaded villagers are already
        // instantiated and registered by SettlementManager.LoadSaveData before Bootstrap runs).
        // Guarded on being in the actual game scene — the raid scene also carries a
        // GameSceneBootstrap (for its own camera/input/UI wiring) but has no VillagerSpawner of
        // its own, so this must never run there (see GameManager.InitializeGameAfterDelay).
        bool isNewGame = isGameScene && !GameManager.Instance.ShouldLoadSave;
        if (isNewGame)
            VillagerSpawner.Instance?.SpawnInitialSettlement();

        JarlManager.Instance?.Init();

        // Assign the first villager as Jarl for a new game (load path restores Jarl via save data).
        if (isNewGame)
            JarlManager.Instance?.EnsureInitialJarl();

        SkillTreeManager.Instance?.Initialize();
        AttackCooldownUI.Instance?.Init();
        DodgeCooldownUI.Instance?.Init();

        MouseInputController.Instance?.Init();
        CameraController.Instance?.Init();
        PauseManager.Instance?.Init();

        // UIManager (FoodUI/HeatUI) is the settlement wood/warmth HUD — raid scene has its own
        // RaidUI instead, and doesn't wire up these HUD references, so skip it there.
        if (isGameScene)
        {
            UIManager.Instance?.Init();

            // Refresh wood cost UI now that the full calendar stack (StormScheduler included) is warm.
            // Guards against UIManager deferring HeatUI.Init() past the point where storm data is available.
            HeatUI.Instance?.UpdateWoodUI();
        }

        if(isNewGame)
            SaveManager.Instance?.SaveToCurrentSlot();

        GameManager.Instance.SetShouldLoad();
    }
}
