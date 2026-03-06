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
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

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
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(8, 4, 8, 1); // y=4 is in sub-chunk 0 (y range 0-15)

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].IsEmpty.Should().BeFalse();
        subChunks[0].IsFullySolid.Should().BeFalse();
        subChunks[0].NonAirCount.Should().Be(1);

        // All other sub-chunks should be empty
        for (int i = 1; i < SubChunkConstants.SubChunkCount; i++)
        {
            subChunks[i].IsEmpty.Should().BeTrue();
        }
    }

    [Fact]
    public void ComputeSubChunkInfo_FullSubChunk_IsFullySolid()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        // Fill sub-chunk 0 (y=0..15) completely
        for (int y = 0; y < SubChunkConstants.SubChunkSize; y++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int x = 0; x < ChunkData.SizeX; x++)
                {
                    chunk.SetBlock(x, y, z, 1);
                }
            }
        }

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].IsFullySolid.Should().BeTrue();
        subChunks[0].IsEmpty.Should().BeFalse();
        subChunks[0].NonAirCount.Should().Be(SubChunkConstants.BlocksPerSubChunk);
    }

    [Fact]
    public void ComputeSubChunkInfo_YIndex_MatchesExpectedRange()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            subChunks[i].YIndex.Should().Be(i);
            subChunks[i].MinY.Should().Be(i * SubChunkConstants.SubChunkSize);
            subChunks[i].MaxY.Should().Be((i + 1) * SubChunkConstants.SubChunkSize);
        }
    }

    [Fact]
    public void ComputeSubChunkInfo_FullSubChunk_HasFullHorizontalBarrier()
    {
        // Arrange - fully solid sub-chunk trivially has a barrier at every Y-level
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        for (int y = 0; y < SubChunkConstants.SubChunkSize; y++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int x = 0; x < ChunkData.SizeX; x++)
                {
                    chunk.SetBlock(x, y, z, 1);
                }
            }
        }

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].HasFullHorizontalBarrier.Should().BeTrue();
    }

    [Fact]
    public void ComputeSubChunkInfo_SingleSolidYSlice_HasBarrier()
    {
        // Arrange - only one Y-level (y=8) in sub-chunk 0 is fully solid
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        for (int z = 0; z < ChunkData.SizeZ; z++)
        {
            for (int x = 0; x < ChunkData.SizeX; x++)
            {
                chunk.SetBlock(x, 8, z, 1);
            }
        }

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].HasFullHorizontalBarrier.Should().BeTrue(
            "a single complete 16x16 Y-slice at y=8 forms a barrier");
        subChunks[0].IsFullySolid.Should().BeFalse(
            "only 256 out of 4096 blocks are filled");
    }

    [Fact]
    public void ComputeSubChunkInfo_OneAirBlockPerYSlice_NoBarrier()
    {
        // Arrange - fill sub-chunk 0 almost completely but leave one air block at each Y
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        for (int y = 0; y < SubChunkConstants.SubChunkSize; y++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int x = 0; x < ChunkData.SizeX; x++)
                {
                    chunk.SetBlock(x, y, z, 1);
                }
            }

            // Remove one block from each Y-level to break the barrier
            chunk.SetBlock(y % ChunkData.SizeX, y, y % ChunkData.SizeZ, 0);
        }

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks[0].HasFullHorizontalBarrier.Should().BeFalse(
            "every Y-slice has at least one air block, so no complete barrier exists");
    }

    [Fact]
    public void ComputeSubChunkInfo_EmptyChunk_NoBarriers()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        SubChunkInfo[] subChunks = chunk.ComputeSubChunkInfo();

        // Assert
        subChunks.Should().AllSatisfy(sc =>
        {
            sc.HasFullHorizontalBarrier.Should().BeFalse();
        });
    }

    [Fact]
    public void GetHighestNonAirY_EmptyChunk_ReturnsNegativeOne()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        int highest = chunk.GetHighestNonAirY();

        // Assert
        highest.Should().Be(-1);
    }

    [Fact]
    public void GetHighestNonAirY_BlockAtY100_Returns100()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(5, 100, 5, 1);

        // Act
        int highest = chunk.GetHighestNonAirY();

        // Assert
        highest.Should().Be(100);
    }

    [Fact]
    public void GetHighestNonAirY_MultipleBlocks_ReturnsHighest()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(0, 10, 0, 1);
        chunk.SetBlock(5, 200, 5, 1);
        chunk.SetBlock(8, 50, 8, 1);

        // Act
        int highest = chunk.GetHighestNonAirY();

        // Assert
        highest.Should().Be(200);
    }
}
