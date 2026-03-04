using MineRPG.Core.Math;

namespace MineRPG.World.Events;

/// <summary>
/// Fired when the player moves into a different chunk.
/// WorldNode subscribes to trigger chunk loading/unloading.
/// </summary>
public readonly struct PlayerChunkChangedEvent
{
    public ChunkCoord OldChunk { get; init; }
    public ChunkCoord NewChunk { get; init; }
}
