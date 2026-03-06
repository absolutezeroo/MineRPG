namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Lightweight entry for chunk map rendering: coordinates + state index.
/// </summary>
public readonly struct ChunkStateEntry
{
    /// <summary>Chunk X coordinate.</summary>
    public int ChunkX { get; init; }

    /// <summary>Chunk Z coordinate.</summary>
    public int ChunkZ { get; init; }

    /// <summary>
    /// State index matching <see cref="MineRPG.World.Chunks.ChunkState"/> ordinal.
    /// Used for color lookup by the chunk map renderer.
    /// </summary>
    public int StateIndex { get; init; }
}
