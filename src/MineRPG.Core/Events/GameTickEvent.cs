namespace MineRPG.Core.Events;

/// <summary>Published on every logical game tick (fixed timestep).</summary>
public readonly struct GameTickEvent
{
    public float DeltaTime { get; init; }
    public ulong TickIndex { get; init; }
}
