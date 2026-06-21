using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a single opponent card inside the Holmgang challenge UI.
/// Attach to the opponent item prefab and wire the three SerializeField references.
/// </summary>
public class HolmgangOpponentItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button challengeButton;

    public void Setup(string opponentName, string description, Action onChallenge)
    {
        if (nameText != null)        nameText.text        = opponentName;
        if (descriptionText != null) descriptionText.text = description;

        if (challengeButton != null)
        {
            challengeButton.onClick.RemoveAllListeners();
            challengeButton.onClick.AddListener(() => onChallenge?.Invoke());
        }
    }
}
