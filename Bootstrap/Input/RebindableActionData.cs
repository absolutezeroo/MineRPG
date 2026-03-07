namespace MineRPG.Game.Bootstrap.Input;

/// <summary>
/// Describes a single rebindable input action: the InputMap action name
/// and the user-facing display label shown in the Controls options tab.
/// </summary>
/// <param name="ActionName">The InputMap action name (e.g., "move_forward").</param>
/// <param name="DisplayLabel">The user-facing label (e.g., "Move Forward").</param>
public readonly record struct RebindableActionData(string ActionName, string DisplayLabel);
