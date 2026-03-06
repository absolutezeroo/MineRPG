using System;

using FluentAssertions;

using MineRPG.World.Chunks;
using MineRPG.World.Chunks.Serialization;

namespace MineRPG.Tests.World;

public sealed class PaletteCompressorTests
{
    [Fact]
    public void Compress_AllAir_ProducesSinglePaletteEntry()
    {
        // Arrange
        ushort[] blocks = new ushort[ChunkData.TotalBlocks];

        // Act
        PaletteChunkData? result = PaletteCompressor.Compress(blocks);

        // Assert
        result.Should().NotBeNull();
        result!.PaletteSize.Should().Be(1);
        result.Palette[0].Should().Be(0);
    }

    [Fact]
    public void Compress_FewBlockTypes_CompressesSuccessfully()
    {
        // Arrange
        ushort[] blocks = new ushort[ChunkData.TotalBlocks];
        blocks[0] = 1;   // Stone
        blocks[1] = 2;   // Dirt
        blocks[100] = 3; // Grass

        // Act
        PaletteChunkData? result = PaletteCompressor.Compress(blocks);

        // Assert
        result.Should().NotBeNull();
        result!.PaletteSize.Should().Be(4); // air + stone + dirt + grass
    }

    [Fact]
    public void CompressDecompress_RoundTrip_ProducesIdenticalData()
    {
        // Arrange
        ushort[] original = new ushort[ChunkData.TotalBlocks];
        Random rng = new Random(42);
        ushort[] blockTypes = new ushort[] { 0, 1, 2, 3, 4, 5 };

        for (int i = 0; i < original.Length; i++)
        {
            original[i] = blockTypes[rng.Next(blockTypes.Length)];
        }

        // Act
        PaletteChunkData? compressed = PaletteCompressor.Compress(original);
        compressed.Should().NotBeNull();

        ushort[] decompressed = new ushort[ChunkData.TotalBlocks];
        PaletteCompressor.Decompress(compressed!, decompressed);

        // Assert
        decompressed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Compress_TooManyDistinctTypes_ReturnsNull()
    {
        // Arrange - more than 256 distinct block types
        ushort[] blocks = new ushort[ChunkData.TotalBlocks];
        for (int i = 0; i < 257 && i < blocks.Length; i++)
        {
            blocks[i] = (ushort)i;
        }

        // Act
        PaletteChunkData? result = PaletteCompressor.Compress(blocks);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EstimateCompressionRatio_FewTypes_LessThanOne()
    {
        // Arrange
        ushort[] blocks = new ushort[ChunkData.TotalBlocks];
        blocks[0] = 1;

        // Act
        float ratio = PaletteCompressor.EstimateCompressionRatio(blocks);

        // Assert
        ratio.Should().BeLessThan(1.0f, "palette with 2 types should be smaller than raw");
    }

    [Fact]
    public void EstimateCompressionRatio_TooManyTypes_ReturnsOne()
    {
        // Arrange
        ushort[] blocks = new ushort[ChunkData.TotalBlocks];
        for (int i = 0; i < 257 && i < blocks.Length; i++)
        {
            blocks[i] = (ushort)i;
        }

        // Act
        float ratio = PaletteCompressor.EstimateCompressionRatio(blocks);

        // Assert
        ratio.Should().Be(1.0f);
    }

    [Fact]
    public void PaletteChunkData_GetBlock_ReturnsCorrectBlockId()
    {
        // Arrange
        ushort[] blocks = new ushort[1024];
        blocks[0] = 5;
        blocks[1] = 3;
        blocks[100] = 5;

        PaletteChunkData? compressed = PaletteCompressor.Compress(blocks);
        compressed.Should().NotBeNull();

        // Act & Assert
        compressed!.GetBlock(0).Should().Be(5);
        compressed.GetBlock(1).Should().Be(3);
        compressed.GetBlock(100).Should().Be(5);
        compressed.GetBlock(2).Should().Be(0); // Air
    }

    [Fact]
    public void PaletteChunkData_EstimatedBytes_IsLessThanRaw()
    {
        // Arrange
        ushort[] blocks = new ushort[ChunkData.TotalBlocks];
        blocks[0] = 1;

        // Act
        PaletteChunkData? compressed = PaletteCompressor.Compress(blocks);

        // Assert
        compressed.Should().NotBeNull();
        int rawBytes = ChunkData.TotalBlocks * 2;
        compressed!.EstimatedBytes.Should().BeLessThan(rawBytes);
    }
}
