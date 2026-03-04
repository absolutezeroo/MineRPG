using MineRPG.Core.Math;

namespace MineRPG.World.Events;

public readonly struct ChunkSavedEvent
{
    public ChunkCoord Coord { get; init; }
    public int ByteSize { get; init; }
}
