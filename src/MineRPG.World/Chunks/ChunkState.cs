namespace MineRPG.World.Chunks;

/// <summary>
/// Chunk lifecycle states. The numeric order matters — code uses ordinal
/// comparisons (&gt;=, &lt;) to check readiness. States from RelightPending onward
/// have valid voxel data. States from Generated onward are eligible for meshing.
/// </summary>
public enum ChunkState
{
    /// <summary>Chunk is not loaded.</summary>
    Unloaded,

    /// <summary>Chunk is queued for generation.</summary>
    Queued,

    /// <summary>Chunk terrain is being generated.</summary>
    Generating,

    /// <summary>Chunk terrain data is complete but light propagation is pending.</summary>
    RelightPending,

    /// <summary>Chunk terrain generation is complete (including lighting).</summary>
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
