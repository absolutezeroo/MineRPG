using System.Buffers;
using System.IO.Hashing;

namespace MineRPG.World.Chunks;

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
    private const int HeaderSize = 4 + 2 + 4 + 4 + 4 + 4; // magic + version + coordX + coordZ + uncompLen + compLen

    public byte[] Serialize(ChunkData data)
    {
        // RLE encode the block data
        var rawSpan = data.GetRawSpan();
        var rleBuffer = ArrayPool<byte>.Shared.Rent(ChunkData.TotalBlocks * 4); // worst case
        int rleLen;

        try
        {
            rleLen = RleEncode(rawSpan, rleBuffer);
            var totalSize = HeaderSize + rleLen + 4; // +4 for CRC32

            var result = new byte[totalSize];
            var writer = new SpanWriter(result);

            // Header
            writer.WriteBytes(Magic);
            writer.WriteUInt16(FormatVersion);
            writer.WriteInt32(data.Coord.X);
            writer.WriteInt32(data.Coord.Z);
            writer.WriteInt32(ChunkData.TotalBlocks);
            writer.WriteInt32(rleLen);

            // RLE data
            writer.WriteBytes(rleBuffer.AsSpan(0, rleLen));

            // CRC32 over everything except the CRC itself
            var crc = Crc32.Hash(result.AsSpan(0, totalSize - 4));
            writer.WriteBytes(crc);

            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rleBuffer);
        }
    }

    public void Deserialize(ReadOnlySpan<byte> source, ChunkData target)
    {
        if (source.Length < HeaderSize + 4)
            throw new ChunkSerializationException(
                $"Data too short ({source.Length} bytes). Minimum is {HeaderSize + 4}.");

        var reader = new SpanReader(source);

        // Verify magic
        var magic = reader.ReadBytes(4);
        if (!magic.SequenceEqual(Magic))
            throw new ChunkSerializationException("Invalid magic bytes — not a chunk file.");

        // Verify version
        var version = reader.ReadUInt16();
        if (version != FormatVersion)
            throw new ChunkSerializationException(
                $"Unsupported chunk format version {version} (expected {FormatVersion}).");

        // Read header
        var coordX = reader.ReadInt32();
        var coordZ = reader.ReadInt32();
        var uncompressedCount = reader.ReadInt32();
        var compressedLen = reader.ReadInt32();

        if (uncompressedCount != ChunkData.TotalBlocks)
            throw new ChunkSerializationException(
                $"Block count mismatch: {uncompressedCount} (expected {ChunkData.TotalBlocks}).");

        if (reader.Remaining < compressedLen + 4)
            throw new ChunkSerializationException(
                $"Truncated data: need {compressedLen + 4} more bytes, have {reader.Remaining}.");

        // Verify CRC32
        var crcOffset = source.Length - 4;
        var expectedCrc = source.Slice(crcOffset, 4);
        var actualCrc = Crc32.Hash(source[..crcOffset]);
        if (!actualCrc.AsSpan().SequenceEqual(expectedCrc))
            throw new ChunkSerializationException("CRC32 checksum mismatch — data is corrupted.");

        // RLE decode
        var rleData = reader.ReadBytes(compressedLen);
        var blocks = ArrayPool<ushort>.Shared.Rent(ChunkData.TotalBlocks);
        try
        {
            var decoded = RleDecode(rleData, blocks);
            if (decoded != ChunkData.TotalBlocks)
                throw new ChunkSerializationException(
                    $"RLE decode produced {decoded} blocks (expected {ChunkData.TotalBlocks}).");

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
        var pos = 0;
        var i = 0;
        while (i < blocks.Length)
        {
            var blockId = blocks[i];
            var runLength = 1;
            while (i + runLength < blocks.Length
                   && blocks[i + runLength] == blockId
                   && runLength < ushort.MaxValue)
            {
                runLength++;
            }

            // Write count (ushort) + blockId (ushort) = 4 bytes
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(output.Slice(pos, 2), (ushort)runLength);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(output.Slice(pos + 2, 2), blockId);
            pos += 4;
            i += runLength;
        }

        return pos;
    }

    /// <summary>
    /// RLE decode. Returns the number of blocks decoded.
    /// </summary>
    private static int RleDecode(ReadOnlySpan<byte> rleData, Span<ushort> output)
    {
        var rlePos = 0;
        var outPos = 0;
        while (rlePos + 3 < rleData.Length)
        {
            var count = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(rleData.Slice(rlePos, 2));
            var blockId = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(rleData.Slice(rlePos + 2, 2));
            rlePos += 4;

            for (var j = 0; j < count && outPos < output.Length; j++)
                output[outPos++] = blockId;
        }

        return outPos;
    }

    private ref struct SpanWriter
    {
        private readonly Span<byte> _buffer;
        private int _pos;

        public SpanWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            data.CopyTo(_buffer.Slice(_pos, data.Length));
            _pos += data.Length;
        }

        public void WriteUInt16(ushort value)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(_pos, 2), value);
            _pos += 2;
        }

        public void WriteInt32(int value)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(_pos, 4), value);
            _pos += 4;
        }
    }

    private ref struct SpanReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _pos;

        public SpanReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public int Remaining => _buffer.Length - _pos;

        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            var slice = _buffer.Slice(_pos, count);
            _pos += count;
            return slice;
        }

        public ushort ReadUInt16()
        {
            var value = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_pos, 2));
            _pos += 2;
            return value;
        }

        public int ReadInt32()
        {
            var value = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_pos, 4));
            _pos += 4;
            return value;
        }
    }
}
