using Godot;

namespace MineRPG.Game.Bootstrap.Input;

/// <summary>
/// Describes a single rebindable input action: the InputMap action
/// (as <see cref="StringName"/>) and the user-facing display label
/// shown in the Controls options tab.
/// </summary>
public readonly record struct RebindableActionData
{
    /// <summary>
    /// Creates a rebindable action descriptor.
    /// </summary>
    /// <param name="action">The pre-allocated StringName constant from <see cref="InputActions"/>.</param>
    /// <param name="displayLabel">The user-facing label (e.g., "Move Forward").</param>
    public RebindableActionData(StringName action, string displayLabel)
    {
        Action = action;
        DisplayLabel = displayLabel;
    }

    /// <summary>The pre-allocated StringName for InputMap API calls.</summary>
    public StringName Action { get; }

    /// <summary>The raw string action name for dictionary keys and serialization.</summary>
    public string ActionName => Action.ToString();

    /// <summary>The user-facing label (e.g., "Move Forward").</summary>
    public string DisplayLabel { get; }
}
