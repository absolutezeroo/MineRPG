using System;
using System.Collections.Generic;

using FluentAssertions;

using MineRPG.World.Biomes.Climate;
using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class BiomeSelectorTests
{
    private const int TestSeed = 42;
    private const float FloatPrecision = 0.001f;
    private const float BlendWeightMin = 0f;
    private const float BlendWeightMax = 1f;

    [Fact]
    public void Select_WithClimateParameters_ReturnsBestMatch()
    {
        // Arrange
        List<BiomeDefinition> biomes = CreateTestBiomes();
        BiomeSelector selector = new BiomeSelector(biomes, TestSeed);

        // Climate parameters clearly inside desert range (high temp, low humidity)
        ClimateParameters desertClimate = new ClimateParameters(
            continentalness: 0.3f,
            erosion: 0.7f,
            peaksAndValleys: 0.0f,
            temperature: 0.8f,
            humidity: -0.7f,
            depth: 0.1f);

        // Act
        BiomeDefinition result = selector.Select(in desertClimate);

        // Assert
        result.Id.Should().Be("desert");
    }

    [Fact]
    public void Select_WithParameterExactlyInRange_SelectsThatBiome()
    {
        // Arrange
        List<BiomeDefinition> biomes = CreateTestBiomes();
        BiomeSelector selector = new BiomeSelector(biomes, TestSeed);

        // Parameters at the center of plains ranges -- distance should be 0
        ClimateParameters plainsCenter = new ClimateParameters(
            continentalness: 0.25f,
            erosion: 0.65f,
            peaksAndValleys: 0.0f,
            temperature: 0.15f,
            humidity: -0.15f,
            depth: 0.1f);

        // Act
        BiomeDefinition result = selector.Select(in plainsCenter);

        // Assert
        result.Id.Should().Be("plains");
    }

    [Fact]
    public void SelectWeighted_ReturnsPrimaryAndSecondary()
    {
        // Arrange
        List<BiomeDefinition> biomes = CreateTestBiomes();
        BiomeSelector selector = new BiomeSelector(biomes, TestSeed);

        // Parameters near the boundary between plains and desert
        ClimateParameters boundaryClimate = new ClimateParameters(
            continentalness: 0.3f,
            erosion: 0.65f,
            peaksAndValleys: 0.0f,
            temperature: 0.5f,
            humidity: -0.3f,
            depth: 0.1f);

        // Act
        (BiomeDefinition primary, BiomeDefinition secondary, float blendWeight) =
            selector.SelectWeighted(in boundaryClimate);

        // Assert
        primary.Should().NotBeNull();
        secondary.Should().NotBeNull();
        primary.Id.Should().NotBe(secondary.Id,
            "primary and secondary should be different biomes near a boundary");
        blendWeight.Should().BeInRange(BlendWeightMin, BlendWeightMax);
    }

    [Fact]
    public void SelectWeighted_BlendWeight_IsZeroWhenFarFromBoundary()
    {
        // Arrange
        List<BiomeDefinition> biomes = CreateTestBiomes();
        BiomeSelector selector = new BiomeSelector(biomes, TestSeed);

        // Parameters deep inside tundra range, far from any other biome
        ClimateParameters deepTundraClimate = new ClimateParameters(
            continentalness: 0.3f,
            erosion: 0.5f,
            peaksAndValleys: 0.0f,
            temperature: -0.8f,
            humidity: -0.8f,
            depth: 0.05f);

        // Act
        (BiomeDefinition primary, BiomeDefinition _, float blendWeight) =
            selector.SelectWeighted(in deepTundraClimate);

        // Assert
        primary.Id.Should().Be("tundra");
        blendWeight.Should().BeApproximately(0f, FloatPrecision,
            "blend weight should be zero when clearly inside one biome");
    }

    [Fact]
    public void Select_WithSingleBiome_AlwaysReturnsThatBiome()
    {
        // Arrange
        List<BiomeDefinition> biomes = new List<BiomeDefinition>
        {
            new BiomeDefinition
            {
                Id = "only_biome",
                HasClimateTarget = true,
                ClimateTarget = new BiomeClimateTarget
                {
                    Continentalness = ParameterRange.Full,
                    Erosion = ParameterRange.Full,
                    PeaksAndValleys = ParameterRange.Full,
                    Temperature = ParameterRange.Full,
                    Humidity = ParameterRange.Full,
                    Depth = ParameterRange.FullDepth,
                },
            },
        };
        BiomeSelector selector = new BiomeSelector(biomes, TestSeed);

        ClimateParameters anyClimate = new ClimateParameters(
            continentalness: 0.5f,
            erosion: -0.3f,
            peaksAndValleys: 0.8f,
            temperature: -0.9f,
            humidity: 0.7f,
            depth: 0.5f);

        // Act
        BiomeDefinition result = selector.Select(in anyClimate);

        // Assert
        result.Id.Should().Be("only_biome");
    }

    [Fact]
    public void Constructor_WithEmptyList_ThrowsInvalidOperationException()
    {
        // Arrange
        List<BiomeDefinition> emptyBiomes = new List<BiomeDefinition>();

        // Act
        Action act = () => new BiomeSelector(emptyBiomes, TestSeed);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one*");
    }

    [Fact]
    public void Select_LegacyMode_WhenNoClimateTargets()
    {
        // Arrange -- biomes without ClimateTarget (all zeros = HasClimateTarget is false)
        List<BiomeDefinition> legacyBiomes = new List<BiomeDefinition>
        {
            new BiomeDefinition
            {
                Id = "legacy_hot",
                MinTemperature = 0.6f,
                MaxTemperature = 1.0f,
                MinHumidity = 0.0f,
                MaxHumidity = 0.5f,
            },
            new BiomeDefinition
            {
                Id = "legacy_cold",
                MinTemperature = 0.0f,
                MaxTemperature = 0.4f,
                MinHumidity = 0.0f,
                MaxHumidity = 0.5f,
            },
            new BiomeDefinition
            {
                Id = "legacy_temperate",
                MinTemperature = 0.3f,
                MaxTemperature = 0.7f,
                MinHumidity = 0.3f,
                MaxHumidity = 0.8f,
            },
        };
        BiomeSelector selector = new BiomeSelector(legacyBiomes, TestSeed);

        // Act -- use the legacy 2D overload
        BiomeDefinition result = selector.Select(worldX: 0, worldZ: 0);

        // Assert -- should return one of the legacy biomes without crashing
        result.Should().NotBeNull();
        result.Id.Should().BeOneOf("legacy_hot", "legacy_cold", "legacy_temperate");
    }

    private static List<BiomeDefinition> CreateTestBiomes()
    {
        BiomeDefinition plains = new BiomeDefinition
        {
            Id = "plains",
            HasClimateTarget = true,
            ClimateTarget = new BiomeClimateTarget
            {
                Continentalness = new ParameterRange(0.0f, 0.5f),
                Erosion = new ParameterRange(0.3f, 1.0f),
                PeaksAndValleys = ParameterRange.Full,
                Temperature = new ParameterRange(-0.2f, 0.5f),
                Humidity = new ParameterRange(-0.5f, 0.2f),
                Depth = new ParameterRange(0f, 0.2f),
            },
        };

        BiomeDefinition desert = new BiomeDefinition
        {
            Id = "desert",
            HasClimateTarget = true,
            ClimateTarget = new BiomeClimateTarget
            {
                Continentalness = new ParameterRange(0.1f, 0.8f),
                Erosion = new ParameterRange(0.2f, 1.0f),
                PeaksAndValleys = ParameterRange.Full,
                Temperature = new ParameterRange(0.5f, 1.0f),
                Humidity = new ParameterRange(-1.0f, -0.3f),
                Depth = new ParameterRange(0f, 0.2f),
            },
        };

        BiomeDefinition tundra = new BiomeDefinition
        {
            Id = "tundra",
            HasClimateTarget = true,
            ClimateTarget = new BiomeClimateTarget
            {
                Continentalness = new ParameterRange(0.0f, 0.7f),
                Erosion = new ParameterRange(0.1f, 0.8f),
                PeaksAndValleys = ParameterRange.Full,
                Temperature = new ParameterRange(-1.0f, -0.3f),
                Humidity = new ParameterRange(-1.0f, -0.2f),
                Depth = new ParameterRange(0f, 0.15f),
            },
        };

        return new List<BiomeDefinition> { plains, desert, tundra };
    }
}
