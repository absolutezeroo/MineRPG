namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's health reaches zero.
/// </summary>
public readonly struct PlayerDiedEvent
{
    /// <summary>World X position where the player died.</summary>
    public float PositionX { get; init; }

    /// <summary>World Y position where the player died.</summary>
    public float PositionY { get; init; }

    /// <summary>World Z position where the player died.</summary>
    public float PositionZ { get; init; }
}
