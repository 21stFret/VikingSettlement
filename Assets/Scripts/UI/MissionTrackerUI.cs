using System.Text;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple HUD element that displays the currently active mission and objective progress.
/// </summary>
public class MissionTrackerUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject trackerPanel;
    private List<MissionItemUI> missionItems = new List<MissionItemUI>();
    private StringBuilder sb = new StringBuilder();
    private MissionManager MM;

    private void Start()
    {
        if (trackerPanel != null) trackerPanel.SetActive(false);
        missionItems.Clear();
        missionItems.AddRange(trackerPanel.GetComponentsInChildren<MissionItemUI>());
    }

    public void Init()
    {
        if (MissionManager.Instance != null)
        {
            MM = MissionManager.Instance;
            MM.OnMissionAccepted += OnMissionAccepted;
            MM.OnMissionCompleted += OnMissionCompleted;
            MM.OnObjectiveUpdated += OnObjectiveUpdated;
        }
    }

    private void OnMissionAccepted(ActiveMission mission)
    {
        UpdateDisplay();
        if (trackerPanel != null) trackerPanel.SetActive(true);
    }

    private void OnMissionCompleted(ActiveMission mission)
    {
        // Check if there's another active mission to track
        if (MissionManager.Instance != null)
        {
            var active = MissionManager.Instance.GetActiveMissions();
            if (active.Count > 0)
            {
                UpdateDisplay();
            }
            else
            {
                if (trackerPanel != null) trackerPanel.SetActive(false);
            }
        }
        
    }

    private void OnObjectiveUpdated(ActiveMission mission, int objectiveIndex)
    {
            UpdateDisplay();
    }

    // ── Public API (for PlayerMenu's Quests tab) ─────────────────────────────
    // Note: this panel is normally driven automatically by mission events (it shows
    // itself on accept, hides itself when nothing is active). Open()/Close() just let
    // the player pull it up/dismiss it manually from the menu on top of that.

    public void Open()
    {
        if (MM != null) UpdateDisplay();
        if (trackerPanel != null) trackerPanel.SetActive(true);
    }

    public void Close()
    {
        if (trackerPanel != null) trackerPanel.SetActive(false);
    }

    private void UpdateDisplay()
    {
        foreach(var item in missionItems)
        { 
            item.gameObject.SetActive(false);
        }

        for(int i = 0; i < MM.activeMissions.Count; i++)
        {
            var item = missionItems[i];
            var activemission = MM.activeMissions[i];
            string objectivesList = "";
            item.missionTitleText.text = activemission.definition.title;
            item.missionInfoText.text = activemission.definition.description;
            for(int j = 0; j< activemission.definition.objectives.Length; j++)
            {
                string objective = activemission.GetObjectiveDescription(j);
                if(activemission.IsObjectiveComplete(j))
                {
                    objective = $"<s>{objective}</s>";
                }
                objectivesList += objective;
                if (j+1 <= activemission.definition.objectives.Length)
                {
                    objectivesList += "\n";
                }
            }
            item.objectivesText.text = objectivesList;
            item.gameObject.SetActive(true);
        }

    }
}
