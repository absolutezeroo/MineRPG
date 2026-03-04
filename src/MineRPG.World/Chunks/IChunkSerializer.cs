using System;

namespace MineRPG.World.Chunks;

/// <summary>
/// Serializes/deserializes ChunkData to/from a compact binary format.
/// </summary>
public interface IChunkSerializer
{
    /// <summary>
    /// Serialize the chunk data to a byte array.
    /// </summary>
    /// <param name="data">The chunk data to serialize.</param>
    /// <returns>The serialized byte array.</returns>
    public byte[] Serialize(ChunkData data);

    /// <summary>
    /// Deserialize binary data and load it into the target ChunkData.
    /// </summary>
    /// <param name="source">The binary data to deserialize.</param>
    /// <param name="target">The target chunk data to populate.</param>
    public void Deserialize(ReadOnlySpan<byte> source, ChunkData target);
}
