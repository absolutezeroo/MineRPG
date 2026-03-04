using MineRPG.Core.Math;

namespace MineRPG.World.Events;

public readonly struct ChunkUnloadedEvent
{
    public ChunkCoord Coord { get; init; }
}
