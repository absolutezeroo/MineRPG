using MineRPG.World.Spatial;

namespace MineRPG.World.Events;

public readonly struct BlockChangedEvent
{
    public WorldPosition Position { get; init; }
    public ushort OldBlockId { get; init; }
    public ushort NewBlockId { get; init; }
}
