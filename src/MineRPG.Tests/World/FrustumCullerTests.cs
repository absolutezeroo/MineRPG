using System;

using FluentAssertions;

using MineRPG.World.Spatial;

namespace MineRPG.Tests.World;

public sealed class FrustumCullerTests
{
    [Fact]
    public void IsChunkVisible_InsideFrustum_ReturnsTrue()
    {
        // Arrange — a box frustum that contains chunks near origin
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
        // Arrange — frustum that only covers x=[0,100], z=[0,100]
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, 0, -10, 0, 100, 300, 100);

        // Act — chunk at x=-32, z=-32 (world coords -32 to -16)
        bool isVisible = FrustumCuller.IsChunkVisible(planes, -32, -32);

        // Assert
        isVisible.Should().BeFalse();
    }

    [Fact]
    public void IsChunkVisible_PartiallyInside_ReturnsTrue()
    {
        // Arrange — frustum edge cuts through the chunk
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, 8, -10, 8, 200, 300, 200);

        // Act — chunk at origin (0-16, 0-256, 0-16) overlaps with frustum starting at x=8
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
        // Arrange — frustum only covers y=[100, 256]
        Span<FrustumPlane> planes = stackalloc FrustumPlane[6];
        CreateBoxFrustum(planes, -100, 100, -100, 100, 256, 100);

        // Act — sub-chunk at y=[0, 16]
        bool isVisible = FrustumCuller.IsSubChunkVisible(planes, 0, 0, 0, 16);

        // Assert
        isVisible.Should().BeFalse();
    }

    [Fact]
    public void FrustumPlane_IsBoxOutside_WithBoxFullyInside_ReturnsFalse()
    {
        // Arrange — plane facing +X at x=0 (everything at x>0 is inside)
        FrustumPlane plane = new FrustumPlane(1, 0, 0, 0);

        // Act — box at x=[1, 5]
        bool isOutside = plane.IsBoxOutside(1, 0, 0, 5, 5, 5);

        // Assert
        isOutside.Should().BeFalse();
    }

    [Fact]
    public void FrustumPlane_IsBoxOutside_WithBoxFullyOutside_ReturnsTrue()
    {
        // Arrange — plane facing +X at x=0
        FrustumPlane plane = new FrustumPlane(1, 0, 0, 0);

        // Act — box at x=[-5, -1]
        bool isOutside = plane.IsBoxOutside(-5, 0, 0, -1, 5, 5);

        // Assert
        isOutside.Should().BeTrue();
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
}
