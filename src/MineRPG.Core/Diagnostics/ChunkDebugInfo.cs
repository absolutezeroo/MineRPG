namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Debug information for a single chunk, used by the chunk map tooltip.
/// </summary>
public readonly struct ChunkDebugInfo
{
    /// <summary>Chunk X coordinate.</summary>
    public int ChunkX { get; init; }

    /// <summary>Chunk Z coordinate.</summary>
    public int ChunkZ { get; init; }

    /// <summary>Current chunk state name.</summary>
    public string StateName { get; init; }

    /// <summary>Biome name at this chunk's center.</summary>
    public string BiomeName { get; init; }

    /// <summary>Minimum Y with a non-air block.</summary>
    public int MinBlockY { get; init; }

    /// <summary>Maximum Y with a non-air block.</summary>
    public int MaxBlockY { get; init; }

    /// <summary>Total vertex count in this chunk's mesh.</summary>
    public int VertexCount { get; init; }

    /// <summary>Time taken to generate this chunk in milliseconds.</summary>
    public double GenerationTimeMs { get; init; }

    /// <summary>Time taken to mesh this chunk in milliseconds.</summary>
    public double MeshTimeMs { get; init; }

    /// <summary>Whether this chunk has been modified by the player.</summary>
    public bool IsModified { get; init; }
}
