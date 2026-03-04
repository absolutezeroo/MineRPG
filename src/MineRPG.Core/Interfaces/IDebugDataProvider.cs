namespace MineRPG.Core.Interfaces;

/// <summary>
/// Provides aggregated debug data for the UI overlay.
/// Implemented at the Game/composition level, injected into Godot.UI.
/// This abstraction decouples the UI from World and Entities projects.
/// </summary>
public interface IDebugDataProvider
{
    /// <summary>Player world-space X coordinate.</summary>
    public float PlayerX { get; }

    /// <summary>Player world-space Y coordinate.</summary>
    public float PlayerY { get; }

    /// <summary>Player world-space Z coordinate.</summary>
    public float PlayerZ { get; }

    /// <summary>Chunk X coordinate the player is currently in.</summary>
    public int ChunkX { get; }

    /// <summary>Chunk Z coordinate the player is currently in.</summary>
    public int ChunkZ { get; }

    /// <summary>Name of the biome at the player's current position.</summary>
    public string CurrentBiome { get; }

    /// <summary>Total number of chunks currently loaded in memory.</summary>
    public int LoadedChunkCount { get; }

    /// <summary>Number of chunks currently visible after frustum culling.</summary>
    public int VisibleChunkCount { get; }

    /// <summary>Number of chunks waiting in the generation/meshing queue.</summary>
    public int ChunksInQueue { get; }

    /// <summary>Rolling average time in milliseconds to mesh a single chunk.</summary>
    public double AverageMeshTimeMs { get; }

    /// <summary>Total vertex count across all loaded chunk meshes.</summary>
    public long TotalVertices { get; }

    /// <summary>Current render distance in chunks.</summary>
    public int RenderDistance { get; }

    /// <summary>Number of idle (recycled) chunk nodes in the pool.</summary>
    public int PoolIdleCount { get; }

    /// <summary>Number of active (in-use) chunk nodes from the pool.</summary>
    public int PoolActiveCount { get; }
}
