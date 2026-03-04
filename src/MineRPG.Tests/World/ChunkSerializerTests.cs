using System;
using System.Diagnostics;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class ChunkSerializerTests
{
    private readonly ChunkSerializer _serializer = new();

    [Fact]
    public void RoundTrip_EmptyChunk_PreservesAllBlocks()
    {
        // Arrange — all air (0)
        ChunkData original = new ChunkData(new ChunkCoord(3, 7));

        // Act
        byte[] bytes = _serializer.Serialize(original);
        ChunkData restored = new ChunkData(new ChunkCoord(3, 7));
        _serializer.Deserialize(bytes, restored);

        // Assert
        restored.GetRawSpan().SequenceEqual(original.GetRawSpan()).Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_MixedBlocks_PreservesAllBlocks()
    {
        // Arrange
        ChunkData original = new ChunkData(new ChunkCoord(5, -2));
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                // Stone layer 0-63, dirt 64-127, grass at 128, air above
                for (int y = 0; y < 64; y++)
                {
                    original.SetBlock(x, y, z, 1); // stone
                }

                for (int y = 64; y < 128; y++)
                {
                    original.SetBlock(x, y, z, 2); // dirt
                }

                original.SetBlock(x, 128, z, 3); // grass
            }
        }

        // Act
        byte[] bytes = _serializer.Serialize(original);
        ChunkData restored = new ChunkData(new ChunkCoord(5, -2));
        _serializer.Deserialize(bytes, restored);

        // Assert
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                restored.GetBlock(x, 0, z).Should().Be(1);
                restored.GetBlock(x, 64, z).Should().Be(2);
                restored.GetBlock(x, 128, z).Should().Be(3);
                restored.GetBlock(x, 200, z).Should().Be(0);
            }
        }
    }

    [Fact]
    public void RoundTrip_WorstCase_AllUniqueBlocks_PreservesData()
    {
        // Arrange — alternating block IDs to minimize RLE compression
        ChunkData original = new ChunkData(new ChunkCoord(0, 0));
        for (int y = 0; y < ChunkData.SizeY; y++)
        {
            for (int x = 0; x < ChunkData.SizeX; x++)
            {
                for (int z = 0; z < ChunkData.SizeZ; z++)
                {
                    int index = ChunkData.GetIndex(x, y, z);
                    original.SetBlock(x, y, z, (ushort)(index % 256));
                }
            }
        }

        // Act
        byte[] bytes = _serializer.Serialize(original);
        ChunkData restored = new ChunkData(new ChunkCoord(0, 0));
        _serializer.Deserialize(bytes, restored);

        // Assert
        restored.GetRawSpan().SequenceEqual(original.GetRawSpan()).Should().BeTrue();
    }

    [Fact]
    public void Serialize_EmptyChunk_CompressesWell()
    {
        // Arrange — all air → should compress to a single RLE run
        ChunkData chunk = new ChunkData(new ChunkCoord(0, 0));

        // Act
        byte[] bytes = _serializer.Serialize(chunk);

        // Assert — header(22) + 1 RLE pair(4) + CRC(4) = 30 bytes
        bytes.Length.Should().BeLessThan(100);
    }

    [Fact]
    public void Deserialize_TruncatedData_ThrowsChunkSerializationException()
    {
        // Arrange
        byte[] tooShort = new byte[10];

        // Act
        Action act = () => _serializer.Deserialize(tooShort, new ChunkData(new ChunkCoord(0, 0)));

        // Assert
        act.Should().Throw<ChunkSerializationException>()
            .WithMessage("*too short*");
    }

    [Fact]
    public void Deserialize_InvalidMagic_ThrowsChunkSerializationException()
    {
        // Arrange — serialize a valid chunk, then corrupt the magic
        ChunkData chunk = new ChunkData(new ChunkCoord(0, 0));
        byte[] bytes = _serializer.Serialize(chunk);
        bytes[0] = (byte)'X';

        // Act
        Action act = () => _serializer.Deserialize(bytes, new ChunkData(new ChunkCoord(0, 0)));

        // Assert
        act.Should().Throw<ChunkSerializationException>()
            .WithMessage("*magic*");
    }

    [Fact]
    public void Deserialize_CorruptedCrc_ThrowsChunkSerializationException()
    {
        // Arrange — serialize a valid chunk, then flip a byte in the data
        ChunkData chunk = new ChunkData(new ChunkCoord(0, 0));
        chunk.SetBlock(0, 0, 0, 1);
        byte[] bytes = _serializer.Serialize(chunk);

        // Corrupt a byte in the RLE data region (after header, before CRC)
        bytes[24] ^= 0xFF;

        // Act
        Action act = () => _serializer.Deserialize(bytes, new ChunkData(new ChunkCoord(0, 0)));

        // Assert
        act.Should().Throw<ChunkSerializationException>()
            .WithMessage("*CRC32*");
    }

    [Fact]
    public void Deserialize_WrongVersion_ThrowsChunkSerializationException()
    {
        // Arrange — serialize a valid chunk, then change version byte
        ChunkData chunk = new ChunkData(new ChunkCoord(0, 0));
        byte[] bytes = _serializer.Serialize(chunk);

        // Version is at offset 4 (after 4-byte magic), as little-endian ushort
        bytes[4] = 99;
        bytes[5] = 0;

        // Act
        Action act = () => _serializer.Deserialize(bytes, new ChunkData(new ChunkCoord(0, 0)));

        // Assert
        act.Should().Throw<ChunkSerializationException>()
            .WithMessage("*version*");
    }

    [Fact]
    public void Serialize_Performance_CompletesUnder5ms()
    {
        // Arrange — realistic terrain chunk
        ChunkData chunk = new ChunkData(new ChunkCoord(0, 0));
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 0; y < 64; y++)
                {
                    chunk.SetBlock(x, y, z, 1);
                }

                for (int y = 64; y < 80; y++)
                {
                    chunk.SetBlock(x, y, z, 2);
                }

                chunk.SetBlock(x, 80, z, 3);
            }
        }

        // Warmup
        _serializer.Serialize(chunk);

        // Act
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _serializer.Serialize(chunk);
        }

        sw.Stop();

        // Assert — average < 5ms
        double avgMs = sw.Elapsed.TotalMilliseconds / 100;
        avgMs.Should().BeLessThan(5.0, "serialization should complete under 5ms per chunk");
    }

    [Fact]
    public void Deserialize_Performance_CompletesUnder3ms()
    {
        // Arrange
        ChunkData chunk = new ChunkData(new ChunkCoord(0, 0));
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 0; y < 64; y++)
                {
                    chunk.SetBlock(x, y, z, 1);
                }

                for (int y = 64; y < 80; y++)
                {
                    chunk.SetBlock(x, y, z, 2);
                }

                chunk.SetBlock(x, 80, z, 3);
            }
        }

        byte[] bytes = _serializer.Serialize(chunk);

        // Warmup
        _serializer.Deserialize(bytes, new ChunkData(new ChunkCoord(0, 0)));

        // Act
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _serializer.Deserialize(bytes, new ChunkData(new ChunkCoord(0, 0)));
        }

        sw.Stop();

        // Assert — average < 3ms
        double avgMs = sw.Elapsed.TotalMilliseconds / 100;
        avgMs.Should().BeLessThan(3.0, "deserialization should complete under 3ms per chunk");
    }
}
