using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles the visual presentation of dialogue: typewriter effect, portrait, speaker name,
/// and accept/decline buttons. Driven by DialogueManager.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject dialoguePanel;

    [Header("Text")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject portraitContainer;

    [Header("Quest Buttons")]
    [SerializeField] private GameObject questButtonContainer;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Continue Indicator")]
    [SerializeField] private GameObject continueIndicator;

    [Header("Typewriter Settings")]
    [SerializeField] private float charsPerSecond = 40f;

    // State
    private Coroutine typewriterCoroutine;
    private bool isTyping;
    private bool lineFullyRevealed;
    private string currentFullText;

    // Input cooldown to prevent spam-advancing
    private float lastAdvanceTime;
    private const float ADVANCE_COOLDOWN = 0.2f;

    // Callbacks
    private Action onAdvanceRequested;
    private Action onAcceptQuest;
    private Action onDeclineQuest;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (questButtonContainer != null) questButtonContainer.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (acceptButton != null) acceptButton.onClick.AddListener(HandleAcceptClicked);
        if (declineButton != null) declineButton.onClick.AddListener(HandleDeclineClicked);

        inputActions = new PlayerInputActions();

    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.ForwardDialogue.performed += GetAdvanceInput;
    }

    private void OnDisable()
    {
        inputActions.Player.ForwardDialogue.performed -= GetAdvanceInput;
        inputActions.Disable();
    }

    public void GetAdvanceInput(InputAction.CallbackContext context)
    {
        if (context.performed && questButtonContainer != null && !questButtonContainer.activeSelf)
        {
            HandleAdvanceInput();
        }
    }

    /// <summary>
    /// Set up callbacks for dialogue interaction
    /// </summary>
    public void SetCallbacks(Action onAdvance, Action onAccept, Action onDecline)
    {
        onAdvanceRequested = onAdvance;
        onAcceptQuest = onAccept;
        onDeclineQuest = onDecline;
    }

    /// <summary>
    /// Show the dialogue panel
    /// </summary>
    public void Show()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (questButtonContainer != null) questButtonContainer.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
        lastAdvanceTime = Time.unscaledTime;
    }

    /// <summary>
    /// Hide the dialogue panel
    /// </summary>
    public void Hide()
    {
        StopTypewriter();
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (questButtonContainer != null) questButtonContainer.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
        PlayerController.Instance?.SetInputEnabled(true);
    }

    /// <summary>
    /// Display a dialogue line with typewriter effect
    /// </summary>
    public void ShowLine(DialogueLine line)
    {
        StopTypewriter();

        // Speaker name
        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        // Portrait
        if (portraitImage != null && portraitContainer != null)
        {
            if (line.portrait != null)
            {
                portraitImage.sprite = line.portrait;
                portraitContainer.SetActive(true);
            }
            else
            {
                portraitContainer.SetActive(false);
            }
        }

        // Hide quest buttons and continue indicator while typing
        if (questButtonContainer != null) questButtonContainer.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        // Start typewriter
        currentFullText = line.text;
        isTyping = true;
        lineFullyRevealed = false;
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(line.text));
    }

    /// <summary>
    /// Show accept/decline buttons for quest offering
    /// </summary>
    public void ShowQuestButtons()
    {
        if (questButtonContainer != null) questButtonContainer.SetActive(true);
        if (continueIndicator != null) continueIndicator.SetActive(false);
        UIFocus.SetNextFrame(acceptButton.gameObject);
    }

    /// <summary>
    /// Hide quest buttons
    /// </summary>
    public void HideQuestButtons()
    {
        if (questButtonContainer != null) questButtonContainer.SetActive(false);
    }

    public bool IsVisible()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

    private void HandleAdvanceInput()
    {
        // Cooldown check
        if (Time.unscaledTime - lastAdvanceTime < ADVANCE_COOLDOWN) return;

        if (isTyping)
        {
            // Currently typing - reveal full text immediately
            CompleteTypewriter();
        }
        else if (lineFullyRevealed)
        {
            // Text fully shown - advance to next line
            lastAdvanceTime = Time.unscaledTime;
            onAdvanceRequested?.Invoke();
        }
    }

    private void HandleAcceptClicked()
    {
        onAcceptQuest?.Invoke();
    }

    private void HandleDeclineClicked()
    {
        onDeclineQuest?.Invoke();
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";
        int charIndex = 0;
        float interval = 1f / charsPerSecond;

        while (charIndex < fullText.Length)
        {
            // Handle rich text tags - reveal entire tag at once
            if (fullText[charIndex] == '<')
            {
                int closeIndex = fullText.IndexOf('>', charIndex);
                if (closeIndex >= 0)
                {
                    charIndex = closeIndex + 1;
                    dialogueText.text = fullText.Substring(0, charIndex);
                    continue;
                }
            }

            charIndex++;
            dialogueText.text = fullText.Substring(0, charIndex);
            yield return new WaitForSecondsRealtime(interval);
        }

        OnTypewriterComplete();
    }

    private void CompleteTypewriter()
    {
        StopTypewriter();

        if (dialogueText != null)
        {
            dialogueText.text = currentFullText;
        }

        OnTypewriterComplete();
    }

    private void OnTypewriterComplete()
    {
        isTyping = false;
        lineFullyRevealed = true;
        lastAdvanceTime = Time.unscaledTime;

        if (continueIndicator != null) continueIndicator.SetActive(true);
        UIFocus.Set(continueIndicator.gameObject);
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        isTyping = false;
    }
}
