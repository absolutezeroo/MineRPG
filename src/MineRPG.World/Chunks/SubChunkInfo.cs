namespace MineRPG.World.Chunks;

/// <summary>
/// Metadata about a 16x16x16 vertical sub-section of a chunk.
/// Computed during generation to allow the mesher to skip empty
/// or fully-solid sub-chunks without iterating their blocks.
/// </summary>
public readonly struct SubChunkInfo
{
    /// <summary>
    /// Vertical index [0..SubChunkCount). SubChunk Y range = [YIndex*16, YIndex*16+16).
    /// </summary>
    public int YIndex { get; }

    /// <summary>True when all 4096 blocks in this sub-chunk are air (block ID 0).</summary>
    public bool IsEmpty { get; }

    /// <summary>True when all 4096 blocks are non-air (no internal faces can exist).</summary>
    public bool IsFullySolid { get; }

    /// <summary>Number of non-air blocks. Useful for heuristics.</summary>
    public int NonAirCount { get; }

    /// <summary>Minimum Y coordinate of this sub-chunk.</summary>
    public int MinY => YIndex * SubChunkConstants.SubChunkSize;

    /// <summary>Exclusive maximum Y coordinate of this sub-chunk.</summary>
    public int MaxY => MinY + SubChunkConstants.SubChunkSize;

    /// <summary>
    /// Creates sub-chunk metadata.
    /// </summary>
    /// <param name="yIndex">Vertical sub-chunk index.</param>
    /// <param name="isEmpty">Whether all blocks are air.</param>
    /// <param name="isFullySolid">Whether all blocks are non-air.</param>
    /// <param name="nonAirCount">Count of non-air blocks.</param>
    public SubChunkInfo(int yIndex, bool isEmpty, bool isFullySolid, int nonAirCount)
    {
        YIndex = yIndex;
        IsEmpty = isEmpty;
        IsFullySolid = isFullySolid;
        NonAirCount = nonAirCount;
    }
}
