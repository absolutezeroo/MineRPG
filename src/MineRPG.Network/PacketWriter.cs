using System.Buffers;
using System.Text;

namespace MineRPG.Network;

/// <summary>
/// Binary packet writer. Builds a byte buffer for network transmission.
/// Uses ArrayPool to avoid GC allocations on hot paths.
/// </summary>
public sealed class PacketWriter : IDisposable
{
    private byte[] _buffer;
    private int _position;

    public PacketWriter(int initialCapacity = 256)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    public int Length => _position;

    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }

    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(2);
        _buffer[_position++] = (byte)(value & 0xFF);
        _buffer[_position++] = (byte)((value >> 8) & 0xFF);
    }

    public void WriteInt32(int value)
    {
        EnsureCapacity(4);
        _buffer[_position++] = (byte)(value & 0xFF);
        _buffer[_position++] = (byte)((value >> 8) & 0xFF);
        _buffer[_position++] = (byte)((value >> 16) & 0xFF);
        _buffer[_position++] = (byte)((value >> 24) & 0xFF);
    }

    public void WriteFloat(float value)
    {
        WriteInt32(BitConverter.SingleToInt32Bits(value));
    }

    public void WriteString(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteUInt16((ushort)byteCount);
        EnsureCapacity(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position, byteCount));
        _position += byteCount;
    }

    public ReadOnlySpan<byte> ToSpan() => _buffer.AsSpan(0, _position);

    public byte[] ToArray() => _buffer.AsSpan(0, _position).ToArray();

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    private void EnsureCapacity(int additional)
    {
        if (_position + additional <= _buffer.Length)
            return;

        var newSize = Math.Max(_buffer.Length * 2, _position + additional);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
}
