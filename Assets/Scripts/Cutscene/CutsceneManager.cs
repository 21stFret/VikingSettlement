using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Cutscenes
{
    /// <summary>
    /// Manages playing cutscenes - sequences of actions like camera moves, dialogue, actor movement.
    /// Actors not involved in the cutscene continue their normal behavior.
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        public static CutsceneManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private string currentCutsceneId;

        [Header("References")]
        [Tooltip("UI canvas to hide during cutscenes (optional)")]
        public Canvas gameUICanvas;

        [Tooltip("Letterbox bars for cinematic feel (optional)")]
        public RectTransform letterboxTop;
        public RectTransform letterboxBottom;

        [Header("Letterbox Animation")]
        [Tooltip("How far off-screen the letterbox bars start")]
        public float letterboxOffscreenOffset = 100f;
        [Tooltip("How long the letterbox takes to slide in/out")]
        public float letterboxAnimDuration = 0.5f;

        [Header("Testing")]
        [Tooltip("Cutscene to play when testing")]
        public CutsceneSO testCutscene;

        [InspectorButton("PlayTestCutscene", ButtonWidth = 150)]
        public bool playTestButton;

        [InspectorButton("StopCurrentCutscene", ButtonWidth = 150)]
        public bool stopButton;

        // Events
        public event Action<CutsceneSO> OnCutsceneStarted;
        public event Action<CutsceneSO> OnCutsceneEnded;
        public event Action<CutsceneAction> OnActionStarted;
        public event Action<CutsceneAction> OnActionEnded;

        // Runtime state
        private CutsceneSO currentCutscene;
        private int currentActionIndex;
        private CutsceneAction currentAction;
        private List<CutsceneAction> runningActions = new List<CutsceneAction>();
        private HashSet<Villager> overriddenVillagers = new HashSet<Villager>();
        private bool wasPlayerControlEnabled;
        private bool wasGameClockPaused;

        // Letterbox animation
        private Vector2 letterboxTopTarget;
        private Vector2 letterboxBottomTarget;
        private Vector2 letterboxTopHidden;
        private Vector2 letterboxBottomHidden;
        private Coroutine letterboxCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            letterboxTopTarget = letterboxTop.anchoredPosition;
            letterboxBottomTarget = letterboxBottom.anchoredPosition;
            letterboxTopHidden = new Vector2(letterboxTopTarget.x, letterboxTop.rect.height + letterboxOffscreenOffset);
            letterboxBottomHidden = new Vector2(letterboxBottomTarget.x, -letterboxBottom.rect.height - letterboxOffscreenOffset);
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Update all running actions
            for (int i = runningActions.Count - 1; i >= 0; i--)
            {
                var action = runningActions[i];
                if (action.Update(this))
                {
                    // Action completed
                    OnActionEnded?.Invoke(action);
                    runningActions.RemoveAt(i);
                }
            }

            // If current action is done and was blocking, advance
            if (currentAction != null && currentAction.waitForCompletion)
            {
                if (!runningActions.Contains(currentAction))
                {
                    // Current blocking action finished, advance to next
                    currentAction = null;
                    AdvanceToNextAction();
                }
            }
        }

        /// <summary>
        /// Play a cutscene
        /// </summary>
        public void PlayCutscene(CutsceneSO cutscene)
        {
            if (cutscene == null)
            {
                Debug.LogError("CutsceneManager: Cannot play null cutscene!");
                return;
            }

            if (isPlaying)
            {
                Debug.LogWarning($"CutsceneManager: Already playing '{currentCutsceneId}', stopping it first.");
                StopCutscene();
            }

            currentCutscene = cutscene;
            currentCutsceneId = cutscene.cutsceneId;
            currentActionIndex = -1;
            currentAction = null;
            runningActions.Clear();
            isPlaying = true;

            // Setup cutscene state
            SetupCutsceneState();

            Debug.Log($"CutsceneManager: Starting cutscene '{cutscene.displayName}'");
            OnCutsceneStarted?.Invoke(cutscene);

            // Start first action
            AdvanceToNextAction();
        }

        /// <summary>
        /// Stop the current cutscene
        /// </summary>
        public void StopCutscene()
        {
            if (!isPlaying) return;

            // Cancel all running actions
            foreach (var action in runningActions)
            {
                action.Cancel(this);
            }
            runningActions.Clear();

            // Restore state
            RestoreCutsceneState();

            var endedCutscene = currentCutscene;
            currentCutscene = null;
            currentCutsceneId = null;
            currentAction = null;
            isPlaying = false;

            Debug.Log("CutsceneManager: Cutscene stopped");
            OnCutsceneEnded?.Invoke(endedCutscene);
        }

        /// <summary>
        /// Skip to the end of the current cutscene
        /// </summary>
        public void SkipCutscene()
        {
            StopCutscene();
        }

        private void AdvanceToNextAction()
        {
            currentActionIndex++;

            if (currentActionIndex >= currentCutscene.actions.Count)
            {
                // Cutscene complete
                CompleteCutscene();
                return;
            }

            currentAction = currentCutscene.actions[currentActionIndex];

            if (currentAction == null)
            {
                // Skip null actions
                AdvanceToNextAction();
                return;
            }

            OnActionStarted?.Invoke(currentAction);

            bool completedImmediately = currentAction.Execute(this);

            if (!completedImmediately)
            {
                runningActions.Add(currentAction);
            }
            else
            {
                OnActionEnded?.Invoke(currentAction);
            }

            // If this action doesn't block, immediately start next
            if (!currentAction.waitForCompletion || completedImmediately)
            {
                currentAction = null;
                AdvanceToNextAction();
            }
        }

        private void CompleteCutscene()
        {
            RestoreCutsceneState();

            var completedCutscene = currentCutscene;
            currentCutscene = null;
            currentCutsceneId = null;
            currentAction = null;
            isPlaying = false;

            Debug.Log($"CutsceneManager: Cutscene '{completedCutscene.displayName}' completed");
            OnCutsceneEnded?.Invoke(completedCutscene);
        }

        private void SetupCutsceneState()
        {
            if (currentCutscene == null) return;

            // Disable player control
            if (currentCutscene.disablePlayerControl && PlayerController.Instance != null)
            {
                wasPlayerControlEnabled = PlayerController.Instance.enabled;
                PlayerController.Instance.enabled = false;
            }

            // Pause game clock (not Time.timeScale - we want animations to continue)
            if (currentCutscene.pauseGameClock && GameTickManager.Instance != null)
            {
                wasGameClockPaused = GameTickManager.Instance.IsPaused;
                GameTickManager.Instance.SetPaused(true);
            }

            // Hide UI
            if (currentCutscene.hideUI && gameUICanvas != null)
            {
                gameUICanvas.enabled = false;
            }

            // Show letterbox with animation
            ShowLetterbox(true);
        }

        private void RestoreCutsceneState()
        {
            if (currentCutscene == null) return;

            // Restore player control
            if (currentCutscene.disablePlayerControl && PlayerController.Instance != null)
            {
                PlayerController.Instance.enabled = wasPlayerControlEnabled;
            }

            // Restore game clock pause state
            if (currentCutscene.pauseGameClock && GameTickManager.Instance != null && !wasGameClockPaused)
            {
                GameTickManager.Instance.SetPaused(false);
            }

            // Show UI
            if (currentCutscene.hideUI && gameUICanvas != null)
            {
                gameUICanvas.enabled = true;
            }

            // Hide letterbox with animation
            ShowLetterbox(false);

            // Resume auto weather
            if (currentCutscene.resumeAutoWeather && WeatherManager.Instance != null)
            {
                WeatherManager.Instance.ResumeAutoWeather();
            }

            // Release all overridden villagers
            foreach (var villager in overriddenVillagers)
            {
                if (villager != null)
                {
                    var ai = villager.GetComponent<VillagerAI>();
                    if (ai != null)
                    {
                        ai.ClearCutsceneTarget();
                    }
                }
            }
            overriddenVillagers.Clear();
        }

        private void ShowLetterbox(bool show)
        {
            if (letterboxTop == null && letterboxBottom == null) return;

            // Stop any existing animation
            if (letterboxCoroutine != null)
            {
                StopCoroutine(letterboxCoroutine);
            }

            letterboxCoroutine = StartCoroutine(AnimateLetterbox(show));
        }

        private IEnumerator AnimateLetterbox(bool show)
        {
            // Set starting positions and activate
            if (show)
            {
                if (letterboxTop != null)
                {
                    letterboxTop.gameObject.SetActive(true);
                    letterboxTop.anchoredPosition = letterboxTopHidden;
                }
                if (letterboxBottom != null)
                {
                    letterboxBottom.gameObject.SetActive(true);
                    letterboxBottom.anchoredPosition = letterboxBottomHidden;
                }
            }

            // Animate
            float elapsed = 0f;
            while (elapsed < letterboxAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / letterboxAnimDuration);

                if (show)
                {
                    // Slide in
                    if (letterboxTop != null)
                        letterboxTop.anchoredPosition = Vector2.Lerp(letterboxTopHidden, letterboxTopTarget, t);
                    if (letterboxBottom != null)
                        letterboxBottom.anchoredPosition = Vector2.Lerp(letterboxBottomHidden, letterboxBottomTarget, t);
                }
                else
                {
                    // Slide out
                    if (letterboxTop != null)
                        letterboxTop.anchoredPosition = Vector2.Lerp(letterboxTopTarget, letterboxTopHidden, t);
                    if (letterboxBottom != null)
                        letterboxBottom.anchoredPosition = Vector2.Lerp(letterboxBottomTarget, letterboxBottomHidden, t);
                }

                yield return null;
            }

            // Ensure final positions and deactivate if hiding
            if (show)
            {
                if (letterboxTop != null) letterboxTop.anchoredPosition = letterboxTopTarget;
                if (letterboxBottom != null) letterboxBottom.anchoredPosition = letterboxBottomTarget;
            }
            else
            {
                if (letterboxTop != null)
                {
                    letterboxTop.anchoredPosition = letterboxTopHidden;
                    letterboxTop.gameObject.SetActive(false);
                }
                if (letterboxBottom != null)
                {
                    letterboxBottom.anchoredPosition = letterboxBottomHidden;
                    letterboxBottom.gameObject.SetActive(false);
                }
            }

            letterboxCoroutine = null;
        }

        /// <summary>
        /// Register a villager as being controlled by the cutscene
        /// </summary>
        public void RegisterOverriddenVillager(Villager villager)
        {
            if (villager != null)
            {
                overriddenVillagers.Add(villager);
            }
        }

        /// <summary>
        /// Check if a cutscene is currently playing
        /// </summary>
        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Get the current cutscene ID
        /// </summary>
        public string CurrentCutsceneId => currentCutsceneId;

        #region Testing

        private void PlayTestCutscene()
        {
            if (testCutscene == null)
            {
                Debug.LogWarning("CutsceneManager: No test cutscene assigned!");
                return;
            }

            if (isPlaying)
            {
                Debug.Log("CutsceneManager: Stopping current cutscene first...");
                StopCutscene();
            }

            Debug.Log($"CutsceneManager: Playing test cutscene '{testCutscene.displayName}'");
            PlayCutscene(testCutscene);
        }

        private void StopCurrentCutscene()
        {
            if (!isPlaying)
            {
                Debug.Log("CutsceneManager: No cutscene is playing");
                return;
            }

            Debug.Log("CutsceneManager: Stopping cutscene...");
            StopCutscene();
        }

        #endregion
    }
}
