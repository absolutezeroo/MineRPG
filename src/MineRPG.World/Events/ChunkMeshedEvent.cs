using MineRPG.Core.Math;

namespace MineRPG.World.Events;

public readonly struct ChunkMeshedEvent
{
    public ChunkCoord Coord { get; init; }
}
