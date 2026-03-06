using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Tests.World;

public sealed class RegionMeshBatcherTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(3, 3, 0, 0)]
    [InlineData(4, 0, 1, 0)]
    [InlineData(7, 7, 1, 1)]
    [InlineData(-1, -1, -1, -1)]
    [InlineData(-4, 0, -1, 0)]
    [InlineData(-5, -5, -2, -2)]
    public void GetRegionCoord_ReturnsCorrectFloorDivision(
        int chunkX, int chunkZ, int expectedRegionX, int expectedRegionZ)
    {
        ChunkCoord chunkCoord = new(chunkX, chunkZ);

        ChunkCoord regionCoord = RegionMeshBatcher.GetRegionCoord(chunkCoord);

        regionCoord.X.Should().Be(expectedRegionX);
        regionCoord.Z.Should().Be(expectedRegionZ);
    }

    [Fact]
    public void RegionSize_IsFour()
    {
        RegionMeshBatcher.RegionSize.Should().Be(4);
    }

    [Fact]
    public void BatchSubChunkOpaque_EmptyList_ReturnsEmpty()
    {
        List<(ChunkCoord Coord, SubChunkMesh[] SubChunks)> meshes = [];
        ChunkCoord regionCoord = new(0, 0);

        MeshData result = RegionMeshBatcher.BatchSubChunkOpaque(meshes, regionCoord, 0);

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void BatchSubChunkOpaque_SingleChunk_OffsetsVertices()
    {
        ChunkCoord chunkCoord = new(1, 0);
        ChunkCoord regionCoord = new(0, 0);

        float[] vertices = [0f, 5f, 0f, 1f, 5f, 0f, 0f, 5f, 1f];
        float[] normals = [0f, 1f, 0f, 0f, 1f, 0f, 0f, 1f, 0f];
        float[] uvs = [0f, 0f, 1f, 0f, 0f, 1f];
        float[] uv2s = [0f, 0f, 0f, 0f, 0f, 0f];
        float[] colors = [1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f];
        int[] indices = [0, 1, 2];

        MeshData opaque = new(vertices, normals, uvs, uv2s, colors, indices);
        SubChunkMesh subMesh = new(opaque, MeshData.Empty);
        SubChunkMesh[] subChunks = new SubChunkMesh[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < subChunks.Length; i++)
        {
            subChunks[i] = SubChunkMesh.Empty;
        }

        subChunks[0] = subMesh;

        List<(ChunkCoord Coord, SubChunkMesh[] SubChunks)> meshes =
            [(chunkCoord, subChunks)];

        MeshData result = RegionMeshBatcher.BatchSubChunkOpaque(meshes, regionCoord, 0);

        result.IsEmpty.Should().BeFalse();
        // Chunk (1,0) relative to region (0,0): offset X = 1 * 16 = 16
        result.Vertices[0].Should().Be(16f, "vertex X should be offset by chunk position");
        result.VertexCount.Should().Be(3);
    }

    [Fact]
    public void BatchSubChunkOpaque_MultipleChunks_CombinesVerticesAndIndices()
    {
        ChunkCoord regionCoord = new(0, 0);

        float[] v1 = [0f, 0f, 0f];
        float[] n1 = [0f, 1f, 0f];
        float[] uv1 = [0f, 0f];
        float[] uv21 = [0f, 0f];
        float[] c1 = [1f, 1f, 1f, 1f];
        int[] i1 = [0];

        float[] v2 = [0f, 0f, 0f];
        float[] n2 = [0f, 1f, 0f];
        float[] uv2 = [0f, 0f];
        float[] uv22 = [0f, 0f];
        float[] c2 = [1f, 1f, 1f, 1f];
        int[] i2 = [0];

        SubChunkMesh[] sub1 = CreateSubChunksWithOpaque(new MeshData(v1, n1, uv1, uv21, c1, i1));
        SubChunkMesh[] sub2 = CreateSubChunksWithOpaque(new MeshData(v2, n2, uv2, uv22, c2, i2));

        List<(ChunkCoord Coord, SubChunkMesh[] SubChunks)> meshes =
        [
            (new ChunkCoord(0, 0), sub1),
            (new ChunkCoord(1, 0), sub2),
        ];

        MeshData result = RegionMeshBatcher.BatchSubChunkOpaque(meshes, regionCoord, 0);

        result.VertexCount.Should().Be(2);
        result.IndexCount.Should().Be(2);
        // Second index should be offset by 1 (first chunk's vertex count)
        result.Indices[1].Should().Be(1);
    }

    private static SubChunkMesh[] CreateSubChunksWithOpaque(MeshData opaque)
    {
        SubChunkMesh[] subChunks = new SubChunkMesh[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < subChunks.Length; i++)
        {
            subChunks[i] = SubChunkMesh.Empty;
        }

        subChunks[0] = new SubChunkMesh(opaque, MeshData.Empty);
        return subChunks;
    }
}
