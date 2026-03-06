using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;

namespace MineRPG.World.Chunks.Serialization;

/// <summary>
/// Binary chunk serializer using RLE compression and CRC32 integrity.
///
/// Format:
///   [4B Magic "MCRK"]
///   [2B Version]
///   [4B CoordX]
///   [4B CoordZ]
///   [4B UncompressedBlockCount]
///   [4B CompressedByteLen]
///   [N bytes RLE data: (count:ushort, blockId:ushort) pairs]
///   [4B CRC32 of everything before this field]
///
/// Typical compression: 128KB raw -> 2-25KB RLE.
/// </summary>
public sealed class ChunkSerializer : IChunkSerializer
{
    private static readonly byte[] Magic = "MCRK"u8.ToArray();
    private const ushort FormatVersion = 1;
    private const int MagicSize = 4;
    private const int VersionSize = 2;
    private const int CoordSize = 4;
    private const int CountSize = 4;
    private const int CrcSize = 4;
    private const int RlePairSize = 4;
    private const int HeaderSize = MagicSize + VersionSize + CoordSize + CoordSize + CountSize + CountSize;
    private const int WorstCaseRleMultiplier = 4;

    /// <summary>
    /// Serialize the chunk data to a byte array.
    /// </summary>
    /// <param name="data">The chunk data to serialize.</param>
    /// <returns>The serialized byte array.</returns>
    public byte[] Serialize(ChunkData data)
    {
        ReadOnlySpan<ushort> rawSpan = data.GetRawSpan();
        byte[] rleBuffer = ArrayPool<byte>.Shared.Rent(ChunkData.TotalBlocks * WorstCaseRleMultiplier);

        try
        {
            int rleLength = RleEncode(rawSpan, rleBuffer);
            int totalSize = HeaderSize + rleLength + CrcSize;
            byte[] result = new byte[totalSize];
            SpanWriter writer = new(result);

            // Header
            writer.WriteBytes(Magic);
            writer.WriteUInt16(FormatVersion);
            writer.WriteInt32(data.Coord.X);
            writer.WriteInt32(data.Coord.Z);
            writer.WriteInt32(ChunkData.TotalBlocks);
            writer.WriteInt32(rleLength);

            // RLE data
            writer.WriteBytes(rleBuffer.AsSpan(0, rleLength));

            // CRC32 over everything except the CRC itself
            byte[] crc = Crc32.Hash(result.AsSpan(0, totalSize - CrcSize));
            writer.WriteBytes(crc);

            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rleBuffer);
        }
    }

    /// <summary>
    /// Deserialize binary data and load it into the target ChunkData.
    /// </summary>
    /// <param name="source">The binary data to deserialize.</param>
    /// <param name="target">The target chunk data to populate.</param>
    public void Deserialize(ReadOnlySpan<byte> source, ChunkData target)
    {
        if (source.Length < HeaderSize + CrcSize)
        {
            throw new ChunkSerializationException(
                $"Data too short ({source.Length} bytes). Minimum is {HeaderSize + CrcSize}.");
        }

        SpanReader reader = new(source);

        // Verify magic
        ReadOnlySpan<byte> magic = reader.ReadBytes(MagicSize);
        if (!magic.SequenceEqual(Magic))
        {
            throw new ChunkSerializationException("Invalid magic bytes - not a chunk file.");
        }

        // Verify version
        ushort version = reader.ReadUInt16();
        if (version != FormatVersion)
        {
            throw new ChunkSerializationException(
                $"Unsupported chunk format version {version} (expected {FormatVersion}).");
        }

        // Read header fields
        int coordX = reader.ReadInt32();
        int coordZ = reader.ReadInt32();
        int uncompressedCount = reader.ReadInt32();
        int compressedLength = reader.ReadInt32();

        if (uncompressedCount != ChunkData.TotalBlocks)
        {
            throw new ChunkSerializationException(
                $"Block count mismatch: {uncompressedCount} (expected {ChunkData.TotalBlocks}).");
        }

        if (reader.Remaining < compressedLength + CrcSize)
        {
            throw new ChunkSerializationException(
                $"Truncated data: need {compressedLength + CrcSize} more bytes, have {reader.Remaining}.");
        }

        // Verify CRC32
        int crcOffset = source.Length - CrcSize;
        ReadOnlySpan<byte> expectedCrc = source.Slice(crcOffset, CrcSize);
        byte[] actualCrc = Crc32.Hash(source[..crcOffset]);
        if (!actualCrc.AsSpan().SequenceEqual(expectedCrc))
        {
            throw new ChunkSerializationException("CRC32 checksum mismatch - data is corrupted.");
        }

        // RLE decode
        ReadOnlySpan<byte> rleData = reader.ReadBytes(compressedLength);
        ushort[] blocks = ArrayPool<ushort>.Shared.Rent(ChunkData.TotalBlocks);

        try
        {
            int decoded = RleDecode(rleData, blocks);
            if (decoded != ChunkData.TotalBlocks)
            {
                throw new ChunkSerializationException(
                    $"RLE decode produced {decoded} blocks (expected {ChunkData.TotalBlocks}).");
            }

            target.LoadFromSpan(blocks.AsSpan(0, ChunkData.TotalBlocks));
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(blocks);
        }
    }

    /// <summary>
    /// RLE encode: (count:ushort, blockId:ushort) pairs.
    /// Returns the number of bytes written.
    /// </summary>
    private static int RleEncode(ReadOnlySpan<ushort> blocks, Span<byte> output)
    {
        int position = 0;
        int i = 0;

        while (i < blocks.Length)
        {
            ushort blockId = blocks[i];
            int runLength = 1;

            while (i + runLength < blocks.Length
                   && blocks[i + runLength] == blockId
                   && runLength < ushort.MaxValue)
            {
                runLength++;
            }

            // Write count (ushort) + blockId (ushort) = 4 bytes
            BinaryPrimitives.WriteUInt16LittleEndian(output.Slice(position, 2), (ushort)runLength);
            BinaryPrimitives.WriteUInt16LittleEndian(output.Slice(position + 2, 2), blockId);
            position += RlePairSize;
            i += runLength;
        }

        return position;
    }

    /// <summary>
    /// RLE decode. Returns the number of blocks decoded.
    /// </summary>
    private static int RleDecode(ReadOnlySpan<byte> rleData, Span<ushort> output)
    {
        int rlePosition = 0;
        int outputPosition = 0;
        int minRemainingBytes = RlePairSize - 1;

        while (rlePosition + minRemainingBytes < rleData.Length)
        {
            ushort count = BinaryPrimitives.ReadUInt16LittleEndian(rleData.Slice(rlePosition, 2));
            ushort blockId = BinaryPrimitives.ReadUInt16LittleEndian(rleData.Slice(rlePosition + 2, 2));
            rlePosition += RlePairSize;

            for (int j = 0; j < count && outputPosition < output.Length; j++)
            {
                output[outputPosition++] = blockId;
            }
        }

        return outputPosition;
    }

    private ref struct SpanWriter
    {
        private readonly Span<byte> _buffer;
        private int _position;

        public SpanWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            data.CopyTo(_buffer.Slice(_position, data.Length));
            _position += data.Length;
        }

        public void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(_position, 2), value);
            _position += 2;
        }

        public void WriteInt32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(_position, 4), value);
            _position += CoordSize;
        }
    }

    private ref struct SpanReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _position;

        public SpanReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public int Remaining => _buffer.Length - _position;

        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            ReadOnlySpan<byte> slice = _buffer.Slice(_position, count);
            _position += count;
            return slice;
        }

        public ushort ReadUInt16()
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_position, 2));
            _position += 2;
            return value;
        }

        public int ReadInt32()
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_position, CoordSize));
            _position += CoordSize;
            return value;
        }
    }
}
