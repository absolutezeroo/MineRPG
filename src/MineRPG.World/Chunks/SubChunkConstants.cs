namespace MineRPG.World.Chunks;

/// <summary>
/// Constants for the sub-chunk height-slicing system.
/// A full chunk (16×256×16) is divided into 16 sub-chunks of 16×16×16.
/// </summary>
public static class SubChunkConstants
{
    public const int SubChunkSize = 16;
    public const int SubChunkCount = ChunkData.SizeY / SubChunkSize; // 256/16 = 16
    public const int BlocksPerSubChunk = ChunkData.SizeX * SubChunkSize * ChunkData.SizeZ; // 16*16*16 = 4096
}
