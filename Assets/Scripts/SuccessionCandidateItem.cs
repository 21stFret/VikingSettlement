using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual candidate item in the succession selection UI
/// </summary>
public class SuccessionCandidateItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI relationshipText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI ageText;
    [SerializeField] private TextMeshProUGUI skillsText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject recommendedBadge;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color recommendedColor = new Color(0.3f, 0.4f, 0.2f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

    private SuccessionCandidate candidate;
    private SuccessionUI parentUI;
    private bool isRecommended;

    /// <summary>
    /// Setup the candidate item with data
    /// </summary>
    public void Setup(SuccessionCandidate candidateData, SuccessionUI ui, bool recommended = false)
    {
        candidate = candidateData;
        parentUI = ui;
        isRecommended = recommended;

        // Set name
        if (nameText != null)
        {
            nameText.text = candidate.Villager.villagerName;
        }

        // Set relationship
        if (relationshipText != null)
        {
            relationshipText.text = candidate.GetRelationshipText();
        }

        // Set score
        if (scoreText != null)
        {
            scoreText.text = $"Score: {candidate.SuccessionScore:F0}";
        }

        // Set age
        if (ageText != null)
        {
            ageText.text = $"Age: {candidate.Villager.age:F0}";
        }

        // Set skills summary
        if (skillsText != null)
        {
            var skills = candidate.Villager.skills;
            skillsText.text = $"Combat: {skills.combat:F1}";
        }

        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = isRecommended ? recommendedColor : normalColor;
        }

        // Show/hide recommended badge
        if (recommendedBadge != null)
        {
            recommendedBadge.SetActive(isRecommended);
        }

        // Setup button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }

    /// <summary>
    /// Called when select button is clicked
    /// </summary>
    private void OnSelectClicked()
    {
        if (parentUI != null && candidate != null)
        {
            parentUI.OnCandidateSelected(candidate);
        }
    }

    /// <summary>
    /// Pointer enter - highlight
    /// </summary>
    public void OnPointerEnter()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = hoverColor;
        }
    }

    /// <summary>
    /// Pointer exit - restore color
    /// </summary>
    public void OnPointerExit()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isRecommended ? recommendedColor : normalColor;
        }
    }
}
