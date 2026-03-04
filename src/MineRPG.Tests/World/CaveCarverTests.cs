using System.Collections.Generic;

using FluentAssertions;

using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class CaveCarverTests
{
    private static CaveCarver CreateCarver(int seed = 42)
    {
        List<BiomeDefinition> biomes = new List<BiomeDefinition>
        {
            new()
            {
                Id = "plains", BiomeType = BiomeType.Plains,
                MinTemperature = 0f, MaxTemperature = 1f,
                MinHumidity = 0f, MaxHumidity = 1f,
                SurfaceBlock = 3, SubSurfaceBlock = 2, StoneBlock = 1,
            },
        };
        BiomeSelector selector = new BiomeSelector(biomes, seed);
        TerrainSampler sampler = new TerrainSampler(selector, seed);
        return new CaveCarver(sampler);
    }

    [Fact]
    public void ShouldCarve_AtBedrock_ReturnsFalse()
    {
        // Arrange
        CaveCarver carver = CreateCarver();

        // Act & Assert
        carver.ShouldCarve(0, 0, 0, surfaceY: 64, continentalness: 0.5f)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldCarve_AboveSubsurfaceLayer_ReturnsFalse()
    {
        // Arrange
        CaveCarver carver = CreateCarver();

        // Act — surfaceY - 3 is within the protected subsurface zone
        bool result = carver.ShouldCarve(0, 61, 0, surfaceY: 64, continentalness: 0.5f);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldCarve_AtSurface_ReturnsFalse()
    {
        // Arrange
        CaveCarver carver = CreateCarver();

        // Act
        bool result = carver.ShouldCarve(0, 64, 0, surfaceY: 64, continentalness: 0.5f);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldCarve_DeepUnderground_ProducesSomeCaves()
    {
        // Arrange
        CaveCarver carver = CreateCarver();
        int carvedCount = 0;

        // Act — sample a volume deep underground
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                for (int y = 5; y < 30; y++)
                {
                    if (carver.ShouldCarve(x, y, z, surfaceY: 64, continentalness: 0.5f))
                    {
                        carvedCount++;
                    }
                }
            }
        }

        // Assert — should have some caves but not too many
        carvedCount.Should().BeGreaterThan(0, "there should be some caves deep underground");
        carvedCount.Should().BeLessThan(32 * 32 * 25, "caves should not carve everything");
    }
}
