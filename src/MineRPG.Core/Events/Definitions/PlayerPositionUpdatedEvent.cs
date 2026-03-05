namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published every physics frame with the player's current world position.
/// WorldNode subscribes to track chunk transitions.
/// </summary>
public readonly struct PlayerPositionUpdatedEvent
{
    /// <summary>
    /// Player world-space X coordinate.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Player world-space Y coordinate.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Player world-space Z coordinate.
    /// </summary>
    public float Z { get; init; }
}
