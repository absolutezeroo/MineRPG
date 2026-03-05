namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published on every logical game tick (fixed timestep).
/// </summary>
public readonly struct GameTickEvent
{
    /// <summary>
    /// Time elapsed since the previous tick, in seconds.
    /// </summary>
    public float DeltaTime { get; init; }

    /// <summary>
    /// Monotonically increasing tick counter.
    /// </summary>
    public ulong TickIndex { get; init; }
}
