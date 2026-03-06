using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks active missions, checks objective completion each tick,
/// and grants rewards when missions are done.
/// </summary>
public class MissionManager : MonoBehaviour, ISaveable
{
    public static MissionManager Instance { get; private set; }

    [Header("Active Missions")]
    [SerializeField] private List<ActiveMission> activeMissions = new List<ActiveMission>();
    [SerializeField] private List<string> completedMissionIds = new List<string>();

    // Events
    public event Action<ActiveMission> OnMissionAccepted;
    public event Action<ActiveMission> OnMissionCompleted;
    public event Action<ActiveMission, int> OnObjectiveUpdated; // mission + objective index

    public MissionTrackerUI missionTrackerUI;

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
        missionTrackerUI?.Init();
    }

    private void Start()
    {
        // Subscribe to game tick for objective checking
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnGameTick += OnTick;
        }

        // Subscribe to raid completion
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.OnRaidEnded += OnRaidEnded;
        }

        // Subscribe to new day for survive time tracking
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNewDay += OnNewDay;
        }

        Building.OnAnyBuildingRepaired += OnBuildingRepaired;
        Building.OnAnyWorkerAssigned  += OnWorkerAssigned;
    }

    private void OnDestroy()
    {
        if (GameTickManager.Instance != null)
        {
            GameTickManager.Instance.OnGameTick -= OnTick;
        }
        if (RaidManager.Instance != null)
        {
            RaidManager.Instance.OnRaidEnded -= OnRaidEnded;
        }
        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnNewDay -= OnNewDay;
        }

        Building.OnAnyBuildingRepaired -= OnBuildingRepaired;
        Building.OnAnyWorkerAssigned  -= OnWorkerAssigned;
    }

    /// <summary>
    /// Accept a mission from a definition SO and begin tracking its objectives
    /// </summary>
    public ActiveMission AcceptMission(MissionDefinitionSO definition)
    {
        if (definition == null) return null;

        ActiveMission mission = new ActiveMission(definition);
        activeMissions.Add(mission);

        OnMissionAccepted?.Invoke(mission);
        Debug.Log($"Mission accepted: {definition.title}");

        // Immediately check gather objectives in case player already has the resources
        CheckGatherObjectives(mission);

        return mission;
    }

    /// <summary>
    /// Complete a mission and grant its rewards
    /// </summary>
    public void CompleteMission(ActiveMission mission)
    {
        if (mission == null) return;
        if (!activeMissions.Contains(mission)) return;

        mission.status = MissionStatus.Completed;
        activeMissions.Remove(mission);
        completedMissionIds.Add(mission.definition.missionId);

        GrantRewards(mission.definition.rewards);

        OnMissionCompleted?.Invoke(mission);
        Debug.Log($"Mission completed: {mission.definition.title}");
    }

    /// <summary>
    /// Check if a mission has been completed by its ID
    /// </summary>
    public bool IsMissionCompleted(string missionId)
    {
        return completedMissionIds.Contains(missionId);
    }

    /// <summary>
    /// Get all currently active missions
    /// </summary>
    public List<ActiveMission> GetActiveMissions()
    {
        return activeMissions;
    }

    /// <summary>
    /// Get completed mission IDs (for save system)
    /// </summary>
    public List<string> GetCompletedMissionIds()
    {
        return completedMissionIds;
    }

    /// <summary>
    /// Check if a specific mission is active and has all objectives complete (ready to turn in)
    /// </summary>
    public bool IsMissionReadyToTurnIn(ActiveMission mission)
    {
        return mission != null && mission.status == MissionStatus.Active && mission.AreAllObjectivesComplete();
    }

    private void OnTick(float deltaTime)
    {
        for (int i = activeMissions.Count - 1; i >= 0; i--)
        {
            CheckGatherObjectives(activeMissions[i]);
        }
    }

    private void OnNewDay()
    {
        // Increment survive time objectives by 1 day
        for (int i = 0; i < activeMissions.Count; i++)
        {
            ActiveMission mission = activeMissions[i];
            if (mission.definition.objectives == null) continue;

            for (int j = 0; j < mission.definition.objectives.Length; j++)
            {
                MissionObjectiveTemplate template = mission.definition.objectives[j];
                if (template.type == MissionObjectiveType.SurviveTime && !mission.IsObjectiveComplete(j))
                {
                    mission.objectiveProgress[j] += 1f;
                    OnObjectiveUpdated?.Invoke(mission, j);
                }
            }
        }
    }

    private void OnRaidEnded(RaidReport report)
    {
        // Mark raid objectives as complete
        for (int i = 0; i < activeMissions.Count; i++)
        {
            ActiveMission mission = activeMissions[i];
            if (mission.definition.objectives == null) continue;

            for (int j = 0; j < mission.definition.objectives.Length; j++)
            {
                MissionObjectiveTemplate template = mission.definition.objectives[j];
                if (template.type == MissionObjectiveType.CompleteRaid && !mission.IsObjectiveComplete(j))
                {
                    mission.objectiveProgress[j] = template.targetAmount;
                    OnObjectiveUpdated?.Invoke(mission, j);
                }
            }
        }
    }

    private void OnBuildingRepaired(Building building)
    {
        foreach (var mission in activeMissions)
        {
            if (mission.definition.objectives == null) continue;
            for (int i = 0; i < mission.definition.objectives.Length; i++)
            {
                var template = mission.definition.objectives[i];
                if (template.type != MissionObjectiveType.RepairBuilding) continue;
                if (mission.IsObjectiveComplete(i)) continue;
                if (building.data == null || building.data.buildingType != template.targetBuildingType) continue;

                mission.objectiveProgress[i] = template.targetAmount;
                OnObjectiveUpdated?.Invoke(mission, i);
            }
        }
    }

    private void OnWorkerAssigned(Building building)
    {
        foreach (var mission in activeMissions)
        {
            if (mission.definition.objectives == null) continue;
            for (int i = 0; i < mission.definition.objectives.Length; i++)
            {
                var template = mission.definition.objectives[i];
                if (template.type != MissionObjectiveType.AssignVillager) continue;
                if (mission.IsObjectiveComplete(i)) continue;
                if (building.data == null || building.data.buildingType != template.targetBuildingType) continue;

                mission.objectiveProgress[i] = template.targetAmount;
                OnObjectiveUpdated?.Invoke(mission, i);
            }
        }
    }

    private void CheckGatherObjectives(ActiveMission mission)
    {
        if (mission.definition.objectives == null || ResourceManager.Instance == null) return;

        for (int i = 0; i < mission.definition.objectives.Length; i++)
        {
            MissionObjectiveTemplate template = mission.definition.objectives[i];
            if (template.type != MissionObjectiveType.GatherResource) continue;

            float currentResource = ResourceManager.Instance.GetResource(template.resourceType);
            float previousAmount = mission.objectiveProgress[i];
            mission.objectiveProgress[i] = Mathf.Min(currentResource, template.targetAmount);

            if (Mathf.FloorToInt(mission.objectiveProgress[i]) != Mathf.FloorToInt(previousAmount))
            {
                OnObjectiveUpdated?.Invoke(mission, i);
            }
        }
    }

    private void GrantRewards(MissionReward[] rewards)
    {
        if (rewards == null) return;

        for (int i = 0; i < rewards.Length; i++)
        {
            MissionReward reward = rewards[i];

            switch (reward.rewardType)
            {
                case MissionRewardType.Resource:
                    if (ResourceManager.Instance != null)
                    {
                        ResourceManager.Instance.AddResource(reward.resourceType, reward.amount);
                        Debug.Log($"Reward: +{reward.amount} {reward.resourceType}");
                    }
                    break;

                case MissionRewardType.Experience:
                    // Grant XP through skill tree
                    if (SkillTreeManager.Instance != null)
                    {
                        SkillTreeManager.Instance.AddXP(Mathf.FloorToInt(reward.amount));
                        Debug.Log($"Reward: +{reward.amount} XP");
                    }
                    break;

                case MissionRewardType.Weapon:
                case MissionRewardType.Shield:
                    if (reward.itemReward != null)
                    {
                        // Instantiate the weapon/shield near the player
                        if (JarlManager.Instance != null && JarlManager.Instance.CurrentJarl != null)
                        {
                            Vector3 spawnPos = JarlManager.Instance.CurrentJarl.transform.position + Vector3.right;
                            Instantiate(reward.itemReward.gameObject, spawnPos, Quaternion.identity);
                            Debug.Log($"Reward: {reward.itemReward.itemName} spawned near Jarl");
                        }
                    }
                    break;
            }
        }
    }

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        var missionSave = new MissionSaveData();

        // Save active missions
        missionSave.activeMissions = new ActiveMissionSave[activeMissions.Count];
        for (int i = 0; i < activeMissions.Count; i++)
        {
            ActiveMission am = activeMissions[i];
            missionSave.activeMissions[i] = new ActiveMissionSave
            {
                missionDefinitionId = am.definition.missionId,
                objectiveProgress = (float[])am.objectiveProgress.Clone(),
                status = (int)am.status
            };
        }

        // Save completed mission IDs
        missionSave.completedMissionIds = completedMissionIds.ToArray();

        data.missions = missionSave;
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.missions == null) return;

        // Restore completed IDs
        completedMissionIds.Clear();
        if (data.missions.completedMissionIds != null)
        {
            completedMissionIds.AddRange(data.missions.completedMissionIds);
        }

        // Restore active missions — requires looking up SO assets by missionId
        activeMissions.Clear();
        if (data.missions.activeMissions != null)
        {
            MissionDefinitionSO[] allDefinitions = Resources.LoadAll<MissionDefinitionSO>("");
            foreach (var saved in data.missions.activeMissions)
            {
                MissionDefinitionSO def = System.Array.Find(allDefinitions, d => d.missionId == saved.missionDefinitionId);
                if (def == null)
                {
                    Debug.LogWarning($"MissionManager: Could not find MissionDefinitionSO with id '{saved.missionDefinitionId}'");
                    continue;
                }

                ActiveMission am = new ActiveMission(def);
                am.status = (MissionStatus)saved.status;
                if (saved.objectiveProgress != null)
                {
                    System.Array.Copy(saved.objectiveProgress, am.objectiveProgress,
                        Mathf.Min(saved.objectiveProgress.Length, am.objectiveProgress.Length));
                }
                activeMissions.Add(am);
            }
        }
    }

    #endregion
}
