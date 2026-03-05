namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the game is paused or unpaused.
/// </summary>
public readonly struct GamePausedEvent
{
    /// <summary>
    /// Whether the game is currently paused.
    /// </summary>
    public bool IsPaused { get; init; }
}
