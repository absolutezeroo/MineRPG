using System;

using FluentAssertions;

using MineRPG.World.Chunks;
using MineRPG.World.Spatial;

namespace MineRPG.Tests.World;

public sealed class FrustumCullerTests
{
    [Fact]
    public void IsChunkVisible_InsideFrustum_ReturnsTrue()
    {
        // Arrange - a box frustum that contains chunks near origin
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, -100, -10, -100, 100, 300, 100);

        // Act
        bool isVisible = FrustumCuller.IsChunkVisible(planes, 0, 0);

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    public void IsChunkVisible_OutsideFrustum_ReturnsFalse()
    {
        // Arrange - frustum that only covers x=[0,100], z=[0,100]
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, 0, -10, 0, 100, 300, 100);

        // Act - chunk at x=-32, z=-32 (world coords -32 to -16)
        bool isVisible = FrustumCuller.IsChunkVisible(planes, -32, -32);

        // Assert
        isVisible.Should().BeFalse();
    }

    [Fact]
    public void IsChunkVisible_PartiallyInside_ReturnsTrue()
    {
        // Arrange - frustum edge cuts through the chunk
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, 8, -10, 8, 200, 300, 200);

        // Act - chunk at origin (0-16, 0-256, 0-16) overlaps with frustum starting at x=8
        bool isVisible = FrustumCuller.IsChunkVisible(planes, 0, 0);

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    public void IsSubChunkVisible_InsideFrustum_ReturnsTrue()
    {
        // Arrange
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, -100, 0, -100, 100, 256, 100);

        // Act
        bool isVisible = FrustumCuller.IsSubChunkVisible(planes, 0, 64, 0, 16);

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    public void IsSubChunkVisible_BelowFrustum_ReturnsFalse()
    {
        // Arrange - frustum only covers y=[100, 256]
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, -100, 100, -100, 100, 256, 100);

        // Act - sub-chunk at y=[0, 16]
        bool isVisible = FrustumCuller.IsSubChunkVisible(planes, 0, 0, 0, 16);

        // Assert
        isVisible.Should().BeFalse();
    }

    [Fact]
    public void FrustumPlane_IsBoxOutside_WithBoxFullyInside_ReturnsFalse()
    {
        // Arrange - plane facing +X at x=0 (everything at x>0 is inside)
        FrustumPlane plane = new FrustumPlane(1, 0, 0, 0);

        // Act - box at x=[1, 5]
        bool isOutside = plane.IsBoxOutside(1, 0, 0, 5, 5, 5);

        // Assert
        isOutside.Should().BeFalse();
    }

    [Fact]
    public void FrustumPlane_IsBoxOutside_WithBoxFullyOutside_ReturnsTrue()
    {
        // Arrange - plane facing +X at x=0
        FrustumPlane plane = new FrustumPlane(1, 0, 0, 0);

        // Act - box at x=[-5, -1]
        bool isOutside = plane.IsBoxOutside(-5, 0, 0, -1, 5, 5);

        // Assert
        isOutside.Should().BeTrue();
    }

    [Fact]
    public void ComputeVerticalOcclusionMask_NoBarriers_ReturnsZero()
    {
        // Arrange - all sub-chunks have no barrier
        SubChunkInfo[] subChunks = CreateSubChunkInfoArray(hasBarrierAt: -1);

        // Act
        ushort mask = FrustumCuller.ComputeVerticalOcclusionMask(subChunks, 100f);

        // Assert
        mask.Should().Be(0, "no barriers means nothing is occluded");
    }

    [Fact]
    public void ComputeVerticalOcclusionMask_BarrierBelowCamera_OccludesSubChunksBelow()
    {
        // Arrange - barrier at sub-chunk 4 (Y 64-79), camera at Y=100 (sub-chunk 6)
        SubChunkInfo[] subChunks = CreateSubChunkInfoArray(hasBarrierAt: 4);

        // Act
        ushort mask = FrustumCuller.ComputeVerticalOcclusionMask(subChunks, 100f);

        // Assert - sub-chunks 0-3 should be occluded (below barrier at 4)
        for (int i = 0; i < 4; i++)
        {
            ((mask >> i) & 1).Should().Be(1, $"sub-chunk {i} should be occluded below barrier at 4");
        }

        // Sub-chunk 4 (barrier) and above should NOT be occluded
        for (int i = 4; i < SubChunkConstants.SubChunkCount; i++)
        {
            ((mask >> i) & 1).Should().Be(0, $"sub-chunk {i} should not be occluded");
        }
    }

    [Fact]
    public void ComputeVerticalOcclusionMask_BarrierAboveCamera_NoOcclusion()
    {
        // Arrange - barrier at sub-chunk 8 (Y 128-143), camera at Y=50 (sub-chunk 3)
        SubChunkInfo[] subChunks = CreateSubChunkInfoArray(hasBarrierAt: 8);

        // Act
        ushort mask = FrustumCuller.ComputeVerticalOcclusionMask(subChunks, 50f);

        // Assert - barrier is above camera, so no downward occlusion
        mask.Should().Be(0, "barrier above camera does not cause downward occlusion");
    }

    [Fact]
    public void ComputeVerticalOcclusionMask_BarrierAtCameraLevel_OccludesBelow()
    {
        // Arrange - barrier at sub-chunk 4, camera at Y=70 (also sub-chunk 4)
        SubChunkInfo[] subChunks = CreateSubChunkInfoArray(hasBarrierAt: 4);

        // Act
        ushort mask = FrustumCuller.ComputeVerticalOcclusionMask(subChunks, 70f);

        // Assert - sub-chunks 0-3 should be occluded
        for (int i = 0; i < 4; i++)
        {
            ((mask >> i) & 1).Should().Be(1, $"sub-chunk {i} should be occluded");
        }
    }

    [Fact]
    public void ComputeVerticalOcclusionMask_FullySolidSubChunk_ActsAsBarrier()
    {
        // Arrange - sub-chunk 3 is fully solid (implies barrier)
        SubChunkInfo[] subChunks = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            bool isFullySolid = i == 3;
            subChunks[i] = new SubChunkInfo(i, false, isFullySolid, false, 100);
        }

        // Act - camera at Y=80 (sub-chunk 5)
        ushort mask = FrustumCuller.ComputeVerticalOcclusionMask(subChunks, 80f);

        // Assert - sub-chunks 0-2 should be occluded (below fully-solid sub-chunk 3)
        for (int i = 0; i < 3; i++)
        {
            ((mask >> i) & 1).Should().Be(1, $"sub-chunk {i} should be occluded below solid at 3");
        }

        ((mask >> 3) & 1).Should().Be(0, "the solid sub-chunk itself is not occluded");
    }

    /// <summary>
    /// Creates 6 axis-aligned planes forming a box frustum.
    /// </summary>
    private static void CreateBoxFrustum(Span<FrustumPlane> planes,
        float minX, float minY, float minZ,
        float maxX, float maxY, float maxZ)
    {
        // Left plane: normal (+1,0,0), D = -minX
        planes[0] = new FrustumPlane(1, 0, 0, -minX);
        // Right plane: normal (-1,0,0), D = maxX
        planes[1] = new FrustumPlane(-1, 0, 0, maxX);
        // Bottom plane: normal (0,+1,0), D = -minY
        planes[2] = new FrustumPlane(0, 1, 0, -minY);
        // Top plane: normal (0,-1,0), D = maxY
        planes[3] = new FrustumPlane(0, -1, 0, maxY);
        // Near plane: normal (0,0,+1), D = -minZ
        planes[4] = new FrustumPlane(0, 0, 1, -minZ);
        // Far plane: normal (0,0,-1), D = maxZ
        planes[5] = new FrustumPlane(0, 0, -1, maxZ);
    }

    private static SubChunkInfo[] CreateSubChunkInfoArray(int hasBarrierAt)
    {
        SubChunkInfo[] subChunks = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            bool hasBarrier = i == hasBarrierAt;
            subChunks[i] = new SubChunkInfo(i, false, false, hasBarrier, 100);
        }

        return subChunks;
    }
}
