using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
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
        ILogger logger = NullLogger.Instance;
        JsonDataLoader loader = new JsonDataLoader(logger, FindDataRoot());
        _blockRegistry = new BlockRegistry(loader, logger);
        _meshBuilder = new ChunkMeshBuilder(_blockRegistry);
    }

    [Fact]
    public void Build_EmptyChunk_ReturnsEmptyMesh()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        ChunkData?[] neighbors = new ChunkData?[4];

        // Act
        ChunkMeshResult result = _meshBuilder.Build(chunk, neighbors, CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeTrue();

        for (int i = 0; i < result.SubChunks.Length; i++)
        {
            result.SubChunks[i].Opaque.IsEmpty.Should().BeTrue();
            result.SubChunks[i].Liquid.IsEmpty.Should().BeTrue();
        }
    }

    [Fact]
    public void Build_SingleBlock_ProducesFaces()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(8, 64, 8, 1); // Stone in the middle
        ChunkData?[] neighbors = new ChunkData?[4];

        // Act
        ChunkMeshResult result = _meshBuilder.Build(chunk, neighbors, CancellationToken.None);

        // Assert
        result.IsEmpty.Should().BeFalse();

        // Block at Y=64 is in sub-chunk 4 (64/16=4)
        SubChunkMesh subChunk = result.SubChunks[4];
        subChunk.Opaque.VertexCount.Should().BeGreaterThan(0);
        subChunk.Opaque.IndexCount.Should().BeGreaterThan(0);
        // A single exposed block should produce 6 faces = 24 vertices, 36 indices
        subChunk.Opaque.VertexCount.Should().Be(24);
        subChunk.Opaque.IndexCount.Should().Be(36);
    }

    [Fact]
    public void Build_TwoAdjacentBlocks_CullsSharedFace()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(8, 64, 8, 1); // Stone
        chunk.SetBlock(9, 64, 8, 1); // Stone adjacent on X axis
        ChunkData?[] neighbors = new ChunkData?[4];

        // Act
        ChunkMeshResult result = _meshBuilder.Build(chunk, neighbors, CancellationToken.None);

        // Assert - both blocks are in sub-chunk 4
        SubChunkMesh subChunk = result.SubChunks[4];
        // Two blocks sharing a face: 2*6 faces - 2 shared = 10 faces = 40 vertices, 60 indices
        // But greedy meshing may merge some faces, so vertex count may be lower
        subChunk.Opaque.VertexCount.Should().BeLessThan(48, "shared faces should be culled");
        subChunk.Opaque.IndexCount.Should().BeLessThan(72, "shared faces should be culled");
    }

    [Fact]
    public void Build_LiquidBlock_ProducesLiquidMesh()
    {
        // Arrange
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(8, 64, 8, 6); // Water (ID 6, Transparent + Liquid)
        ChunkData?[] neighbors = new ChunkData?[4];

        // Act
        ChunkMeshResult result = _meshBuilder.Build(chunk, neighbors, CancellationToken.None);

        // Assert - water at Y=64 is in sub-chunk 4
        SubChunkMesh subChunk = result.SubChunks[4];
        subChunk.Liquid.IsEmpty.Should().BeFalse("water should produce liquid mesh");
        subChunk.Liquid.VertexCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Build_IsThreadSafe()
    {
        // Arrange - fill chunk with a layer of stone at Y=0
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                chunk.SetBlock(x, 0, z, 1);
            }
        }

        ChunkData?[] neighbors = new ChunkData?[4];

        // Act - build meshes concurrently from multiple threads
        Task<int>[] tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            ChunkMeshResult result = _meshBuilder.Build(chunk, neighbors, CancellationToken.None);
            return TotalOpaqueVertexCount(result);
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert - all should produce the same result
        System.Collections.Generic.List<int> vertexCounts = tasks.Select(t => t.Result).Distinct().ToList();
        vertexCounts.Should().ContainSingle("all threads should produce identical vertex counts");
    }

    [Fact]
    public void Build_BlocksAtSubChunkBoundary_SplitsCorrectly()
    {
        // Arrange - blocks straddling the Y=16 sub-chunk boundary
        ChunkData chunk = new ChunkData(ChunkCoord.Zero);
        chunk.SetBlock(8, 15, 8, 1); // Stone in sub-chunk 0
        chunk.SetBlock(8, 16, 8, 1); // Stone in sub-chunk 1
        ChunkData?[] neighbors = new ChunkData?[4];

        // Act
        ChunkMeshResult result = _meshBuilder.Build(chunk, neighbors, CancellationToken.None);

        // Assert - each sub-chunk should have geometry
        result.SubChunks[0].Opaque.IsEmpty.Should().BeFalse("sub-chunk 0 has a block at Y=15");
        result.SubChunks[1].Opaque.IsEmpty.Should().BeFalse("sub-chunk 1 has a block at Y=16");

        // Other sub-chunks should be empty
        for (int i = 2; i < SubChunkConstants.SubChunkCount; i++)
        {
            result.SubChunks[i].Opaque.IsEmpty.Should().BeTrue($"sub-chunk {i} should be empty");
        }
    }

    private static int TotalOpaqueVertexCount(ChunkMeshResult result)
    {
        int total = 0;

        for (int i = 0; i < result.SubChunks.Length; i++)
        {
            total += result.SubChunks[i].Opaque.VertexCount;
        }

        return total;
    }

    private static string FindDataRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data");
    }
}
