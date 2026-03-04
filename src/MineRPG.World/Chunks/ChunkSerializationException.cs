using System;

namespace MineRPG.World.Chunks;

/// <summary>
/// Thrown when chunk serialization or deserialization fails due to
/// corrupted data, version mismatch, or CRC checksum failure.
/// </summary>
public sealed class ChunkSerializationException : Exception
{
    /// <summary>
    /// Creates a new chunk serialization exception with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ChunkSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new chunk serialization exception with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ChunkSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
