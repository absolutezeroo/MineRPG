using FluentAssertions;

using MineRPG.World.Meshing;
using MineRPG.World.Terrain;

namespace MineRPG.Tests.World;

public sealed class ClipmapGeneratorTests
{
    [Fact]
    public void Generate_DefaultConfig_ReturnsThreeRings()
    {
        ClipmapConfig config = new();

        MeshData[] rings = ClipmapGenerator.Generate(
            config, 0f, 0f,
            FlatHeight,
            GreenColor);

        rings.Should().HaveCount(ClipmapConfig.RingCount);
    }

    [Fact]
    public void Generate_FlatTerrain_ProducesNonEmptyFirstRing()
    {
        ClipmapConfig config = new();

        MeshData[] rings = ClipmapGenerator.Generate(
            config, 0f, 0f,
            FlatHeight,
            GreenColor);

        rings[0].IsEmpty.Should().BeFalse("first ring should have geometry");
    }

    [Fact]
    public void Generate_FlatTerrain_AllVerticesHaveConstantY()
    {
        ClipmapConfig config = new();
        float expectedHeight = 64f;

        MeshData[] rings = ClipmapGenerator.Generate(
            config, 0f, 0f,
            (_, _) => expectedHeight,
            GreenColor);

        if (!rings[0].IsEmpty)
        {
            for (int i = 0; i < rings[0].VertexCount; i++)
            {
                rings[0].Vertices[i * 3 + 1].Should().Be(expectedHeight,
                    "all Y values should equal the constant height");
            }
        }
    }

    [Fact]
    public void Generate_VerySmallConfig_MayReturnEmptyRings()
    {
        ClipmapConfig config = new()
        {
            Rings =
            [
                new ClipmapRing
                {
                    InnerRadiusChunks = 1,
                    OuterRadiusChunks = 2,
                    BlocksPerVertex = 64,
                },
            ],
        };

        MeshData[] rings = ClipmapGenerator.Generate(
            config, 0f, 0f,
            FlatHeight,
            GreenColor);

        // With very few vertices and large step size, may produce an empty mesh
        rings.Should().HaveCount(ClipmapConfig.RingCount);
    }

    [Fact]
    public void RingCount_IsThree()
    {
        ClipmapConfig.RingCount.Should().Be(3);
    }

    [Fact]
    public void DefaultConfig_HasCorrectRingRadii()
    {
        ClipmapConfig config = new();

        int cutoff = ClipmapConfig.DefaultVoxelRenderDistance;

        config.Rings.Should().HaveCount(3);
        config.Rings[0].InnerRadiusChunks.Should().Be(cutoff);
        config.Rings[0].OuterRadiusChunks.Should().Be(cutoff * 4);
        config.Rings[1].InnerRadiusChunks.Should().Be(cutoff * 4);
        config.Rings[2].OuterRadiusChunks.Should().Be(cutoff * 16);
    }

    [Fact]
    public void DefaultConfig_RebuildThreshold_Is16()
    {
        ClipmapConfig config = new();

        config.RebuildThresholdBlocks.Should().Be(16);
    }

    private static float FlatHeight(float worldX, float worldZ) => 64f;

    private static void GreenColor(float worldX, float worldZ, out float r, out float g, out float b)
    {
        r = 0.2f;
        g = 0.8f;
        b = 0.2f;
    }
}
