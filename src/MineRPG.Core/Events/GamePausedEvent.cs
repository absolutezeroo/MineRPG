namespace MineRPG.Core.Events;

/// <summary>Published when the game is paused or unpaused.</summary>
public readonly struct GamePausedEvent
{
    public bool IsPaused { get; init; }
}
