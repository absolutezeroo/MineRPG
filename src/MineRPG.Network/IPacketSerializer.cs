namespace MineRPG.Network;

/// <summary>
/// Serializes and deserializes packets to/from byte arrays.
/// Uses the packet ID header to resolve the correct <see cref="IPacket"/> type.
/// </summary>
public interface IPacketSerializer
{
    byte[] Serialize(IPacket packet);
    IPacket Deserialize(ReadOnlyMemory<byte> data);
}
