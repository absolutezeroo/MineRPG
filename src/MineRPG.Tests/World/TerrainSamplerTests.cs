using FluentAssertions;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class TerrainSamplerTests
{
    private static TerrainSampler CreateSampler(int seed = 42)
    {
        var biomes = new List<BiomeDefinition>
        {
            new()
            {
                Id = "plains", BiomeType = BiomeType.Plains,
                MinTemperature = 0f, MaxTemperature = 1f,
                MinHumidity = 0f, MaxHumidity = 1f,
                SurfaceBlock = 3, SubSurfaceBlock = 2, StoneBlock = 1,
            },
        };
        var selector = new BiomeSelector(biomes, seed);
        return new TerrainSampler(selector, seed);
    }

    [Fact]
    public void SampleColumn_IsDeterministic()
    {
        // Arrange
        var sampler = CreateSampler();

        // Act
        var a = sampler.SampleColumn(100, 200);
        var b = sampler.SampleColumn(100, 200);

        // Assert
        a.SurfaceY.Should().Be(b.SurfaceY);
    }

    [Fact]
    public void SampleColumn_SurfaceY_IsWithinChunkBounds()
    {
        // Arrange
        var sampler = CreateSampler();

        // Act & Assert
        for (var x = -50; x <= 50; x += 5)
        for (var z = -50; z <= 50; z += 5)
        {
            var col = sampler.SampleColumn(x, z);
            col.SurfaceY.Should().BeInRange(1, ChunkData.SizeY - 2,
                $"surface at ({x},{z}) should be within valid range");
        }
    }

    [Fact]
    public void SampleColumn_ProducesMeaningfulVariation()
    {
        // Arrange
        var sampler = CreateSampler();
        var min = int.MaxValue;
        var max = int.MinValue;

        // Act — sample a wide area to capture terrain variation
        for (var x = 0; x < 500; x += 10)
        for (var z = 0; z < 500; z += 10)
        {
            var col = sampler.SampleColumn(x, z);
            if (col.SurfaceY < min) min = col.SurfaceY;
            if (col.SurfaceY > max) max = col.SurfaceY;
        }

        // Assert — should have at least 10 blocks of height variation
        (max - min).Should().BeGreaterThan(10,
            "terrain should have meaningful height variation");
    }

    [Fact]
    public void SampleColumn_PrimaryBiome_IsNotNull()
    {
        // Arrange
        var sampler = CreateSampler();

        // Act
        var col = sampler.SampleColumn(0, 0);

        // Assert
        col.PrimaryBiome.Should().NotBeNull();
        col.SecondaryBiome.Should().NotBeNull();
    }

    [Fact]
    public void SampleCaveDensity_NearSurface_ReturnsSolid()
    {
        // Arrange
        var sampler = CreateSampler();

        // Act & Assert — depth suppression should prevent caves near surface
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            var col = sampler.SampleColumn(x, z);
            // Right at the surface minus 2: inside subsurface, should be solid
            var density = sampler.SampleCaveDensity(x, col.SurfaceY - 2, z,
                col.SurfaceY, col.Continentalness);
            density.Should().BeGreaterThanOrEqualTo(0f,
                $"voxel near surface at ({x},{z}) should not be carved");
        }
    }
}
