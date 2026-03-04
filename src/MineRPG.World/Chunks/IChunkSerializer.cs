namespace MineRPG.World.Chunks;

/// <summary>
/// Serializes/deserializes ChunkData to/from a compact binary format.
/// </summary>
public interface IChunkSerializer
{
    /// <summary>
    /// Serialize the chunk data to a byte array.
    /// </summary>
    byte[] Serialize(ChunkData data);

    /// <summary>
    /// Deserialize binary data and load it into the target ChunkData.
    /// </summary>
    void Deserialize(ReadOnlySpan<byte> source, ChunkData target);
}
