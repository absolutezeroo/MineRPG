using FluentAssertions;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class SubChunkInfoTests
{
    [Fact]
    public void ComputeSubChunkInfo_EmptyChunk_AllSubChunksEmpty()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        var subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks.Should().HaveCount(SubChunkConstants.SubChunkCount);
        subChunks.Should().AllSatisfy(sc =>
        {
            sc.IsEmpty.Should().BeTrue();
            sc.IsFullySolid.Should().BeFalse();
            sc.NonAirCount.Should().Be(0);
        });
    }

    [Fact]
    public void ComputeSubChunkInfo_SingleBlockInSubChunk_NotEmptyNotSolid()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(8, 4, 8, 1); // y=4 is in sub-chunk 0 (y range 0-15)

        // Act
        var subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].IsEmpty.Should().BeFalse();
        subChunks[0].IsFullySolid.Should().BeFalse();
        subChunks[0].NonAirCount.Should().Be(1);

        // All other sub-chunks should be empty
        for (var i = 1; i < SubChunkConstants.SubChunkCount; i++)
        {
            subChunks[i].IsEmpty.Should().BeTrue();
        }
    }

    [Fact]
    public void ComputeSubChunkInfo_FullSubChunk_IsFullySolid()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);

        // Fill sub-chunk 0 (y=0..15) completely
        for (var y = 0; y < SubChunkConstants.SubChunkSize; y++)
        for (var z = 0; z < ChunkData.SizeZ; z++)
        for (var x = 0; x < ChunkData.SizeX; x++)
        {
            chunk.SetBlock(x, y, z, 1);
        }

        // Act
        var subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].IsFullySolid.Should().BeTrue();
        subChunks[0].IsEmpty.Should().BeFalse();
        subChunks[0].NonAirCount.Should().Be(SubChunkConstants.BlocksPerSubChunk);
    }

    [Fact]
    public void ComputeSubChunkInfo_YIndex_MatchesExpectedRange()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        var subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        for (var i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            subChunks[i].YIndex.Should().Be(i);
            subChunks[i].MinY.Should().Be(i * SubChunkConstants.SubChunkSize);
            subChunks[i].MaxY.Should().Be((i + 1) * SubChunkConstants.SubChunkSize);
        }
    }

    [Fact]
    public void GetHighestNonAirY_EmptyChunk_ReturnsNegativeOne()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        var highest = chunk.GetHighestNonAirY();

        // Assert
        highest.Should().Be(-1);
    }

    [Fact]
    public void GetHighestNonAirY_BlockAtY100_Returns100()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(5, 100, 5, 1);

        // Act
        var highest = chunk.GetHighestNonAirY();

        // Assert
        highest.Should().Be(100);
    }

    [Fact]
    public void GetHighestNonAirY_MultipleBlocks_ReturnsHighest()
    {
        // Arrange
        var chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(0, 10, 0, 1);
        chunk.SetBlock(5, 200, 5, 1);
        chunk.SetBlock(8, 50, 8, 1);

        // Act
        var highest = chunk.GetHighestNonAirY();

        // Assert
        highest.Should().Be(200);
    }
}
