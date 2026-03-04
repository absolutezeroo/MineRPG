namespace MineRPG.Core.Events;

/// <summary>
/// Published every physics frame with the player's current world position.
/// WorldNode subscribes to track chunk transitions.
/// </summary>
public readonly struct PlayerPositionUpdatedEvent
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}
