using UnityEngine;

/// <summary>
/// Interface for objects that can be clicked in the world
/// </summary>
public interface IClickable
{
    /// <summary>
    /// The collider used for click detection
    /// </summary>
    Collider2D Collider { get; }

    /// <summary>
    /// Priority for click handling (higher = checked first)
    /// Useful when objects overlap
    /// </summary>
    int ClickPriority { get; }

    /// <summary>
    /// Whether this object can currently be clicked
    /// </summary>
    bool IsClickable { get; }

    /// <summary>
    /// Called when this object is clicked
    /// </summary>
    void OnClicked();
}
