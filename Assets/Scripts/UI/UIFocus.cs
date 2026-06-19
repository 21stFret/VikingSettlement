using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central hub for controller/EventSystem focus management.
/// Use instead of calling EventSystem.current.SetSelectedGameObject directly.
///
/// Push/Pop for nested panels: Push when opening a sub-panel (saves where focus was),
/// Pop when closing it (restores the parent panel's selection).
/// Set for a direct one-shot assignment. Clear to fully exit UI mode.
/// </summary>
public static class UIFocus
{
    private static readonly Stack<GameObject> _stack = new Stack<GameObject>();

    public static GameObject Current { get; private set; }

    /// <summary>Open a nested panel — saves current focus so Pop() can restore it.</summary>
    public static void Push(GameObject target)
    {
        _stack.Push(Current);
        Apply(target);
    }

    /// <summary>Close a nested panel — restores whatever had focus before the last Push.</summary>
    public static void Pop()
    {
        Apply(_stack.Count > 0 ? _stack.Pop() : null);
    }

    /// <summary>Set focus without stack tracking — use for top-level panels.</summary>
    public static void Set(GameObject target)
    {
        _stack.Clear();
        Apply(target);
    }

    /// <summary>Fully exit UI mode and clear the stack.</summary>
    public static void Clear()
    {
        _stack.Clear();
        Apply(null);
    }

    private static void Apply(GameObject target)
    {
        Current = target;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target);
    }
}
