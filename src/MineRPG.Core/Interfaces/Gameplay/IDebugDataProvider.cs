namespace MineRPG.Core.Interfaces.Gameplay;

/// <summary>
/// Provides aggregated debug data for the UI overlay.
/// Implemented at the Game/composition level, injected into Godot.UI.
/// This abstraction decouples the UI from World and Entities projects.
/// </summary>
public interface IDebugDataProvider
{
    /// <summary>Player world-space X coordinate.</summary>
    float PlayerX { get; }

    /// <summary>Player world-space Y coordinate.</summary>
    float PlayerY { get; }

    /// <summary>Player world-space Z coordinate.</summary>
    float PlayerZ { get; }

    /// <summary>Chunk X coordinate the player is currently in.</summary>
    int ChunkX { get; }

    /// <summary>Chunk Z coordinate the player is currently in.</summary>
    int ChunkZ { get; }

    /// <summary>Name of the biome at the player's current position.</summary>
    string CurrentBiome { get; }

    /// <summary>Total number of chunks currently loaded in memory.</summary>
    int LoadedChunkCount { get; }

    /// <summary>Number of chunks currently visible after frustum culling.</summary>
    int VisibleChunkCount { get; }

    /// <summary>Number of chunks waiting in the generation/meshing queue.</summary>
    int ChunksInQueue { get; }

    /// <summary>Rolling average time in milliseconds to mesh a single chunk.</summary>
    double AverageMeshTimeMs { get; }

    /// <summary>Total vertex count across all loaded chunk meshes.</summary>
    long TotalVertices { get; }

    /// <summary>Current render distance in chunks.</summary>
    int RenderDistance { get; }

    /// <summary>Number of idle (recycled) chunk nodes in the pool.</summary>
    int PoolIdleCount { get; }

    /// <summary>Number of active (in-use) chunk nodes from the pool.</summary>
    int PoolActiveCount { get; }
}
