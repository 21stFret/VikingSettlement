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
        public GameObject letterboxTop;
        public GameObject letterboxBottom;

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
        private bool wasGamePaused;

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

            // Pause game
            if (currentCutscene.pauseGame)
            {
                wasGamePaused = Time.timeScale == 0f;
                Time.timeScale = 0f;
            }

            // Hide UI
            if (currentCutscene.hideUI && gameUICanvas != null)
            {
                gameUICanvas.enabled = false;
            }

            // Show letterbox
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

            // Restore pause state
            if (currentCutscene.pauseGame && !wasGamePaused)
            {
                Time.timeScale = 1f;
            }

            // Show UI
            if (currentCutscene.hideUI && gameUICanvas != null)
            {
                gameUICanvas.enabled = true;
            }

            // Hide letterbox
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
            if (letterboxTop != null) letterboxTop.SetActive(show);
            if (letterboxBottom != null) letterboxBottom.SetActive(show);
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
    }
}
