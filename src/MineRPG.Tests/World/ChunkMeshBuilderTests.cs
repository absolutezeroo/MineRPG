using FluentAssertions;
using MineRPG.Core.Logging;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Tests.World;

public sealed class ChunkMeshBuilderTests
{
    private readonly ChunkMeshBuilder _meshBuilder;
    private readonly BlockRegistry _blockRegistry;

    public ChunkMeshBuilderTests()
    {
        var logger = NullLogger.Instance;
        var loader = new Core.DataLoading.JsonDataLoader(logger, FindDataRoot());
        _blockRegistry = new BlockRegistry(loader, logger);
        _meshBuilder = new ChunkMeshBuilder(_blockRegistry);
    }

    [Fact]
    public void Build_EmptyChunk_ReturnsEmptyMesh()
    {
        // Arrange
        var chunk = new ChunkData(Core.Math.ChunkCoord.Zero);
        var neighbors = new ChunkData?[4];

        // Act
        var result = _meshBuilder.Build(chunk, neighbors);

        // Assert
        result.IsEmpty.Should().BeTrue();
        result.Opaque.IsEmpty.Should().BeTrue();
        result.Liquid.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Build_SingleBlock_ProducesFaces()
    {
        // Arrange
        var chunk = new ChunkData(Core.Math.ChunkCoord.Zero);
        chunk.SetBlock(8, 64, 8, 1); // Stone in the middle
        var neighbors = new ChunkData?[4];

        // Act
        var result = _meshBuilder.Build(chunk, neighbors);

        // Assert
        result.IsEmpty.Should().BeFalse();
        result.Opaque.VertexCount.Should().BeGreaterThan(0);
        result.Opaque.IndexCount.Should().BeGreaterThan(0);
        // A single exposed block should produce 6 faces = 24 vertices, 36 indices
        result.Opaque.VertexCount.Should().Be(24);
        result.Opaque.IndexCount.Should().Be(36);
    }

    [Fact]
    public void Build_TwoAdjacentBlocks_CullsSharedFace()
    {
        // Arrange
        var chunk = new ChunkData(Core.Math.ChunkCoord.Zero);
        chunk.SetBlock(8, 64, 8, 1); // Stone
        chunk.SetBlock(9, 64, 8, 1); // Stone adjacent on X axis
        var neighbors = new ChunkData?[4];

        // Act
        var result = _meshBuilder.Build(chunk, neighbors);

        // Assert
        // Two blocks sharing a face: 2*6 faces - 2 shared = 10 faces = 40 vertices, 60 indices
        // But greedy meshing may merge some faces, so vertex count may be lower
        result.Opaque.VertexCount.Should().BeLessThan(48, "shared faces should be culled");
        result.Opaque.IndexCount.Should().BeLessThan(72, "shared faces should be culled");
    }

    [Fact]
    public void Build_LiquidBlock_ProducesLiquidMesh()
    {
        // Arrange
        var chunk = new ChunkData(Core.Math.ChunkCoord.Zero);
        chunk.SetBlock(8, 64, 8, 6); // Water (ID 6, Transparent + Liquid)
        var neighbors = new ChunkData?[4];

        // Act
        var result = _meshBuilder.Build(chunk, neighbors);

        // Assert
        result.Liquid.IsEmpty.Should().BeFalse("water should produce liquid mesh");
        result.Liquid.VertexCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Build_IsThreadSafe()
    {
        // Arrange — fill chunk with a layer of stone
        var chunk = new ChunkData(Core.Math.ChunkCoord.Zero);
        for (var x = 0; x < ChunkData.SizeX; x++)
        for (var z = 0; z < ChunkData.SizeZ; z++)
        {
            chunk.SetBlock(x, 0, z, 1);
        }

        var neighbors = new ChunkData?[4];

        // Act — build meshes concurrently from multiple threads
        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            var result = _meshBuilder.Build(chunk, neighbors);
            return result.Opaque.VertexCount;
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert — all should produce the same result
        var vertexCounts = tasks.Select(t => t.Result).Distinct().ToList();
        vertexCounts.Should().ContainSingle("all threads should produce identical vertex counts");
    }

    private static string FindDataRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "Data");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data");
    }
}
