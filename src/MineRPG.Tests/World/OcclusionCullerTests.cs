using System;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Spatial;

namespace MineRPG.Tests.World;

public sealed class OcclusionCullerTests
{
    [Fact]
    public void IsChunkVisible_BeforeUpdate_ReturnsTrue()
    {
        OcclusionCuller culler = new();

        bool isVisible = culler.IsChunkVisible(new ChunkCoord(5, 5));

        isVisible.Should().BeTrue(
            "before any BFS update, all chunks should be considered visible");
    }

    [Fact]
    public void Update_PlayerChunk_AlwaysVisible()
    {
        OcclusionCuller culler = new();
        ChunkCoord playerChunk = new(0, 0);

        // Set all surrounding chunks as opaque
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                culler.SetMatrix(new ChunkCoord(x, z), ChunkVisibilityMatrix.Opaque);
            }
        }

        // Player chunk is transparent (they're standing in it)
        culler.SetMatrix(playerChunk, ChunkVisibilityMatrix.AllVisible);

        culler.Update(playerChunk, 16, ReadOnlySpan<FrustumPlane>.Empty);

        culler.IsChunkVisible(playerChunk).Should().BeTrue(
            "player's chunk should always be visible");
    }

    [Fact]
    public void Update_TransparentNeighbors_AreVisible()
    {
        OcclusionCuller culler = new();
        ChunkCoord playerChunk = new(0, 0);

        // All chunks are transparent
        for (int x = -3; x <= 3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                culler.SetMatrix(new ChunkCoord(x, z), ChunkVisibilityMatrix.AllVisible);
            }
        }

        culler.Update(playerChunk, 16, ReadOnlySpan<FrustumPlane>.Empty);

        culler.IsChunkVisible(new ChunkCoord(1, 0)).Should().BeTrue();
        culler.IsChunkVisible(new ChunkCoord(0, 1)).Should().BeTrue();
        culler.IsChunkVisible(new ChunkCoord(2, 0)).Should().BeTrue();
    }

    [Fact]
    public void Update_OpaqueWall_BlocksChunksBehind()
    {
        OcclusionCuller culler = new();
        ChunkCoord playerChunk = new(0, 0);
        int renderDistance = 5;

        // Fill entire area with transparent chunks
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                culler.SetMatrix(new ChunkCoord(x, z), ChunkVisibilityMatrix.AllVisible);
            }
        }

        // Wall of opaque chunks completely enclosing the east side at X=2
        // The wall must extend to the render distance boundary to prevent BFS from wrapping
        for (int z = -renderDistance; z <= renderDistance; z++)
        {
            culler.SetMatrix(new ChunkCoord(2, z), ChunkVisibilityMatrix.Opaque);
        }

        culler.Update(playerChunk, renderDistance, ReadOnlySpan<FrustumPlane>.Empty);

        // Chunks behind the opaque wall should not be visible
        culler.IsChunkVisible(new ChunkCoord(3, 0)).Should().BeFalse(
            "chunk behind opaque wall should be hidden");
    }

    [Fact]
    public void RemoveMatrix_InvalidatesResults()
    {
        OcclusionCuller culler = new();
        ChunkCoord coord = new(1, 0);

        culler.SetMatrix(coord, ChunkVisibilityMatrix.Opaque);
        culler.RemoveMatrix(coord);

        culler.MatrixCount.Should().Be(0);
    }

    [Fact]
    public void Update_WithRenderDistanceBound_DoesNotGoBeyond()
    {
        OcclusionCuller culler = new();
        ChunkCoord playerChunk = new(0, 0);

        // All chunks transparent
        for (int x = -20; x <= 20; x++)
        {
            for (int z = -20; z <= 20; z++)
            {
                culler.SetMatrix(new ChunkCoord(x, z), ChunkVisibilityMatrix.AllVisible);
            }
        }

        culler.Update(playerChunk, 4, ReadOnlySpan<FrustumPlane>.Empty);

        // Chunks beyond render distance should not be in visible set
        culler.IsChunkVisible(new ChunkCoord(5, 0)).Should().BeFalse(
            "chunk beyond render distance should not be visible");
    }
}
