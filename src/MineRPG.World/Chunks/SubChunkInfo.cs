namespace MineRPG.World.Chunks;

/// <summary>
/// Metadata about a 16x16x16 vertical sub-section of a chunk.
/// Computed during generation to allow the mesher to skip empty
/// or fully-solid sub-chunks without iterating their blocks.
/// </summary>
public readonly struct SubChunkInfo(int yIndex, bool isEmpty, bool isFullySolid, int nonAirCount)
{
    /// <summary>
    /// Vertical index [0..SubChunkCount). SubChunk Y range = [YIndex*16, YIndex*16+16).
    /// </summary>
    public int YIndex { get; } = yIndex;

    /// <summary>True when all 4096 blocks in this sub-chunk are air (block ID 0).</summary>
    public bool IsEmpty { get; } = isEmpty;

    /// <summary>True when all 4096 blocks are non-air (no internal faces can exist).</summary>
    public bool IsFullySolid { get; } = isFullySolid;

    /// <summary>Number of non-air blocks. Useful for heuristics.</summary>
    public int NonAirCount { get; } = nonAirCount;

    public int MinY => YIndex * SubChunkConstants.SubChunkSize;
    public int MaxY => MinY + SubChunkConstants.SubChunkSize;
}
