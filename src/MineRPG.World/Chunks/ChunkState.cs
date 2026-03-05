namespace MineRPG.World.Chunks;

/// <summary>
/// Lifecycle state of a chunk in the loading pipeline.
/// </summary>
public enum ChunkState
{
    /// <summary>Chunk is not loaded.</summary>
    Unloaded,

    /// <summary>Chunk is queued for generation.</summary>
    Queued,

    /// <summary>Chunk terrain is being generated.</summary>
    Generating,

    /// <summary>Chunk terrain generation is complete.</summary>
    Generated,

    /// <summary>Chunk mesh is being built.</summary>
    Meshing,

    /// <summary>Chunk is fully loaded, meshed, and ready for rendering.</summary>
    Ready,

    /// <summary>Chunk has been modified and needs re-meshing.</summary>
    Dirty,

    /// <summary>Chunk is being unloaded. Save may be in progress. No further access allowed.</summary>
    Unloading,
}
