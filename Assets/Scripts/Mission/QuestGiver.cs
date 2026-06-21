using UnityEngine;

/// <summary>
/// Component placed on an NPC that can give and complete quests.
/// Shows icons above the NPC to indicate quest availability or completion.
/// Implements IClickable so the player can interact with it.
/// </summary>
public class QuestGiver : WorldInteractable, IClickable
{
    [Header("Missions")]
    [SerializeField] private MissionDefinitionSO[] availableMissions;

    [Header("Icons")]
    [SerializeField] private GameObject questAvailableIcon;
    [SerializeField] private GameObject questCompleteIcon;

    [Header("Interaction")]
    [SerializeField] private Collider2D interactionCollider;
    [SerializeField] private int clickPriority = 5;

    // State
    private int currentMissionIndex = 0;
    private ActiveMission activeMission;

    // IClickable implementation
    public Collider2D Collider => interactionCollider;
    public int ClickPriority => clickPriority;
    public bool IsClickable => CanInteract();

    /// <summary>
    /// Unique identifier for this quest giver (used by save system)
    /// </summary>
    public string questGiverId;

    private void Start()
    {
        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider2D>();
        }
        MouseInputController.RegisterClickable(this);

        if (string.IsNullOrEmpty(questGiverId))
        {
            questGiverId = gameObject.name;
        }

        UpdateIcons();
    }

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionCompleted += OnMissionCompleted;
            MissionManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
        }
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionCompleted -= OnMissionCompleted;
            MissionManager.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
        }
    }

    public override void Interact()
    {
        OnClicked();
        PlayerController.Instance?.SetInputEnabled(false);
    }

    public override void Deselect()
    {
        PlayerController.Instance?.SetInputEnabled(true);
    }


    /// <summary>
    /// Called when the player clicks this quest giver
    /// </summary>
    public void OnClicked()
    {
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsDialogueActive) return;

        if (activeMission != null && MissionManager.Instance.IsMissionReadyToTurnIn(activeMission))
        {
            // Quest is done - play completion dialogue and turn in
            HandleQuestTurnIn();
        }
        else if (activeMission == null && HasAvailableMission())
        {
            // Offer a new quest
            HandleQuestOffer();
        }
        else if (activeMission != null)
        {
            // Quest is active but not done - show a reminder
            DialogueSO startDialogue = activeMission.definition.startDialogue;
            if (startDialogue != null && startDialogue.lines != null && startDialogue.lines.Length > 0)
            {
                // Create a temporary reminder SO (runtime-only, not saved as asset)
                DialogueSO reminderDialogue = ScriptableObject.CreateInstance<DialogueSO>();
                reminderDialogue.lines = new DialogueLine[] { startDialogue.lines[startDialogue.lines.Length - 1] };
                reminderDialogue.offersQuest = false;
                DialogueManager.Instance.StartDialogue(reminderDialogue);
            }
        }
    }

    private void HandleQuestOffer()
    {
        MissionDefinitionSO definition = GetNextAvailableMission();
        if (definition == null) return;

        DialogueManager.Instance.StartDialogue(
            definition.startDialogue,
            onComplete: null,
            onAccepted: () =>
            {
                activeMission = MissionManager.Instance.AcceptMission(definition);
                currentMissionIndex++;
                UpdateIcons();
            },
            onDeclined: () =>
            {
                UpdateIcons();
            }
        );
    }

    private void HandleQuestTurnIn()
    {
        ActiveMission mission = activeMission;

        DialogueSO completionDialogue = mission.definition.completionDialogue;
        if (completionDialogue != null && completionDialogue.lines != null && completionDialogue.lines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(
                completionDialogue,
                onComplete: () =>
                {
                    MissionManager.Instance.CompleteMission(mission);
                    activeMission = null;
                    UpdateIcons();
                }
            );
        }
        else
        {
            // No completion dialogue - just complete
            MissionManager.Instance.CompleteMission(mission);
            activeMission = null;
            UpdateIcons();
        }
    }

    private bool CanInteract()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            return false;

        // Can interact if: has available mission, or active mission is ready to turn in, or active mission in progress
        return HasAvailableMission() || activeMission != null;
    }

    private bool HasAvailableMission()
    {
        return availableMissions != null
            && currentMissionIndex < availableMissions.Length
            && activeMission == null;
    }

    private MissionDefinitionSO GetNextAvailableMission()
    {
        if (availableMissions == null || currentMissionIndex >= availableMissions.Length)
            return null;

        MissionDefinitionSO definition = availableMissions[currentMissionIndex];

        // Skip already completed missions
        while (definition != null && MissionManager.Instance != null && MissionManager.Instance.IsMissionCompleted(definition.missionId))
        {
            currentMissionIndex++;
            if (currentMissionIndex >= availableMissions.Length) return null;
            definition = availableMissions[currentMissionIndex];
        }

        return definition;
    }

    private void UpdateIcons()
    {
        bool showAvailable = HasAvailableMission() && activeMission == null;
        bool showComplete = activeMission != null
            && MissionManager.Instance != null
            && MissionManager.Instance.IsMissionReadyToTurnIn(activeMission);

        if (questAvailableIcon != null) questAvailableIcon.SetActive(showAvailable);
        if (questCompleteIcon != null) questCompleteIcon.SetActive(showComplete);
    }

    private void OnMissionCompleted(ActiveMission mission)
    {
        UpdateIcons();
    }

    private void OnObjectiveUpdated(ActiveMission mission, int objectiveIndex)
    {
        if (activeMission == mission)
        {
            UpdateIcons();
        }
    }

    #region Save/Load Helpers

    public int GetCurrentMissionIndex()
    {
        return currentMissionIndex;
    }

    public string GetActiveMissionId()
    {
        return activeMission?.definition.missionId ?? "";
    }

    public void RestoreState(int missionIndex, ActiveMission restored)
    {
        currentMissionIndex = missionIndex;
        activeMission = restored;
        UpdateIcons();
    }

    #endregion
}
