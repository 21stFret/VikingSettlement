using System;
using UnityEngine;

/// <summary>
/// Singleton that orchestrates dialogue playback.
/// Enters DialoguePause when active, drives DialogueUI for display.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private DialogueUI dialogueUI;

    // State
    private DialogueSO currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive;

    // Callbacks for the current dialogue session
    private Action onDialogueComplete;
    private Action onQuestAccepted;
    private Action onQuestDeclined;

    // Events
    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;

    public bool IsDialogueActive => isDialogueActive;

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

    private void Start()
    {
        // Auto-find UI if not assigned
        if (dialogueUI == null)
        {
            dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetCallbacks(OnAdvanceRequested, OnQuestAccepted, OnQuestDeclined);
        }
    }

    /// <summary>
    /// Start a dialogue sequence.
    /// </summary>
    /// <param name="dialogue">The DialogueSO to play</param>
    /// <param name="onComplete">Called when dialogue ends (regardless of quest choice)</param>
    /// <param name="onAccepted">Called if player accepts the quest offer</param>
    /// <param name="onDeclined">Called if player declines the quest offer</param>
    public void StartDialogue(DialogueSO dialogue, Action onComplete = null, Action onAccepted = null, Action onDeclined = null)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: Tried to start null or empty dialogue.");
            onComplete?.Invoke();
            return;
        }

        if (isDialogueActive)
        {
            Debug.LogWarning("DialogueManager: Dialogue already active, ignoring new request.");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;
        onDialogueComplete = onComplete;
        onQuestAccepted = onAccepted;
        onQuestDeclined = onDeclined;

        // Enter dialogue pause
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.EnterDialoguePause();
        }

        // Show UI and first line
        if (dialogueUI != null)
        {
            dialogueUI.Show();
            dialogueUI.ShowLine(currentDialogue.lines[currentLineIndex]);
        }

        OnDialogueStarted?.Invoke();
        Debug.Log($"Dialogue started with {dialogue.lines.Length} lines.");
    }

    /// <summary>
    /// Called by DialogueUI when the player wants to advance
    /// </summary>
    private void OnAdvanceRequested()
    {
        if (!isDialogueActive) return;

        currentLineIndex++;

        if (currentLineIndex < currentDialogue.lines.Length)
        {
            // Show next line
            if (dialogueUI != null)
            {
                dialogueUI.ShowLine(currentDialogue.lines[currentLineIndex]);
            }
        }
        else
        {
            // End of dialogue lines
            if (currentDialogue.offersQuest)
            {
                // Show quest accept/decline buttons
                if (dialogueUI != null)
                {
                    dialogueUI.ShowQuestButtons();
                }
            }
            else
            {
                EndDialogue();
            }
        }
    }

    /// <summary>
    /// Called when player accepts the quest from dialogue
    /// </summary>
    private void OnQuestAccepted()
    {
        Action callback = onQuestAccepted;
        EndDialogue();
        callback?.Invoke();
    }

    /// <summary>
    /// Called when player declines the quest from dialogue
    /// </summary>
    private void OnQuestDeclined()
    {
        Action callback = onQuestDeclined;
        EndDialogue();
        callback?.Invoke();
    }

    /// <summary>
    /// End the current dialogue and restore game state
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;

        isDialogueActive = false;

        // Hide UI
        if (dialogueUI != null)
        {
            dialogueUI.Hide();
        }

        // Exit dialogue pause
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ExitDialoguePause();
        }

        // Fire completion callback
        onDialogueComplete?.Invoke();

        OnDialogueEnded?.Invoke();

        // Clear references
        currentDialogue = null;
        onDialogueComplete = null;
        onQuestAccepted = null;
        onQuestDeclined = null;

        Debug.Log("Dialogue ended.");
    }
}
