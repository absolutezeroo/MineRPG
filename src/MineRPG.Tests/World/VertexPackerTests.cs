using FluentAssertions;

using MineRPG.World.Meshing;

namespace MineRPG.Tests.World;

public sealed class VertexPackerTests
{
    [Fact]
    public void Pack_EmptyMesh_ReturnsEmptyArray()
    {
        PackedVertex[] result = VertexPacker.Pack(MeshData.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Pack_SingleVertex_ReturnsOnePackedVertex()
    {
        MeshData meshData = CreateSingleVertexMesh(1f, 2f, 3f, 0f, 1f, 0f, 0.5f, 0.5f, 0.25f, 0.75f, 1f, 1f, 1f, 0.8f);

        PackedVertex[] packed = VertexPacker.Pack(meshData);

        packed.Should().HaveCount(1);
    }

    [Fact]
    public void Pack_PreservesPositionApproximately()
    {
        MeshData meshData = CreateSingleVertexMesh(5f, 10f, 15f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);

        PackedVertex[] packed = VertexPacker.Pack(meshData);

        packed[0].UnpackPositionX().Should().BeApproximately(5f, 0.01f);
        packed[0].UnpackPositionY().Should().BeApproximately(10f, 0.01f);
        packed[0].UnpackPositionZ().Should().BeApproximately(15f, 0.01f);
    }

    [Fact]
    public void Pack_QuantizesNormalToAxisAligned()
    {
        // Normal pointing mostly in +Y direction
        MeshData meshData = CreateSingleVertexMesh(0f, 0f, 0f, 0.1f, 0.9f, 0.1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);

        PackedVertex[] packed = VertexPacker.Pack(meshData);

        // +Y = index 2
        packed[0].NormalIndex.Should().Be(2);
    }

    [Fact]
    public void Pack_NegativeZ_QuantizesToIndex5()
    {
        MeshData meshData = CreateSingleVertexMesh(0f, 0f, 0f, 0f, 0f, -1f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);

        PackedVertex[] packed = VertexPacker.Pack(meshData);

        // -Z = index 5
        packed[0].NormalIndex.Should().Be(5);
    }

    [Fact]
    public void Pack_PreservesAoApproximately()
    {
        MeshData meshData = CreateSingleVertexMesh(0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 0.75f);

        PackedVertex[] packed = VertexPacker.Pack(meshData);

        packed[0].UnpackAo().Should().BeApproximately(0.75f, 0.01f);
    }

    [Fact]
    public void Unpack_RoundTrip_PreservesDataApproximately()
    {
        MeshData original = CreateSingleVertexMesh(3f, 7f, 11f, 0f, 1f, 0f, 0.5f, 0.5f, 0.25f, 0.75f, 1f, 1f, 1f, 0.5f);

        PackedVertex[] packed = VertexPacker.Pack(original);
        MeshData unpacked = VertexPacker.Unpack(packed, original.Indices);

        unpacked.Vertices[0].Should().BeApproximately(3f, 0.01f);
        unpacked.Vertices[1].Should().BeApproximately(7f, 0.01f);
        unpacked.Vertices[2].Should().BeApproximately(11f, 0.01f);
    }

    [Fact]
    public void Unpack_EmptyArray_ReturnsEmptyMesh()
    {
        MeshData result = VertexPacker.Unpack([], []);

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void PackedVertex_SizeInBytes_Is20() => PackedVertex.SizeInBytes.Should().Be(20);

    private static MeshData CreateSingleVertexMesh(
        float px, float py, float pz,
        float nx, float ny, float nz,
        float u, float v,
        float au, float av,
        float r, float g, float b, float a)
    {
        float[] vertices = [px, py, pz];
        float[] normals = [nx, ny, nz];
        float[] uvs = [u, v];
        float[] uv2s = [au, av];
        float[] colors = [r, g, b, a];
        int[] indices = [0];

        return new MeshData(vertices, normals, uvs, uv2s, colors, indices);
    }
}
