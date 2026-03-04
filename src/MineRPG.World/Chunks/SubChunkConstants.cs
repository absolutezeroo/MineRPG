namespace MineRPG.World.Chunks;

/// <summary>
/// Constants for the sub-chunk height-slicing system.
/// A full chunk (16x256x16) is divided into 16 sub-chunks of 16x16x16.
/// </summary>
public static class SubChunkConstants
{
    /// <summary>Size of each sub-chunk in blocks per axis.</summary>
    public const int SubChunkSize = 16;

    /// <summary>Total number of sub-chunks per full chunk (256 / 16 = 16).</summary>
    public const int SubChunkCount = ChunkData.SizeY / SubChunkSize;

    /// <summary>Total blocks in one sub-chunk (16 x 16 x 16 = 4096).</summary>
    public const int BlocksPerSubChunk = ChunkData.SizeX * SubChunkSize * ChunkData.SizeZ;
}
