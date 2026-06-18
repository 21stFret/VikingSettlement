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

        // Layer 0: no dependencies
        GameTickManager.Instance?.Initialize();

        // Layer 1: needs GameTickManager
        DayNightManager.Instance?.Initialize();

        // Layer 2: needs GameTickManager + DayNightManager
        SeasonManager.Instance?.Initialize();
        SettlementManager.Instance?.Initialize();
        MissionManager.Instance?.Initialize();

        // Layer 3: needs DayNightManager
        FindAnyObjectByType<BeehiveManager>()?.Initialize();

        // Layer 3: needs JarlManager
        VillagerSpawner.Instance?.Init();
        JarlManager.Instance?.Init();
        SkillTreeManager.Instance?.Initialize();
        AttackCooldownUI.Instance?.Init();

        MouseInputController.Instance?.Init();
        CameraController.Instance?.Init();
    }
}
