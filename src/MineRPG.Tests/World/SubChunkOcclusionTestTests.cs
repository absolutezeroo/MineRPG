using FluentAssertions;

using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class SubChunkOcclusionTestTests
{
    [Fact]
    public void ComputeOcclusionMask_AllEmpty_ReturnsZero()
    {
        SubChunkInfo[] subChunks = CreateEmptySubChunkInfos();
        SubChunkInfo[]?[] neighbors = CreateNullNeighbors();

        ushort mask = SubChunkOcclusionTest.ComputeOcclusionMask(subChunks, neighbors);

        mask.Should().Be(0, "empty sub-chunks are already skipped, not marked occluded");
    }

    [Fact]
    public void ComputeOcclusionMask_FullySurrounded_MarksOccluded()
    {
        // Target: sub-chunk 4 has blocks, all 6 neighbors are fully solid
        SubChunkInfo[] subChunks = CreateSubChunkInfosWithSolidExcept(4);

        SubChunkInfo[]?[] neighbors = new SubChunkInfo[]?[4];

        for (int n = 0; n < 4; n++)
        {
            neighbors[n] = CreateAllSolidSubChunkInfos();
        }

        ushort mask = SubChunkOcclusionTest.ComputeOcclusionMask(subChunks, neighbors);

        // Sub-chunk 4 has solid above (5) and solid below (3) and all 4 cardinal solid
        ((mask >> 4) & 1).Should().Be(1,
            "sub-chunk 4 is fully surrounded by solid sub-chunks");
    }

    [Fact]
    public void ComputeOcclusionMask_NoNeighborData_NotOccluded()
    {
        SubChunkInfo[] subChunks = CreateSubChunkInfosWithSolidExcept(4);
        SubChunkInfo[]?[] neighbors = CreateNullNeighbors();

        ushort mask = SubChunkOcclusionTest.ComputeOcclusionMask(subChunks, neighbors);

        ((mask >> 4) & 1).Should().Be(0,
            "without neighbor data, sub-chunk should not be marked occluded");
    }

    [Fact]
    public void ComputeOcclusionMask_TopSubChunk_TopFaceAlwaysCovered()
    {
        // Top sub-chunk (15) — its top face is the world ceiling, considered covered
        SubChunkInfo[] subChunks = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            bool isSolid = i != 15;
            int nonAirCount = isSolid ? SubChunkConstants.BlocksPerSubChunk : 100;
            subChunks[i] = new SubChunkInfo(i, false, isSolid, false, nonAirCount);
        }

        SubChunkInfo[]?[] neighbors = new SubChunkInfo[]?[4];

        for (int n = 0; n < 4; n++)
        {
            neighbors[n] = CreateAllSolidSubChunkInfos();
        }

        ushort mask = SubChunkOcclusionTest.ComputeOcclusionMask(subChunks, neighbors);

        // Sub-chunk 15: top = world ceiling (covered), bottom = 14 (solid), 4 neighbors solid
        ((mask >> 15) & 1).Should().Be(1,
            "top sub-chunk with solid below and solid neighbors should be occluded");
    }

    [Fact]
    public void ComputeOcclusionMask_BottomSubChunk_BottomFaceAlwaysCovered()
    {
        SubChunkInfo[] subChunks = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            bool isSolid = i != 0;
            int nonAirCount = isSolid ? SubChunkConstants.BlocksPerSubChunk : 100;
            subChunks[i] = new SubChunkInfo(i, false, isSolid, false, nonAirCount);
        }

        SubChunkInfo[]?[] neighbors = new SubChunkInfo[]?[4];

        for (int n = 0; n < 4; n++)
        {
            neighbors[n] = CreateAllSolidSubChunkInfos();
        }

        ushort mask = SubChunkOcclusionTest.ComputeOcclusionMask(subChunks, neighbors);

        // Sub-chunk 0: bottom = world floor (covered), top = 1 (solid), 4 neighbors solid
        (mask & 1).Should().Be(1,
            "bottom sub-chunk with solid above and solid neighbors should be occluded");
    }

    private static SubChunkInfo[] CreateEmptySubChunkInfos()
    {
        SubChunkInfo[] infos = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            infos[i] = new SubChunkInfo(i, true, false, false, 0);
        }

        return infos;
    }

    private static SubChunkInfo[] CreateAllSolidSubChunkInfos()
    {
        SubChunkInfo[] infos = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            infos[i] = new SubChunkInfo(
                i, false, true, true, SubChunkConstants.BlocksPerSubChunk);
        }

        return infos;
    }

    private static SubChunkInfo[] CreateSubChunkInfosWithSolidExcept(int nonSolidIndex)
    {
        SubChunkInfo[] infos = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            bool isSolid = i != nonSolidIndex;
            int nonAirCount = isSolid ? SubChunkConstants.BlocksPerSubChunk : 100;
            infos[i] = new SubChunkInfo(i, false, isSolid, false, nonAirCount);
        }

        return infos;
    }

    private static SubChunkInfo[]?[] CreateNullNeighbors()
    {
        return new SubChunkInfo[]?[4];
    }
}
