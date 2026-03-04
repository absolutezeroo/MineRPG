using System.Text;

namespace MineRPG.Network;

/// <summary>
/// Binary packet reader. Reads sequential fields from a byte buffer.
/// </summary>
public sealed class PacketReader
{
    private readonly ReadOnlyMemory<byte> _data;
    private int _position;

    public PacketReader(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }

    public int Remaining => _data.Length - _position;

    public byte ReadByte()
    {
        EnsureAvailable(1);
        return _data.Span[_position++];
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        var span = _data.Span;
        var value = (ushort)(span[_position] | (span[_position + 1] << 8));
        _position += 2;
        return value;
    }

    public int ReadInt32()
    {
        EnsureAvailable(4);
        var span = _data.Span;
        var value = span[_position]
                    | (span[_position + 1] << 8)
                    | (span[_position + 2] << 16)
                    | (span[_position + 3] << 24);
        _position += 4;
        return value;
    }

    public float ReadFloat()
    {
        return BitConverter.Int32BitsToSingle(ReadInt32());
    }

    public string ReadString()
    {
        var byteCount = ReadUInt16();
        EnsureAvailable(byteCount);
        var value = Encoding.UTF8.GetString(_data.Span.Slice(_position, byteCount));
        _position += byteCount;
        return value;
    }

    private void EnsureAvailable(int count)
    {
        if (_position + count > _data.Length)
            throw new InvalidOperationException(
                $"Packet underflow: need {count} bytes at position {_position}, but only {_data.Length - _position} remain.");
    }
}
