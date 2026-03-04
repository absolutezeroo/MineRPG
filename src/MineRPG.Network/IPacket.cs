namespace MineRPG.Network;

/// <summary>
/// Base contract for all network packets.
/// Each packet type has a unique ID and knows how to serialize/deserialize itself.
/// </summary>
public interface IPacket
{
    /// <summary>Unique numeric identifier for this packet type.</summary>
    public ushort PacketId { get; }

    /// <summary>
    /// Serializes this packet's fields into the provided writer.
    /// </summary>
    /// <param name="writer">The binary writer to serialize into.</param>
    public void Write(PacketWriter writer);

    /// <summary>
    /// Deserializes this packet's fields from the provided reader.
    /// </summary>
    /// <param name="reader">The binary reader to deserialize from.</param>
    public void Read(PacketReader reader);
}
