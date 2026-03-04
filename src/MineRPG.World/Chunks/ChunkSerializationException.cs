namespace MineRPG.World.Chunks;

/// <summary>
/// Thrown when chunk serialization or deserialization fails due to
/// corrupted data, version mismatch, or CRC checksum failure.
/// </summary>
public sealed class ChunkSerializationException : Exception
{
    public ChunkSerializationException(string message)
        : base(message) { }

    public ChunkSerializationException(string message, Exception innerException)
        : base(message, innerException) { }
}
