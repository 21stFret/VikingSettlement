using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central manager for Jarl state, succession, and lineage tracking.
/// Handles the death of a Jarl and selection of a new heir.
/// </summary>
public class JarlManager : MonoBehaviour, ISaveable
{
    public static JarlManager Instance { get; private set; }

    [Header("Current Jarl")]
    [SerializeField] private Villager currentJarl;
    public Villager CurrentJarl => currentJarl;

    [Header("Succession Settings")]
    [SerializeField] private float successionDelay = 2f; // Delay before showing succession UI
    [SerializeField] private int maxCandidatesToShow = 5;
    [SerializeField] private bool allowChildrenInSuccession = false;

    [Header("State")]
    [SerializeField] private bool isInSuccession = false;

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    // Events
    public event Action<Villager> OnJarlDied;
    public event Action<Villager> OnJarlChanged;
    public event Action<List<SuccessionCandidate>> OnSuccessionStarted;
    public event Action OnSuccessionEnded;

    // Track the dead Jarl during succession
    private Villager deadJarl;
    private DeathCause lastDeathCause = DeathCause.Other;

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

    public void Init()
    {
        // Find PlayerController if not assigned
        if (playerController == null)
        {
            playerController = PlayerController.Instance;
        }
    }

    /// <summary>
    /// Set a villager as the current Jarl
    /// </summary>
    public void SetJarl(Villager villager, bool isInitial = false)
    {
        if (villager == null)
        {
            Debug.LogError("Cannot set null villager as Jarl!");
            return;
        }

        // Clear previous Jarl's status
        if (currentJarl != null && currentJarl != villager)
        {
            currentJarl.isJarl = false;
            currentJarl.currentJob = JobType.None;
            currentJarl.OnJarlStatusChanged(false);

            // Switch previous Jarl back to standard villager AI
            var prevJarlAI = currentJarl.GetComponent<JarlAI>();
            if (prevJarlAI != null) prevJarlAI.enabled = false;
            var prevVillagerAI = currentJarl.GetComponent<VillagerAI>();
            if (prevVillagerAI != null) prevVillagerAI.enabled = true;
        }

        // Set new Jarl
        currentJarl = villager;
        villager.isJarl = true;
        villager.isOfJarlLineage = true;
        villager.generationsFromJarl = 0;
        villager.currentJob = JobType.Jarl;
        villager.OnJarlStatusChanged(true);

        // Switch new Jarl to dedicated JarlAI
        var newVillagerAI = villager.GetComponent<VillagerAI>();
        if (newVillagerAI != null) newVillagerAI.enabled = false;
        var jarlAI = villager.GetComponent<JarlAI>();
        if (jarlAI != null) jarlAI.enabled = true;

        // Transfer player control (PlayerController will call SetAIEnabled(false) on JarlAI)
        if (playerController != null)
        {
            playerController.SetControlTarget(villager);
        }

        // Update camera
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerTarget(villager.transform);
        }

        WorldInteractionZone interactionZone = villager.GetComponentInChildren<WorldInteractionZone>(true);
        if (interactionZone != null) 
        {
            interactionZone.gameObject.SetActive(true);
        }


        Debug.Log($"{villager.villagerName} is now the Jarl!");

        AttackCooldownUI.Instance?.Init(); // Ensure UI is initialized for new Jarl
        DodgeCooldownUI.Instance?.Init();

        if (!isInitial)
        {
            OnJarlChanged?.Invoke(villager);
        }
    }

    /// <summary>
    /// Called when the current Jarl dies
    /// </summary>
    public void OnCurrentJarlDied(DeathCause cause = DeathCause.Other)
    {
        if (isInSuccession)
        {
            Debug.LogWarning("Already in succession process!");
            return;
        }

        deadJarl = currentJarl;
        lastDeathCause = cause;
        Debug.Log($"The Jarl {deadJarl.villagerName} has fallen! Cause: {cause}");

        // Fire death event
        OnJarlDied?.Invoke(deadJarl);

        // Start succession process
        StartCoroutine(SuccessionProcess());
    }

    /// <summary>
    /// Coroutine handling the succession process
    /// </summary>
    private IEnumerator SuccessionProcess()
    {
        isInSuccession = true;

        // Optional: Pause game
        // Time.timeScale = 0f;

        // Wait for dramatic effect
        yield return new WaitForSeconds(successionDelay);

        // Get succession candidates
        var candidates = GetSuccessionCandidates();

        if (candidates.Count == 0)
        {
            // No viable candidates - game over or emergency selection
            HandleNoHeirs();
            yield break;
        }

        // Fire succession started event - UI should respond
        OnSuccessionStarted?.Invoke(candidates);

        // Wait for player selection — 120 s real-time fallback in case UI never fires
        float elapsed = 0f;
        while (isInSuccession && elapsed < 120f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Timeout: auto-select best candidate so the game never hard-locks
        if (isInSuccession && candidates.Count > 0)
        {
            Debug.LogWarning("Succession timeout — auto-selecting best candidate");
            SelectHeir(candidates[0].Villager);
        }
    }

    /// <summary>
    /// Calculate and return ranked succession candidates
    /// </summary>
    public List<SuccessionCandidate> GetSuccessionCandidates()
    {
        var candidates = new List<SuccessionCandidate>();

        if (SettlementManager.Instance == null) return candidates;

        var allVillagers = SettlementManager.Instance.GetAllVillagers();

        // Filter to eligible candidates
        var eligible = allVillagers
            .Where(v => v != deadJarl &&
                        !v.IsDead() &&
                        (v.currentLifeStage == LifeStage.Mature ||
                         (allowChildrenInSuccession && v.currentLifeStage == LifeStage.Young)))
            .ToList();

        foreach (var villager in eligible)
        {
            var candidate = new SuccessionCandidate
            {
                Villager = villager
            };

            // Determine relationship priority
            candidate.Priority = DetermineRelationshipPriority(villager);
            candidate.RelationshipDistance = CalculateRelationshipDistance(villager);

            // Calculate score components
            candidate.CombatSkillBonus = villager.skills.combat * 10f;
            candidate.AgeBonus = CalculateAgeBonus(villager.age);
            candidate.LineageBonus = villager.isOfJarlLineage ? 50f : 0f;

            // Total score
            candidate.SuccessionScore = candidate.CombatSkillBonus +
                                        candidate.AgeBonus +
                                        candidate.LineageBonus;

            candidates.Add(candidate);
        }

        // Sort by priority first, then by score within same priority
        return candidates
            .OrderBy(c => (int)c.Priority)
            .ThenByDescending(c => c.SuccessionScore)
            .Take(maxCandidatesToShow)
            .ToList();
    }

    /// <summary>
    /// Determine the relationship priority of a candidate to the dead Jarl
    /// </summary>
    private SuccessionPriority DetermineRelationshipPriority(Villager candidate)
    {
        if (deadJarl == null) return SuccessionPriority.UnrelatedHighSkill;

        // Direct children
        if (candidate.parent1 == deadJarl || candidate.parent2 == deadJarl)
        {
            return SuccessionPriority.DirectChild;
        }

        // Siblings (share a parent with the dead Jarl)
        if (SharesParent(candidate, deadJarl))
        {
            return SuccessionPriority.Sibling;
        }

        // Grandchildren (child of Jarl's children)
        var jarlChildren = deadJarl.GetChildren();
        foreach (var child in jarlChildren)
        {
            if (candidate.parent1 == child || candidate.parent2 == child)
            {
                return SuccessionPriority.Grandchild;
            }
        }

        // Distant relative (in Jarl lineage)
        if (candidate.isOfJarlLineage)
        {
            return SuccessionPriority.DistantRelative;
        }

        // Unrelated
        return SuccessionPriority.UnrelatedHighSkill;
    }

    /// <summary>
    /// Check if two villagers share a parent
    /// </summary>
    private bool SharesParent(Villager a, Villager b)
    {
        if (a.parent1 != null && (a.parent1 == b.parent1 || a.parent1 == b.parent2))
            return true;
        if (a.parent2 != null && (a.parent2 == b.parent1 || a.parent2 == b.parent2))
            return true;
        return false;
    }

    /// <summary>
    /// Calculate relationship distance (lower = closer)
    /// </summary>
    private int CalculateRelationshipDistance(Villager candidate)
    {
        return candidate.generationsFromJarl >= 0 ? candidate.generationsFromJarl : 99;
    }

    /// <summary>
    /// Calculate age bonus - peaks at 25-40
    /// </summary>
    private float CalculateAgeBonus(float age)
    {
        if (age < 18f) return 0f; // Too young
        if (age >= 18f && age <= 25f) return (age - 18f) * 2f; // Ramping up
        if (age > 25f && age <= 40f) return 20f; // Peak
        if (age > 40f && age <= 60f) return 20f - (age - 40f) * 0.5f; // Declining
        return 10f; // Elder
    }

    /// <summary>
    /// Player or system selects an heir
    /// </summary>
    public void SelectHeir(Villager heir)
    {
        if (!isInSuccession)
        {
            Debug.LogWarning("Not in succession - cannot select heir!");
            return;
        }

        if (heir == null || heir.IsDead())
        {
            Debug.LogError("Invalid heir selection!");
            return;
        }

        Debug.Log($"{heir.villagerName} has been chosen as the new Jarl!");

        // Set the new Jarl
        SetJarl(heir);

        // End succession state
        isInSuccession = false;

        // Apply death type buff
        if (DeathTypeBuff.Instance != null)
        {
            DeathTypeBuff.Instance.ApplyBuff(lastDeathCause);
        }

        // Start runestone selection if manager exists
        // Use (object) cast to bypass Unity's fake-null on destroyed GameObjects —
        // the C# object and its fields (skills, name) are still valid
        if (RunestoneManager.Instance != null && (object)deadJarl != null)
        {
            // Defer OnSuccessionEnded until runestone selection completes
            RunestoneManager.Instance.OnSelectionComplete += OnRunestoneSelectionComplete;
            RunestoneManager.Instance.StartSelection(deadJarl);
        }
        else
        {
            // No runestone manager - fire succession ended immediately
            deadJarl = null;
            OnSuccessionEnded?.Invoke();
        }
    }

    private void OnRunestoneSelectionComplete()
    {
        if (RunestoneManager.Instance != null)
        {
            RunestoneManager.Instance.OnSelectionComplete -= OnRunestoneSelectionComplete;
        }

        // Re-apply skill bonuses to ALL villagers so the newly-selected runestone
        // (e.g. Resilient Blood) takes effect immediately on everyone, including the new Jarl.
        if (SettlementManager.Instance != null)
        {
            foreach (var villager in SettlementManager.Instance.GetAllVillagers())
            {
                villager.ApplySkillBonuses();
            }
        }

        deadJarl = null;
        OnSuccessionEnded?.Invoke();
    }

    /// <summary>
    /// Handle case where no valid heirs exist
    /// </summary>
    private void HandleNoHeirs()
    {
        Debug.LogWarning("No valid heirs found!");

        if (SettlementManager.Instance == null)
        {
            TriggerGameOver();
            return;
        }

        var allVillagers = SettlementManager.Instance.GetAllVillagers();

        // Try to find any mature villager
        var anyMature = allVillagers
            .Where(v => v != deadJarl &&
                        v.currentLifeStage == LifeStage.Mature &&
                        !v.IsDead())
            .OrderByDescending(v => v.GetTotalSkillScore())
            .FirstOrDefault();

        if (anyMature != null)
        {
            Debug.Log($"No relatives found - {anyMature.villagerName} rises to lead!");
            SelectHeir(anyMature);
        }
        else
        {
            // Check for young villagers (future heirs)
            var anyYoung = allVillagers
                .Where(v => v.currentLifeStage == LifeStage.Young && !v.IsDead())
                .FirstOrDefault();

            if (anyYoung != null)
            {
                // TODO: Implement regent system
                Debug.Log("Only children remain - the settlement awaits a new leader...");
                isInSuccession = false;
                OnSuccessionEnded?.Invoke();
            }
            else
            {
                TriggerGameOver();
            }
        }
    }

    /// <summary>
    /// Trigger game over state
    /// </summary>
    private void TriggerGameOver()
    {
        Debug.Log("The settlement has no one left to lead. Game Over.");
        isInSuccession = false;
        GameOverScreen.Instance?.Show();
    }

    /// <summary>
    /// Check if a villager is of Jarl lineage
    /// </summary>
    public bool IsOfJarlLineage(Villager villager)
    {
        return villager != null && villager.isOfJarlLineage;
    }

    /// <summary>
    /// Get the current Jarl
    /// </summary>
    public Villager GetCurrentJarl()
    {
        return currentJarl;
    }

    /// <summary>
    /// Set the initial Jarl without firing the OnJarlChanged event.
    /// </summary>
    public void SetInitialJarl(Villager villager)
    {
        SetJarl(villager, isInitial: true);
    }

    /// <summary>
    /// For a brand-new game: find whichever villager is already flagged as Jarl, or promote
    /// the first villager. Called by Bootstrap after villagers have been spawned and
    /// JarlManager.Init() has wired up PlayerController.
    /// </summary>
    public void EnsureInitialJarl()
    {
        if (currentJarl != null) return;
        if (SettlementManager.Instance == null) return;

        var villagers = SettlementManager.Instance.GetAllVillagers();

        foreach (var v in villagers)
        {
            if (v != null && v.isJarl)
            {
                SetInitialJarl(v);
                return;
            }
        }

        if (villagers.Count > 0 && villagers[0] != null)
        {
            Villager newJarl = villagers[0];
            newJarl.isJarl = true;
            newJarl.isOfJarlLineage = true;
            newJarl.generationsFromJarl = 0;
            newJarl.age = 20;
            SetInitialJarl(newJarl);
            Debug.Log($"JarlManager: Assigned {newJarl.villagerName} as initial Jarl");
        }
    }

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        data.currentJarlId = currentJarl != null ? currentJarl.uniqueId : "";
    }

    public void LoadSaveData(SaveData data)
    {
        if (string.IsNullOrEmpty(data.currentJarlId)) return;

        if (SettlementManager.Instance == null) return;

        var allVillagers = SettlementManager.Instance.GetAllVillagers();
        foreach (var v in allVillagers)
        {
            if (v.uniqueId == data.currentJarlId)
            {
                SetJarl(v, isInitial: true);
                break;
            }
        }
    }

    #endregion
}
