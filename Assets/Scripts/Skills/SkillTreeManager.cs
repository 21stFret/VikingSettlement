using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton managing the skill tree system.
/// Unlocked skills persist through succession, XP resets with each new Jarl.
/// </summary>
public class SkillTreeManager : MonoBehaviour, ISaveable
{
    public static SkillTreeManager Instance { get; private set; }

    [Header("Skill Database")]
    [Tooltip("All available skills in the game")]
    [SerializeField] private SkillDefinitionSO[] allSkills;

    [Header("Current State")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private List<string> unlockedSkillIds = new List<string>();

    // Cached effect totals (recalculated when skills change)
    private Dictionary<SkillEffectType, float> cachedEffects = new Dictionary<SkillEffectType, float>();
    private Dictionary<(SkillEffectType, JobType), float> cachedJobEffects = new Dictionary<(SkillEffectType, JobType), float>();
    private Dictionary<(SkillEffectType, ResourceType), float> cachedResourceEffects = new Dictionary<(SkillEffectType, ResourceType), float>();

    // Events
    public event Action<int> OnXPChanged;               // new XP total
    public event Action<SkillDefinitionSO> OnSkillUnlocked;
    public event Action OnSkillsReset;                  // fired on succession

    public int CurrentXP => currentXP;

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

        RecalculateEffects();
    }

    private void Start()
    {
        // Subscribe to Jarl changes to reset XP
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied += OnJarlChanged;
        }
    }

    private void OnDestroy()
    {
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied -= OnJarlChanged;
        }
    }

    #region XP Management

    /// <summary>
    /// Add XP to the current Jarl's pool.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        // Apply XP gain bonus
        float bonus = GetEffect(SkillEffectType.XPGainPercent);
        int finalAmount = Mathf.RoundToInt(amount * (1f + bonus / 100f));

        currentXP += finalAmount;
        OnXPChanged?.Invoke(currentXP);
        Debug.Log($"Gained {finalAmount} XP (base: {amount}). Total: {currentXP}");
    }

    /// <summary>
    /// Reset XP to zero (called on succession).
    /// </summary>
    public void ResetXP()
    {
        currentXP = 0;
        OnXPChanged?.Invoke(currentXP);
        OnSkillsReset?.Invoke();
        Debug.Log("XP reset for new Jarl.");
    }

    private void OnJarlChanged(Villager newJarl)
    {
        ResetXP();
    }

    #endregion

    #region Skill Unlocking

    /// <summary>
    /// Check if a skill can be unlocked (has XP and prerequisites met).
    /// </summary>
    public bool CanUnlockSkill(SkillDefinitionSO skill)
    {
        if (skill == null) return false;
        if (IsSkillUnlocked(skill)) return false;
        if (currentXP < skill.xpCost) return false;

        // Check prerequisites
        if (skill.prerequisites != null)
        {
            foreach (var prereq in skill.prerequisites)
            {
                if (!IsSkillUnlocked(prereq))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Attempt to unlock a skill. Returns true if successful.
    /// </summary>
    public bool TryUnlockSkill(SkillDefinitionSO skill)
    {
        if (!CanUnlockSkill(skill)) return false;

        currentXP -= skill.xpCost;
        unlockedSkillIds.Add(skill.skillId);

        RecalculateEffects();

        OnXPChanged?.Invoke(currentXP);
        OnSkillUnlocked?.Invoke(skill);

        Debug.Log($"Unlocked skill: {skill.skillName}");
        return true;
    }

    /// <summary>
    /// Check if a skill is unlocked.
    /// </summary>
    public bool IsSkillUnlocked(SkillDefinitionSO skill)
    {
        return skill != null && unlockedSkillIds.Contains(skill.skillId);
    }

    /// <summary>
    /// Check if a skill is unlocked by ID.
    /// </summary>
    public bool IsSkillUnlocked(string skillId)
    {
        return unlockedSkillIds.Contains(skillId);
    }

    /// <summary>
    /// Get all unlocked skills.
    /// </summary>
    public List<SkillDefinitionSO> GetUnlockedSkills()
    {
        List<SkillDefinitionSO> unlocked = new List<SkillDefinitionSO>();
        foreach (var skill in allSkills)
        {
            if (IsSkillUnlocked(skill))
                unlocked.Add(skill);
        }
        return unlocked;
    }

    /// <summary>
    /// Get all skills in the database.
    /// </summary>
    public SkillDefinitionSO[] GetAllSkills()
    {
        return allSkills;
    }

    #endregion

    #region Effect Queries

    /// <summary>
    /// Get the total value of an effect type from all unlocked skills.
    /// </summary>
    public float GetEffect(SkillEffectType effectType)
    {
        return cachedEffects.TryGetValue(effectType, out float value) ? value : 0f;
    }

    /// <summary>
    /// Get effect value for a specific job (e.g., gathering speed for Farmer).
    /// </summary>
    public float GetEffectForJob(SkillEffectType effectType, JobType job)
    {
        // Get generic effect + job-specific effect
        float generic = GetEffect(effectType);
        float specific = cachedJobEffects.TryGetValue((effectType, job), out float value) ? value : 0f;
        return generic + specific;
    }

    /// <summary>
    /// Get effect value for a specific resource.
    /// </summary>
    public float GetEffectForResource(SkillEffectType effectType, ResourceType resource)
    {
        float generic = GetEffect(effectType);
        float specific = cachedResourceEffects.TryGetValue((effectType, resource), out float value) ? value : 0f;
        return generic + specific;
    }

    private void RecalculateEffects()
    {
        cachedEffects.Clear();
        cachedJobEffects.Clear();
        cachedResourceEffects.Clear();

        foreach (var skill in allSkills)
        {
            if (!IsSkillUnlocked(skill) || skill.effects == null) continue;

            foreach (var effect in skill.effects)
            {
                // Job-specific effect
                if (effect.specificJob != JobType.None)
                {
                    var key = (effect.effectType, effect.specificJob);
                    if (!cachedJobEffects.ContainsKey(key))
                        cachedJobEffects[key] = 0f;
                    cachedJobEffects[key] += effect.value;
                }
                // Resource-specific effect
                else if (effect.specificResource != ResourceType.None)
                {
                    var key = (effect.effectType, effect.specificResource);
                    if (!cachedResourceEffects.ContainsKey(key))
                        cachedResourceEffects[key] = 0f;
                    cachedResourceEffects[key] += effect.value;
                }
                // Generic effect
                else
                {
                    if (!cachedEffects.ContainsKey(effect.effectType))
                        cachedEffects[effect.effectType] = 0f;
                    cachedEffects[effect.effectType] += effect.value;
                }
            }
        }
    }

    #endregion

    #region ISaveable

    public void PopulateSaveData(SaveData data)
    {
        data.skillTreeData = new SkillTreeSaveData
        {
            currentXP = this.currentXP,
            unlockedSkillIds = this.unlockedSkillIds.ToArray()
        };
    }

    public void LoadSaveData(SaveData data)
    {
        if (data.skillTreeData == null) return;

        currentXP = data.skillTreeData.currentXP;

        unlockedSkillIds.Clear();
        if (data.skillTreeData.unlockedSkillIds != null)
        {
            unlockedSkillIds.AddRange(data.skillTreeData.unlockedSkillIds);
        }

        RecalculateEffects();
        OnXPChanged?.Invoke(currentXP);
    }

    #endregion
}
