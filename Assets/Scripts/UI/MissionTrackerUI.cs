using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Simple HUD element that displays the currently active mission and objective progress.
/// </summary>
public class MissionTrackerUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TMP_Text missionTitleText;
    [SerializeField] private TMP_Text objectivesText;

    private ActiveMission trackedMission;
    private StringBuilder sb = new StringBuilder();

    private void Start()
    {
        if (trackerPanel != null) trackerPanel.SetActive(false);
    }

    public void Init()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionAccepted += OnMissionAccepted;
            MissionManager.Instance.OnMissionCompleted += OnMissionCompleted;
            MissionManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
        }
    }

    private void OnMissionAccepted(ActiveMission mission)
    {
        trackedMission = mission;
        UpdateDisplay();
        if (trackerPanel != null) trackerPanel.SetActive(true);
    }

    private void OnMissionCompleted(ActiveMission mission)
    {
        if (trackedMission == mission)
        {
            trackedMission = null;

            // Check if there's another active mission to track
            if (MissionManager.Instance != null)
            {
                var active = MissionManager.Instance.GetActiveMissions();
                if (active.Count > 0)
                {
                    trackedMission = active[0];
                    UpdateDisplay();
                }
                else
                {
                    if (trackerPanel != null) trackerPanel.SetActive(false);
                }
            }
        }
    }

    private void OnObjectiveUpdated(ActiveMission mission, int objectiveIndex)
    {
        if (trackedMission == mission)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (trackedMission == null) return;

        if (missionTitleText != null)
        {
            missionTitleText.text = trackedMission.definition.title;
        }

        if (objectivesText != null && trackedMission.definition.objectives != null)
        {
            sb.Clear();
            for (int i = 0; i < trackedMission.definition.objectives.Length; i++)
            {
                if (i > 0) sb.Append("\n");

                if (trackedMission.IsObjectiveComplete(i))
                {
                    sb.Append("<s>");
                    sb.Append(trackedMission.GetObjectiveDescription(i));
                    sb.Append("</s>");
                }
                else
                {
                    sb.Append(trackedMission.GetObjectiveDescription(i));
                }
            }
            objectivesText.text = sb.ToString();
        }
    }
}
