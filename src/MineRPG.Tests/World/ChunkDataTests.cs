using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class ChunkDataTests
{
    [Fact]
    public void SetBlock_ThenGetBlock_ReturnsSameId()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);

        // Act
        chunk.SetBlock(5, 64, 3, 42);

        // Assert
        chunk.GetBlock(5, 64, 3).Should().Be(42);
    }

    [Fact]
    public void SetBlock_AtOrigin_DoesNotAffectOtherBlocks()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(0, 0, 0, 1);

        // Assert
        chunk.GetBlock(1, 0, 0).Should().Be(0);
        chunk.GetBlock(0, 1, 0).Should().Be(0);
    }

    [Fact]
    public void GetIndex_MatchesVoxelMath()
    {
        // Act & Assert
        int index = ChunkData.GetIndex(3, 10, 7);
        index.Should().Be(VoxelMath.GetIndex(3, 10, 7, ChunkData.SizeX, ChunkData.SizeZ));
    }

    [Fact]
    public void IsInBounds_WithValidCoords_ReturnsTrue()
    {
        // Act & Assert
        ChunkData.IsInBounds(0, 0, 0).Should().BeTrue();
        ChunkData.IsInBounds(15, 255, 15).Should().BeTrue();
    }

    [Fact]
    public void IsInBounds_WithOutOfRangeCoords_ReturnsFalse()
    {
        // Act & Assert
        ChunkData.IsInBounds(-1, 0, 0).Should().BeFalse();
        ChunkData.IsInBounds(16, 0, 0).Should().BeFalse();
        ChunkData.IsInBounds(0, 256, 0).Should().BeFalse();
    }
}
