using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main skill tree UI panel.
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject skillTreePanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.K;

    [Header("Node Spawning")]
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private GameObject skillNodePrefab;
    [SerializeField] private float nodeScale = 1f;
    [SerializeField] private Vector2 treeOffset = Vector2.zero;

    [Header("Connection Lines")]
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Color unlockedLineColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color lockedLineColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;
    [SerializeField] private TMP_Text tooltipCost;
    [SerializeField] private TMP_Text tooltipEffects;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TMP_Text unlockButtonText;

    [Header("XP Display")]
    [SerializeField] private TMP_Text xpText;

    [Header("Category Filters")]
    [SerializeField] private Toggle combatToggle;
    [SerializeField] private Toggle passiveToggle;
    [SerializeField] private Toggle gatheringToggle;

    private Dictionary<string, SkillNodeUI> nodeMap = new Dictionary<string, SkillNodeUI>();
    private List<Image> connectionLines = new List<Image>();
    private SkillNodeUI selectedNode;
    private bool isInitialized = false;

    private void Start()
    {
        if (skillTreePanel != null)
            skillTreePanel.SetActive(false);

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnUnlockClicked);

        // Subscribe to events
        if (SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnXPChanged += UpdateXPDisplay;
            SkillTreeManager.Instance.OnSkillUnlocked += OnSkillUnlocked;
        }

        // Setup filter toggles
        SetupFilterToggles();
    }

    private void OnDestroy()
    {
        if (SkillTreeManager.Instance != null)
        {
            SkillTreeManager.Instance.OnXPChanged -= UpdateXPDisplay;
            SkillTreeManager.Instance.OnSkillUnlocked -= OnSkillUnlocked;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleSkillTree();
        }
    }

    public void ToggleSkillTree()
    {
        if (skillTreePanel == null) return;

        bool show = !skillTreePanel.activeSelf;
        skillTreePanel.SetActive(show);

        if (show)
        {
            if (!isInitialized)
            {
                BuildTree();
                isInitialized = true;
            }
            else
            {
                RefreshAllNodes();
            }
            UpdateXPDisplay(SkillTreeManager.Instance.CurrentXP);

            // Pause game while skill tree is open
            if (PauseManager.Instance != null)
                PauseManager.Instance.EnterMenuPause();
        }
        else
        {
            HideTooltip();
            if (PauseManager.Instance != null)
                PauseManager.Instance.ExitMenuPause();
        }
    }

    private void BuildTree()
    {
        if (SkillTreeManager.Instance == null || skillNodePrefab == null || nodeContainer == null)
            return;

        // Clear existing
        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);
        nodeMap.Clear();

        foreach (Transform child in lineContainer)
            Destroy(child.gameObject);
        connectionLines.Clear();

        // Create nodes
        var allSkills = SkillTreeManager.Instance.GetAllSkills();
        foreach (var skill in allSkills)
        {
            CreateNode(skill);
        }

        // Create connection lines
        foreach (var skill in allSkills)
        {
            if (skill.prerequisites == null) continue;

            foreach (var prereq in skill.prerequisites)
            {
                CreateConnectionLine(prereq, skill);
            }
        }

        UpdateConnectionLineColors();
    }

    private void CreateNode(SkillDefinitionSO skill)
    {
        GameObject nodeObj = Instantiate(skillNodePrefab, nodeContainer);
        RectTransform rect = nodeObj.GetComponent<RectTransform>();

        // Position based on skill's treePosition
        rect.anchoredPosition = (skill.treePosition * nodeScale) + treeOffset;

        SkillNodeUI nodeUI = nodeObj.GetComponent<SkillNodeUI>();
        if (nodeUI != null)
        {
            nodeUI.Initialize(skill, this);
            nodeMap[skill.skillId] = nodeUI;
        }
    }

    private void CreateConnectionLine(SkillDefinitionSO from, SkillDefinitionSO to)
    {
        if (linePrefab == null || lineContainer == null) return;

        GameObject lineObj = Instantiate(linePrefab, lineContainer);
        Image lineImage = lineObj.GetComponent<Image>();

        if (lineImage != null)
        {
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();

            Vector2 startPos = (from.treePosition * nodeScale) + treeOffset;
            Vector2 endPos = (to.treePosition * nodeScale) + treeOffset;

            // Position line at midpoint
            Vector2 midpoint = (startPos + endPos) / 2f;
            lineRect.anchoredPosition = midpoint;

            // Calculate length and rotation
            float distance = Vector2.Distance(startPos, endPos);
            float angle = Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x) * Mathf.Rad2Deg;

            lineRect.sizeDelta = new Vector2(distance, 4f);
            lineRect.rotation = Quaternion.Euler(0, 0, angle);

            connectionLines.Add(lineImage);
        }
    }

    private void UpdateConnectionLineColors()
    {
        // This is simplified - ideally track which line goes to which skills
        // For now just use a general "some unlocked" color
        bool anyUnlocked = SkillTreeManager.Instance.GetUnlockedSkills().Count > 0;
        foreach (var line in connectionLines)
        {
            line.color = anyUnlocked ? unlockedLineColor : lockedLineColor;
        }
    }

    private void RefreshAllNodes()
    {
        foreach (var node in nodeMap.Values)
        {
            node.UpdateState();
        }
        UpdateConnectionLineColors();
    }

    #region Tooltip

    public void ShowTooltip(SkillDefinitionSO skill, SkillNodeUI node)
    {
        if (tooltipPanel == null || skill == null) return;

        selectedNode = node;
        tooltipPanel.SetActive(true);

        if (tooltipTitle != null)
            tooltipTitle.text = skill.skillName;

        if (tooltipDescription != null)
            tooltipDescription.text = skill.description;

        if (tooltipCost != null)
            tooltipCost.text = $"Cost: {skill.xpCost} XP";

        if (tooltipEffects != null)
            tooltipEffects.text = GetEffectsText(skill);

        UpdateUnlockButton(skill, node);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        selectedNode = null;
    }

    private string GetEffectsText(SkillDefinitionSO skill)
    {
        if (skill.effects == null || skill.effects.Length == 0)
            return "No effects";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var effect in skill.effects)
        {
            if (sb.Length > 0) sb.Append("\n");
            sb.Append(FormatEffect(effect));
        }
        return sb.ToString();
    }

    private string FormatEffect(SkillEffect effect)
    {
        string sign = effect.value >= 0 ? "+" : "";
        string suffix = effect.effectType.ToString().Contains("Percent") ? "%" : "";

        string effectName = effect.effectType switch
        {
            SkillEffectType.DamagePercent => "Damage",
            SkillEffectType.AttackSpeedPercent => "Attack Speed",
            SkillEffectType.DefenseFlat => "Defense",
            SkillEffectType.DefensePercent => "Defense",
            SkillEffectType.MaxHealthFlat => "Max Health",
            SkillEffectType.MaxHealthPercent => "Max Health",
            SkillEffectType.LifeSteal => "Life Steal",
            SkillEffectType.CriticalChance => "Crit Chance",
            SkillEffectType.MoveSpeedPercent => "Move Speed",
            SkillEffectType.XPGainPercent => "XP Gain",
            SkillEffectType.MoraleDecayReduction => "Morale Decay",
            SkillEffectType.GatheringSpeedPercent => "Gathering Speed",
            SkillEffectType.GatheringYieldPercent => "Gathering Yield",
            SkillEffectType.ResourceCapacityPercent => "Storage Capacity",
            _ => effect.effectType.ToString()
        };

        string qualifier = "";
        if (effect.specificJob != JobType.None)
            qualifier = $" ({effect.specificJob})";
        else if (effect.specificResource != ResourceType.None)
            qualifier = $" ({effect.specificResource})";

        return $"{sign}{effect.value}{suffix} {effectName}{qualifier}";
    }

    private void UpdateUnlockButton(SkillDefinitionSO skill, SkillNodeUI node)
    {
        if (unlockButton == null) return;

        bool isUnlocked = SkillTreeManager.Instance.IsSkillUnlocked(skill);
        bool canUnlock = SkillTreeManager.Instance.CanUnlockSkill(skill);

        unlockButton.gameObject.SetActive(!isUnlocked);
        unlockButton.interactable = canUnlock;

        if (unlockButtonText != null)
        {
            if (isUnlocked)
                unlockButtonText.text = "Unlocked";
            else if (canUnlock)
                unlockButtonText.text = "Unlock";
            else if (SkillTreeManager.Instance.CurrentXP < skill.xpCost)
                unlockButtonText.text = "Not Enough XP";
            else
                unlockButtonText.text = "Locked";
        }
    }

    #endregion

    #region Actions

    public void OnNodeClicked(SkillNodeUI node)
    {
        ShowTooltip(node.Skill, node);
    }

    private void OnUnlockClicked()
    {
        if (selectedNode == null) return;

        if (SkillTreeManager.Instance.TryUnlockSkill(selectedNode.Skill))
        {
            RefreshAllNodes();
            ShowTooltip(selectedNode.Skill, selectedNode);
        }
    }

    private void OnSkillUnlocked(SkillDefinitionSO skill)
    {
        RefreshAllNodes();
    }

    private void UpdateXPDisplay(int xp)
    {
        if (xpText != null)
            xpText.text = $"XP: {xp}";
    }

    #endregion

    #region Filters

    private void SetupFilterToggles()
    {
        if (combatToggle != null)
            combatToggle.onValueChanged.AddListener(_ => ApplyFilters());
        if (passiveToggle != null)
            passiveToggle.onValueChanged.AddListener(_ => ApplyFilters());
        if (gatheringToggle != null)
            gatheringToggle.onValueChanged.AddListener(_ => ApplyFilters());
    }

    private void ApplyFilters()
    {
        bool showCombat = combatToggle == null || combatToggle.isOn;
        bool showPassive = passiveToggle == null || passiveToggle.isOn;
        bool showGathering = gatheringToggle == null || gatheringToggle.isOn;

        foreach (var kvp in nodeMap)
        {
            var skill = kvp.Value.Skill;
            bool visible = skill.skillType switch
            {
                SkillType.Combat => showCombat,
                SkillType.Passive => showPassive,
                SkillType.ResourceGathering => showGathering,
                _ => true
            };
            kvp.Value.gameObject.SetActive(visible);
        }
    }

    #endregion
}
