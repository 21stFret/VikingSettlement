using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to the root of a UI panel to trap controller/keyboard navigation inside it.
/// Caches all child Buttons (including inactive) on Awake so automatic navigation
/// can still do its geometry work — this just blocks any result that would escape
/// the panel's button set.
///
/// Call Activate() when the panel opens and Deactivate() when it closes.
/// Call RefreshCache() only if new Button GameObjects are instantiated at runtime
/// (pooled/toggled buttons are already captured by the initial includeInactive pass).
/// </summary>
public class PanelNavigationBoundary : MonoBehaviour
{
    public bool IsActive { get; private set; }

    private readonly HashSet<Button> _cache = new();
    private GameObject _lastValid;

    private void Awake()
    {
        Invoke("RefreshCache", 0.5f);
    }

    private void OnEnable()
    {
        Activate();
    }

    private void OnDisable()
    {
        Deactivate();
    }

    public void RefreshCache()
    {
        _cache.Clear();
        foreach (var btn in GetComponentsInChildren<Button>(true))
            _cache.Add(btn);
    }

    public void Activate(GameObject initialFocus = null)
    {
        IsActive = true;
        if (initialFocus != null)
        {
            _lastValid = initialFocus;
            EventSystem.current?.SetSelectedGameObject(initialFocus);
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        _lastValid = null;
    }

    private void LateUpdate()
    {
        if (!IsActive || EventSystem.current == null) return;

        var current = EventSystem.current.currentSelectedGameObject;

        if (current == null)
        {
            if (_lastValid != null)
                EventSystem.current.SetSelectedGameObject(_lastValid);
            return;
        }

        var btn = current.GetComponent<Button>();
        if (btn != null && !_cache.Contains(btn))
        {
            EventSystem.current.SetSelectedGameObject(_lastValid);
            return;
        }

        if (btn != null)
            _lastValid = current;
    }
}
