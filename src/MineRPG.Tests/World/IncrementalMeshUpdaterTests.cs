using System.Collections.Generic;

using FluentAssertions;

using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Tests.World;

public sealed class IncrementalMeshUpdaterTests
{
    [Fact]
    public void GetAffectedSubChunks_MiddleOfSubChunk_ReturnsOneIndex()
    {
        List<int> indices = new();

        IncrementalMeshUpdater.GetAffectedSubChunks(40, indices);

        indices.Should().ContainSingle()
            .Which.Should().Be(2, "Y=40 is in sub-chunk 2 (40/16=2)");
    }

    [Fact]
    public void GetAffectedSubChunks_BottomBoundary_ReturnsTwoIndices()
    {
        List<int> indices = new();

        // Y=32 is the bottom edge of sub-chunk 2 (32%16==0)
        IncrementalMeshUpdater.GetAffectedSubChunks(32, indices);

        indices.Should().HaveCount(2);
        indices.Should().Contain(2, "the sub-chunk containing the block");
        indices.Should().Contain(1, "the sub-chunk below the boundary");
    }

    [Fact]
    public void GetAffectedSubChunks_TopBoundary_ReturnsTwoIndices()
    {
        List<int> indices = new();

        // Y=31 is the top edge of sub-chunk 1 (31%16==15)
        IncrementalMeshUpdater.GetAffectedSubChunks(31, indices);

        indices.Should().HaveCount(2);
        indices.Should().Contain(1, "the sub-chunk containing the block");
        indices.Should().Contain(2, "the sub-chunk above the boundary");
    }

    [Fact]
    public void GetAffectedSubChunks_AtY0_ReturnsOnlySubChunk0()
    {
        List<int> indices = new();

        IncrementalMeshUpdater.GetAffectedSubChunks(0, indices);

        // Y=0 is bottom of sub-chunk 0. No sub-chunk below, so only 1 result.
        indices.Should().ContainSingle()
            .Which.Should().Be(0);
    }

    [Fact]
    public void GetAffectedSubChunks_AtMaxY_ReturnsOnlyTopSubChunk()
    {
        List<int> indices = new();
        int maxY = ChunkData.SizeY - 1; // 255

        IncrementalMeshUpdater.GetAffectedSubChunks(maxY, indices);

        // Y=255 is the top of sub-chunk 15. 255%16==15 but no sub-chunk above.
        indices.Should().ContainSingle()
            .Which.Should().Be(SubChunkConstants.SubChunkCount - 1);
    }

    [Fact]
    public void GetAffectedSubChunks_ClearsListBeforePopulating()
    {
        List<int> indices = new() { 99, 100 };

        IncrementalMeshUpdater.GetAffectedSubChunks(40, indices);

        indices.Should().NotContain(99);
        indices.Should().NotContain(100);
    }

    [Fact]
    public void IsOnChunkBorder_CenterBlock_ReturnsFalse()
    {
        bool isOnBorder = IncrementalMeshUpdater.IsOnChunkBorder(8, 8);

        isOnBorder.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 8)]
    [InlineData(15, 8)]
    [InlineData(8, 0)]
    [InlineData(8, 15)]
    [InlineData(0, 0)]
    [InlineData(15, 15)]
    public void IsOnChunkBorder_EdgeBlock_ReturnsTrue(int localX, int localZ)
    {
        bool isOnBorder = IncrementalMeshUpdater.IsOnChunkBorder(localX, localZ);

        isOnBorder.Should().BeTrue();
    }

    [Fact]
    public void GetAffectedSubChunkMask_MiddleOfSubChunk_ReturnsSingleBit()
    {
        ushort mask = IncrementalMeshUpdater.GetAffectedSubChunkMask(40);

        // Sub-chunk 2 → bit 2
        mask.Should().Be(1 << 2);
    }

    [Fact]
    public void GetAffectedSubChunkMask_BottomBoundary_ReturnsTwoBits()
    {
        ushort mask = IncrementalMeshUpdater.GetAffectedSubChunkMask(32);

        // Sub-chunk 2 + sub-chunk 1
        int expected = (1 << 2) | (1 << 1);
        mask.Should().Be((ushort)expected);
    }

    [Fact]
    public void GetAffectedSubChunkMask_TopBoundary_ReturnsTwoBits()
    {
        ushort mask = IncrementalMeshUpdater.GetAffectedSubChunkMask(31);

        // Sub-chunk 1 + sub-chunk 2
        int expected = (1 << 1) | (1 << 2);
        mask.Should().Be((ushort)expected);
    }

    [Fact]
    public void GetAffectedSubChunkMask_AgreesWithGetAffectedSubChunks()
    {
        List<int> indices = new();

        for (int y = 0; y < ChunkData.SizeY; y++)
        {
            IncrementalMeshUpdater.GetAffectedSubChunks(y, indices);
            ushort mask = IncrementalMeshUpdater.GetAffectedSubChunkMask(y);

            ushort expectedMask = 0;

            foreach (int index in indices)
            {
                expectedMask |= (ushort)(1 << index);
            }

            mask.Should().Be(expectedMask,
                "mask and list should agree for Y={0}", y);
        }
    }
}
