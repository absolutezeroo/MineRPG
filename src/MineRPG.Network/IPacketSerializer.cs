using System;

namespace MineRPG.Network;

/// <summary>
/// Serializes and deserializes packets to/from byte arrays.
/// Uses the packet ID header to resolve the correct <see cref="IPacket"/> type.
/// </summary>
public interface IPacketSerializer
{
    /// <summary>
    /// Serializes the given packet into a byte array including the packet ID header.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <returns>A byte array containing the serialized packet data.</returns>
    byte[] Serialize(IPacket packet);

    /// <summary>
    /// Deserializes a packet from raw byte data, using the packet ID header to resolve the type.
    /// </summary>
    /// <param name="data">The raw byte data to deserialize.</param>
    /// <returns>The deserialized packet instance.</returns>
    IPacket Deserialize(ReadOnlyMemory<byte> data);
}
