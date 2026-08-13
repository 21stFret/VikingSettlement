using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player-facing panel for choosing a Holmgang opponent.
///
/// Setup:
///   - panel          : root GameObject to show/hide
///   - opponentListParent : layout group that holds opponent item prefabs
///   - opponentItemPrefab : prefab with HolmgangOpponentItemUI component
///   - closeButton    : optional X / cancel button
///
/// Call Populate() once (or whenever the opponent list changes), then Open() / Close() as needed.
/// Subscribe to OnOpponentSelected to receive the chosen index.
/// </summary>
public class HolmgangUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    [Header("Opponent List")]
    [SerializeField] private Transform opponentListParent;
    [SerializeField] private GameObject opponentItemPrefab;

    public event Action<int> OnOpponentSelected;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        panel?.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    /// <summary>
    /// Rebuild the opponent list. Call this once after HolmgangManager is ready.
    /// </summary>
    public void Populate(string[] names, string[] descriptions)
    {
        foreach (Transform child in opponentListParent)
            Destroy(child.gameObject);

        int count = Mathf.Min(names.Length, descriptions.Length);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            var go   = Instantiate(opponentItemPrefab, opponentListParent);
            var item = go.GetComponent<HolmgangOpponentItemUI>();
            item?.Setup(names[i], descriptions[i], () => SelectOpponent(index));
        }
    }

    public void Open()
    {
        panel?.SetActive(true);
        UIFocus.SetNextFrame(opponentListParent.GetChild(0).gameObject);
    }

    public void Close()
    {
        panel?.SetActive(false);
        PlayerController.Instance?.SetInputEnabled(true);
    }

    private void SelectOpponent(int index)
    {
        Close();
        OnOpponentSelected?.Invoke(index);
    }
}
