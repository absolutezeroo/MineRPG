using MineRPG.Core.Math;

namespace MineRPG.World.Events;

public readonly struct ChunkGeneratedEvent
{
    public ChunkCoord Coord { get; init; }
}
