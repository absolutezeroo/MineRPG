namespace MineRPG.Network;

/// <summary>
/// Base contract for all network packets.
/// Each packet type has a unique ID and knows how to serialize/deserialize itself.
/// </summary>
public interface IPacket
{
    ushort PacketId { get; }
    void Write(PacketWriter writer);
    void Read(PacketReader reader);
}
