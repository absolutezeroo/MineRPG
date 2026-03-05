using System;
using System.Buffers;
using System.Text;

namespace MineRPG.Network;

/// <summary>
/// Binary packet writer. Builds a byte buffer for network transmission.
/// Uses ArrayPool to avoid GC allocations on hot paths.
/// </summary>
public sealed class PacketWriter : IDisposable
{
    private const int DefaultInitialCapacity = 256;
    private const int BitsPerByte = 8;
    private const int GrowthFactor = 2;
    private const byte ByteMask = 0xFF;
    private const int ByteSize = 1;
    private const int UInt16Size = 2;
    private const int Int32Size = 4;

    private byte[] _buffer;
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketWriter"/> class.
    /// </summary>
    /// <param name="initialCapacity">Initial buffer capacity in bytes.</param>
    public PacketWriter(int initialCapacity = DefaultInitialCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    /// <summary>Number of bytes written so far.</summary>
    public int Length => _position;

    /// <summary>
    /// Writes a single byte to the buffer.
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    public void WriteByte(byte value)
    {
        EnsureCapacity(ByteSize);
        _buffer[_position++] = value;
    }

    /// <summary>
    /// Writes an unsigned 16-bit integer to the buffer in little-endian format.
    /// </summary>
    /// <param name="value">The unsigned 16-bit integer to write.</param>
    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(UInt16Size);
        _buffer[_position++] = (byte)(value & ByteMask);
        _buffer[_position++] = (byte)((value >> BitsPerByte) & ByteMask);
    }

    /// <summary>
    /// Writes a signed 32-bit integer to the buffer in little-endian format.
    /// </summary>
    /// <param name="value">The signed 32-bit integer to write.</param>
    public void WriteInt32(int value)
    {
        EnsureCapacity(Int32Size);
        _buffer[_position++] = (byte)(value & ByteMask);
        _buffer[_position++] = (byte)((value >> BitsPerByte) & ByteMask);
        _buffer[_position++] = (byte)((value >> (BitsPerByte * 2)) & ByteMask);
        _buffer[_position++] = (byte)((value >> (BitsPerByte * 3)) & ByteMask);
    }

    /// <summary>
    /// Writes a 32-bit floating-point value to the buffer.
    /// </summary>
    /// <param name="value">The float value to write.</param>
    public void WriteFloat(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));

    /// <summary>
    /// Writes a length-prefixed UTF-8 string to the buffer.
    /// </summary>
    /// <param name="value">The string to write.</param>
    public void WriteString(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteUInt16((ushort)byteCount);
        EnsureCapacity(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position, byteCount));
        _position += byteCount;
    }

    /// <summary>
    /// Returns the written bytes as a read-only span.
    /// </summary>
    /// <returns>A span over the written portion of the buffer.</returns>
    public ReadOnlySpan<byte> ToSpan() => _buffer.AsSpan(0, _position);

    /// <summary>
    /// Returns the written bytes as a new byte array.
    /// </summary>
    /// <returns>A copy of the written bytes.</returns>
    public byte[] ToArray() => _buffer.AsSpan(0, _position).ToArray();

    /// <summary>
    /// Returns the rented buffer to the array pool.
    /// </summary>
    public void Dispose() => ArrayPool<byte>.Shared.Return(_buffer);

    private void EnsureCapacity(int additional)
    {
        if (_position + additional <= _buffer.Length)
        {
            return;
        }

        int newSize = Math.Max(_buffer.Length * GrowthFactor, _position + additional);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
}
