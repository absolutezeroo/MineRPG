using System;
using System.Collections.Generic;

using FluentAssertions;

using MineRPG.World.Biomes.Climate;
using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class BiomeCoverageTests
{
    private const int WorldSeed = 42;
    private const int SampleCount = 10000;
    private const int RandomSeed = 12345;
    private const float FullRangeMin = -1f;
    private const float FullRangeMax = 1f;
    private const float DepthRangeMin = 0f;
    private const float DepthRangeMax = 1f;

    private static List<BiomeDefinition> CreateBiomeSet()
    {
        List<BiomeDefinition> biomes = new List<BiomeDefinition>
        {
            new()
            {
                Id = "ocean",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(-1.0f, -0.4f),
                    Erosion = ParameterRange.Full,
                    PeaksAndValleys = ParameterRange.Full,
                    Temperature = ParameterRange.Full,
                    Humidity = ParameterRange.Full,
                    Depth = ParameterRange.FullDepth,
                },
            },
            new()
            {
                Id = "plains",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(-0.11f, 0.55f),
                    Erosion = new ParameterRange(0.1f, 1.0f),
                    PeaksAndValleys = ParameterRange.Full,
                    Temperature = new ParameterRange(-0.15f, 0.55f),
                    Humidity = new ParameterRange(-1.0f, 0.1f),
                    Depth = new ParameterRange(0f, 0.2f),
                },
            },
            new()
            {
                Id = "desert",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(0.0f, 1.0f),
                    Erosion = new ParameterRange(0.2f, 1.0f),
                    PeaksAndValleys = new ParameterRange(-0.5f, 0.5f),
                    Temperature = new ParameterRange(0.5f, 1.0f),
                    Humidity = new ParameterRange(-1.0f, -0.3f),
                    Depth = new ParameterRange(0f, 0.3f),
                },
            },
            new()
            {
                Id = "snowy_tundra",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(-0.2f, 0.8f),
                    Erosion = new ParameterRange(-0.5f, 1.0f),
                    PeaksAndValleys = new ParameterRange(-1.0f, 0.3f),
                    Temperature = new ParameterRange(-1.0f, -0.5f),
                    Humidity = new ParameterRange(-1.0f, 0.0f),
                    Depth = new ParameterRange(0f, 0.2f),
                },
            },
            new()
            {
                Id = "jungle",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(-0.1f, 0.7f),
                    Erosion = new ParameterRange(-0.2f, 0.8f),
                    PeaksAndValleys = new ParameterRange(-0.5f, 0.5f),
                    Temperature = new ParameterRange(0.3f, 1.0f),
                    Humidity = new ParameterRange(0.3f, 1.0f),
                    Depth = new ParameterRange(0f, 0.3f),
                },
            },
            new()
            {
                Id = "mountains",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(0.2f, 1.0f),
                    Erosion = new ParameterRange(-1.0f, -0.3f),
                    PeaksAndValleys = new ParameterRange(0.3f, 1.0f),
                    Temperature = new ParameterRange(-0.7f, 0.3f),
                    Humidity = new ParameterRange(-0.5f, 0.5f),
                    Depth = new ParameterRange(0f, 0.5f),
                },
            },
            new()
            {
                Id = "swamp",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(-0.3f, 0.3f),
                    Erosion = new ParameterRange(0.3f, 1.0f),
                    PeaksAndValleys = new ParameterRange(-1.0f, -0.3f),
                    Temperature = new ParameterRange(0.0f, 0.6f),
                    Humidity = new ParameterRange(0.3f, 1.0f),
                    Depth = new ParameterRange(0f, 0.2f),
                },
            },
            new()
            {
                Id = "taiga",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(0.0f, 0.8f),
                    Erosion = new ParameterRange(-0.3f, 0.5f),
                    PeaksAndValleys = new ParameterRange(-0.3f, 0.5f),
                    Temperature = new ParameterRange(-0.7f, -0.1f),
                    Humidity = new ParameterRange(0.0f, 0.7f),
                    Depth = new ParameterRange(0f, 0.3f),
                },
            },
            new()
            {
                Id = "deep_caves",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = ParameterRange.Full,
                    Erosion = ParameterRange.Full,
                    PeaksAndValleys = ParameterRange.Full,
                    Temperature = ParameterRange.Full,
                    Humidity = ParameterRange.Full,
                    Depth = new ParameterRange(0.5f, 1.0f),
                },
            },
            new()
            {
                Id = "forest",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = new ParameterRange(-0.1f, 0.6f),
                    Erosion = new ParameterRange(-0.2f, 0.7f),
                    PeaksAndValleys = new ParameterRange(-0.5f, 0.5f),
                    Temperature = new ParameterRange(-0.3f, 0.4f),
                    Humidity = new ParameterRange(0.0f, 0.8f),
                    Depth = new ParameterRange(0f, 0.2f),
                },
            },
        };

        return biomes;
    }

    [Fact]
    public void Select_RandomPointsIn6DSpace_AlwaysReturnsBiome()
    {
        // Arrange
        List<BiomeDefinition> biomes = CreateBiomeSet();
        BiomeSelector selector = new BiomeSelector(biomes, WorldSeed);
        Random random = new Random(RandomSeed);

        // Act & Assert — every random point in the 6D space must get a biome
        for (int i = 0; i < SampleCount; i++)
        {
            float continentalness = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float erosion = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float peaksAndValleys = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float temperature = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float humidity = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float depth = NextFloatInRange(random, DepthRangeMin, DepthRangeMax);

            ClimateParameters parameters = new ClimateParameters(
                continentalness, erosion, peaksAndValleys, temperature, humidity, depth);

            BiomeDefinition result = selector.Select(in parameters);

            result.Should().NotBeNull(
                $"sample {i} at C={continentalness:F2} E={erosion:F2} "
                + $"PV={peaksAndValleys:F2} T={temperature:F2} "
                + $"H={humidity:F2} D={depth:F2} must be assigned a biome");
        }
    }

    [Fact]
    public void Select_ExtremeCorners_AlwaysReturnsBiome()
    {
        // Arrange — test all 64 corners of the 6D hypercube
        List<BiomeDefinition> biomes = CreateBiomeSet();
        BiomeSelector selector = new BiomeSelector(biomes, WorldSeed);
        float[] extremeValues = [FullRangeMin, FullRangeMax];
        float[] depthValues = [DepthRangeMin, DepthRangeMax];

        // Act & Assert — each corner of the 6D hypercube must resolve to a biome
        foreach (float continentalness in extremeValues)
        {
            foreach (float erosion in extremeValues)
            {
                foreach (float peaksAndValleys in extremeValues)
                {
                    foreach (float temperature in extremeValues)
                    {
                        foreach (float humidity in extremeValues)
                        {
                            foreach (float depth in depthValues)
                            {
                                ClimateParameters parameters = new ClimateParameters(
                                    continentalness, erosion, peaksAndValleys,
                                    temperature, humidity, depth);

                                BiomeDefinition result = selector.Select(in parameters);

                                result.Should().NotBeNull(
                                    $"corner C={continentalness} E={erosion} "
                                    + $"PV={peaksAndValleys} T={temperature} "
                                    + $"H={humidity} D={depth} must be assigned a biome");
                            }
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void Select_RandomPoints_ProducesMultipleBiomes()
    {
        // Arrange — the biome set should produce variety, not just one biome
        List<BiomeDefinition> biomes = CreateBiomeSet();
        BiomeSelector selector = new BiomeSelector(biomes, WorldSeed);
        Random random = new Random(RandomSeed);
        HashSet<string> selectedBiomeIds = new HashSet<string>();
        int minimumExpectedBiomes = 3;

        // Act
        for (int i = 0; i < SampleCount; i++)
        {
            float continentalness = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float erosion = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float peaksAndValleys = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float temperature = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float humidity = NextFloatInRange(random, FullRangeMin, FullRangeMax);
            float depth = NextFloatInRange(random, DepthRangeMin, DepthRangeMax);

            ClimateParameters parameters = new ClimateParameters(
                continentalness, erosion, peaksAndValleys, temperature, humidity, depth);

            BiomeDefinition result = selector.Select(in parameters);
            selectedBiomeIds.Add(result.Id);
        }

        // Assert — uniform random sampling should hit multiple biomes
        selectedBiomeIds.Count.Should().BeGreaterThanOrEqualTo(minimumExpectedBiomes,
            "random sampling across the 6D space should produce biome variety");
    }

    private static float NextFloatInRange(Random random, float min, float max)
    {
        float range = max - min;
        return (float)(random.NextDouble() * range) + min;
    }
}
