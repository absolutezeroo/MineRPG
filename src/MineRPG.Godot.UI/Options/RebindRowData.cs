namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Describes a single rebindable action row in the Controls tab.
/// </summary>
/// <param name="ActionName">The InputMap action name (e.g., "move_forward").</param>
/// <param name="DisplayLabel">The user-facing label (e.g., "Move Forward").</param>
public readonly record struct RebindRowData(string ActionName, string DisplayLabel);
