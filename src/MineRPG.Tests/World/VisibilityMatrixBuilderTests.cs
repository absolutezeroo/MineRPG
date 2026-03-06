using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Spatial;

namespace MineRPG.Tests.World;

public sealed class VisibilityMatrixBuilderTests
{
    [Fact]
    public void Build_EmptyChunk_ReturnsAllVisible()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        SubChunkInfo[] infos = chunk.ComputeSubChunkInfo();

        ChunkVisibilityMatrix matrix = VisibilityMatrixBuilder.Build(chunk, infos);

        matrix.IsFullyTransparent.Should().BeTrue(
            "an entirely empty chunk should be fully transparent");
    }

    [Fact]
    public void Build_FullySolidChunk_ReturnsOpaque()
    {
        ChunkData chunk = CreateSolidChunk();
        SubChunkInfo[] infos = chunk.ComputeSubChunkInfo();

        ChunkVisibilityMatrix matrix = VisibilityMatrixBuilder.Build(chunk, infos);

        matrix.IsFullyOpaque.Should().BeTrue(
            "a fully solid chunk should block all visibility");
    }

    [Fact]
    public void Build_NorthSouthTunnel_AllowsNorthSouthPassage()
    {
        ChunkData chunk = CreateSolidChunk();

        // Carve a tunnel from Z=0 to Z=15 at X=8, Y=64
        for (int z = 0; z < ChunkData.SizeZ; z++)
        {
            chunk.SetBlock(8, 64, z, 0);
        }

        SubChunkInfo[] infos = chunk.ComputeSubChunkInfo();

        ChunkVisibilityMatrix matrix = VisibilityMatrixBuilder.Build(chunk, infos);

        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceNorth, ChunkVisibilityMatrix.FaceSouth)
            .Should().BeTrue("tunnel runs from north to south face");
        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceSouth, ChunkVisibilityMatrix.FaceNorth)
            .Should().BeTrue("tunnel is bidirectional");
    }

    [Fact]
    public void Build_EastWestTunnel_AllowsEastWestPassage()
    {
        ChunkData chunk = CreateSolidChunk();

        // Carve a tunnel from X=0 to X=15 at Z=8, Y=64
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            chunk.SetBlock(x, 64, 8, 0);
        }

        SubChunkInfo[] infos = chunk.ComputeSubChunkInfo();

        ChunkVisibilityMatrix matrix = VisibilityMatrixBuilder.Build(chunk, infos);

        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceEast, ChunkVisibilityMatrix.FaceWest)
            .Should().BeTrue("tunnel runs from east to west face");
        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceWest, ChunkVisibilityMatrix.FaceEast)
            .Should().BeTrue("tunnel is bidirectional");
    }

    [Fact]
    public void Build_NorthSouthTunnel_BlocksEastWest()
    {
        ChunkData chunk = CreateSolidChunk();

        // Carve a tunnel only from Z=0 to Z=15 (north-south)
        for (int z = 0; z < ChunkData.SizeZ; z++)
        {
            chunk.SetBlock(8, 64, z, 0);
        }

        SubChunkInfo[] infos = chunk.ComputeSubChunkInfo();

        ChunkVisibilityMatrix matrix = VisibilityMatrixBuilder.Build(chunk, infos);

        // East-west passage should be blocked (tunnel doesn't reach those faces)
        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceEast, ChunkVisibilityMatrix.FaceWest)
            .Should().BeFalse("tunnel only goes N-S, not E-W");
    }

    [Fact]
    public void Build_NullSubChunkInfos_StillWorks()
    {
        ChunkData chunk = new(ChunkCoord.Zero);

        ChunkVisibilityMatrix matrix = VisibilityMatrixBuilder.Build(chunk, null);

        matrix.IsFullyTransparent.Should().BeTrue(
            "empty chunk with null infos should still be transparent");
    }

    private static ChunkData CreateSolidChunk()
    {
        ChunkData chunk = new(ChunkCoord.Zero);

        for (int y = 0; y < ChunkData.SizeY; y++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int x = 0; x < ChunkData.SizeX; x++)
                {
                    chunk.SetBlock(x, y, z, 1);
                }
            }
        }

        return chunk;
    }
}
