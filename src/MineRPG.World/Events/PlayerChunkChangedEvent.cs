using MineRPG.Core.Math;

namespace MineRPG.World.Events;

/// <summary>
/// Fired when the player moves into a different chunk.
/// WorldNode subscribes to trigger chunk loading/unloading.
/// </summary>
public readonly struct PlayerChunkChangedEvent
{
    /// <summary>The chunk the player was previously in.</summary>
    public ChunkCoord OldChunk { get; init; }

    /// <summary>The chunk the player has moved into.</summary>
    public ChunkCoord NewChunk { get; init; }
}
