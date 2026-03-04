using System;
using System.Text;

namespace MineRPG.Network;

/// <summary>
/// Binary packet reader. Reads sequential fields from a byte buffer.
/// </summary>
public sealed class PacketReader
{
    private const int ByteSize = 1;
    private const int UInt16Size = 2;
    private const int Int32Size = 4;
    private const int BitsPerByte = 8;

    private readonly ReadOnlyMemory<byte> _data;
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketReader"/> class.
    /// </summary>
    /// <param name="data">The byte buffer to read from.</param>
    public PacketReader(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }

    /// <summary>Number of bytes remaining in the buffer.</summary>
    public int Remaining => _data.Length - _position;

    /// <summary>
    /// Reads a single byte from the buffer.
    /// </summary>
    /// <returns>The byte value read.</returns>
    public byte ReadByte()
    {
        EnsureAvailable(ByteSize);
        return _data.Span[_position++];
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer from the buffer in little-endian format.
    /// </summary>
    /// <returns>The unsigned 16-bit integer value read.</returns>
    public ushort ReadUInt16()
    {
        EnsureAvailable(UInt16Size);
        ReadOnlySpan<byte> span = _data.Span;
        ushort value = (ushort)(span[_position] | (span[_position + 1] << BitsPerByte));
        _position += UInt16Size;
        return value;
    }

    /// <summary>
    /// Reads a signed 32-bit integer from the buffer in little-endian format.
    /// </summary>
    /// <returns>The signed 32-bit integer value read.</returns>
    public int ReadInt32()
    {
        EnsureAvailable(Int32Size);
        ReadOnlySpan<byte> span = _data.Span;
        int value = span[_position]
                    | (span[_position + 1] << BitsPerByte)
                    | (span[_position + 2] << (BitsPerByte * 2))
                    | (span[_position + 3] << (BitsPerByte * 3));
        _position += Int32Size;
        return value;
    }

    /// <summary>
    /// Reads a 32-bit floating-point value from the buffer.
    /// </summary>
    /// <returns>The float value read.</returns>
    public float ReadFloat()
    {
        return BitConverter.Int32BitsToSingle(ReadInt32());
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string from the buffer.
    /// </summary>
    /// <returns>The string value read.</returns>
    public string ReadString()
    {
        ushort byteCount = ReadUInt16();
        EnsureAvailable(byteCount);
        string value = Encoding.UTF8.GetString(_data.Span.Slice(_position, byteCount));
        _position += byteCount;
        return value;
    }

    private void EnsureAvailable(int count)
    {
        if (_position + count > _data.Length)
        {
            throw new InvalidOperationException(
                $"Packet underflow: need {count} bytes at position {_position}, but only {_data.Length - _position} remain.");
        }
    }
}
