using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel for Jarl succession - displays death notification and heir selection
/// </summary>
public class SuccessionUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject successionPanel;
    [SerializeField] private Transform candidateContainer;
    [SerializeField] private GameObject candidateItemPrefab;

    [Header("Death Notification")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI deathMessageText;

    private List<SuccessionCandidateItem> candidateItems = new List<SuccessionCandidateItem>();
    private List<SuccessionCandidate> currentCandidates;
    private float autoSelectTimer;
    private bool isShowing = false;

    private void Awake()
    {
        // Hide panel on start
        if (successionPanel != null)
        {
            successionPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Subscribe to JarlManager events
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied += OnJarlDied;
            JarlManager.Instance.OnSuccessionStarted += ShowSuccessionUI;
            JarlManager.Instance.OnSuccessionEnded += HideSuccessionUI;
        }
    }

    private void OnDestroy()
    {
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.OnJarlDied -= OnJarlDied;
            JarlManager.Instance.OnSuccessionStarted -= ShowSuccessionUI;
            JarlManager.Instance.OnSuccessionEnded -= HideSuccessionUI;
        }
    }

    private void Update()
    {
        if (!isShowing) return;
    }

    /// <summary>
    /// Called when the Jarl dies - show death message
    /// </summary>
    private void OnJarlDied(Villager deadJarl)
    {
        if (deathMessageText != null)
        {
            deathMessageText.text = $"{deadJarl.villagerName} has fallen at age {deadJarl.age:F0}";
        }
    }

    /// <summary>
    /// Show the succession selection UI
    /// </summary>
    public void ShowSuccessionUI(List<SuccessionCandidate> candidates)
    {
        if (successionPanel == null) return;

        currentCandidates = candidates;
        isShowing = true;

        // Clear existing items
        ClearCandidateItems();

        // Show panel
        successionPanel.SetActive(true);

        // Set title
        if (titleText != null)
        {
            titleText.text = "THE JARL HAS FALLEN";
        }

        // Create candidate items
        if (candidateContainer != null && candidateItemPrefab != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var itemObj = Instantiate(candidateItemPrefab, candidateContainer);
                var item = itemObj.GetComponent<SuccessionCandidateItem>();

                if (item != null)
                {
                    item.Setup(candidate, this, i == 0); // First candidate is recommended
                    candidateItems.Add(item);
                }
            }
        }

        // Pause game
        PauseManager.Instance?.EnterStrategicPause();
    }

    /// <summary>
    /// Hide the succession UI
    /// </summary>
    public void HideSuccessionUI()
    {
        if (successionPanel != null)
        {
            successionPanel.SetActive(false);
        }

        isShowing = false;
        ClearCandidateItems();

        // Resume game
        PauseManager.Instance?.ExitStrategicPause();
    }

    /// <summary>
    /// Clear all candidate item UI elements
    /// </summary>
    private void ClearCandidateItems()
    {
        foreach (var item in candidateItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        candidateItems.Clear();
    }

    /// <summary>
    /// Called when player selects a candidate
    /// </summary>
    public void OnCandidateSelected(SuccessionCandidate candidate)
    {
        if (JarlManager.Instance != null)
        {
            JarlManager.Instance.SelectHeir(candidate.Villager);
        }
    }

    /// <summary>
    /// Fallback OnGUI if no Canvas UI is set up
    /// </summary>
    private void OnGUI()
    {
        if (!isShowing) return;
        if (successionPanel != null && successionPanel.activeInHierarchy) return; // Using Canvas UI

        // Fallback GUI rendering
        float panelWidth = 400;
        float panelHeight = 400;
        float x = (Screen.width - panelWidth) / 2;
        float y = (Screen.height - panelHeight) / 2;

        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "");

        GUILayout.BeginArea(new Rect(x + 10, y + 10, panelWidth - 20, panelHeight - 20));

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("THE JARL HAS FALLEN", titleStyle);
        GUILayout.Space(10);

        // Death message
        if (JarlManager.Instance != null && JarlManager.Instance.CurrentJarl != null)
        {
            GUILayout.Label($"Choose your heir:", GUI.skin.label);
        }
        GUILayout.Space(10);

        // Candidates
        if (currentCandidates != null)
        {
            foreach (var candidate in currentCandidates)
            {
                string buttonText = $"{candidate.Villager.villagerName} ({candidate.GetRelationshipText()}) - Score: {candidate.SuccessionScore:F0}";

                if (candidate.Priority == SuccessionPriority.DirectChild)
                {
                    buttonText += " [RECOMMENDED]";
                }

                if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                {
                    OnCandidateSelected(candidate);
                }
                GUILayout.Space(5);
            }
        }

        GUILayout.FlexibleSpace();

        // Timer
        GUILayout.Label($"Auto-select best candidate in {Mathf.CeilToInt(autoSelectTimer)}s");

        GUILayout.EndArea();
    }
}
