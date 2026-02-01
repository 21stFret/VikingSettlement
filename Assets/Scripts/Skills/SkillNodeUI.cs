using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component for a single skill node in the skill tree.
/// </summary>
public class SkillNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private GameObject lockedOverlay;

    [Header("Colors")]
    [SerializeField] private Color unlockedColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color availableColor = new Color(0.9f, 0.9f, 0.3f);
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.3f);

    private SkillDefinitionSO skill;
    private SkillTreeUI treeUI;
    private SkillNodeState currentState;

    public SkillDefinitionSO Skill => skill;
    public SkillNodeState State => currentState;

    public enum SkillNodeState
    {
        Locked,     // Prerequisites not met
        Available,  // Can be unlocked (has prereqs, maybe not XP)
        Unlocked    // Already unlocked
    }

    public void Initialize(SkillDefinitionSO skillDef, SkillTreeUI tree)
    {
        skill = skillDef;
        treeUI = tree;

        if (iconImage != null && skill.icon != null)
        {
            iconImage.sprite = skill.icon;
        }

        UpdateState();
    }

    public void UpdateState()
    {
        if (skill == null) return;

        bool isUnlocked = SkillTreeManager.Instance.IsSkillUnlocked(skill);

        if (isUnlocked)
        {
            currentState = SkillNodeState.Unlocked;
        }
        else if (ArePrerequisitesMet())
        {
            currentState = SkillNodeState.Available;
        }
        else
        {
            currentState = SkillNodeState.Locked;
        }

        UpdateVisuals();
    }

    private bool ArePrerequisitesMet()
    {
        if (skill.prerequisites == null || skill.prerequisites.Length == 0)
            return true;

        foreach (var prereq in skill.prerequisites)
        {
            if (!SkillTreeManager.Instance.IsSkillUnlocked(prereq))
                return false;
        }
        return true;
    }

    private void UpdateVisuals()
    {
        Color bgColor;
        bool showLocked;

        switch (currentState)
        {
            case SkillNodeState.Unlocked:
                bgColor = unlockedColor;
                showLocked = false;
                break;
            case SkillNodeState.Available:
                bgColor = availableColor;
                showLocked = false;
                break;
            default:
                bgColor = lockedColor;
                showLocked = true;
                break;
        }

        if (backgroundImage != null)
            backgroundImage.color = bgColor;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(showLocked);

        if (iconImage != null)
            iconImage.color = showLocked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (borderImage != null)
            borderImage.color = hoverColor;

        if (treeUI != null)
            treeUI.ShowTooltip(skill, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (borderImage != null)
            borderImage.color = Color.clear;

        if (treeUI != null)
            treeUI.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (treeUI != null)
            treeUI.OnNodeClicked(this);
    }
}
