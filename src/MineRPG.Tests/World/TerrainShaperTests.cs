using System;

using FluentAssertions;

using MineRPG.World.Biomes.Climate;
using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class TerrainShaperTests
{
    private const int SeaLevel = 62;
    private const int MinClampHeight = 1;
    private const int MaxClampHeight = 254;
    private const float OceanContinentalness = -0.5f;
    private const float InlandContinentalness = 0.5f;
    private const float HighPeaksAndValleys = 1.0f;
    private const float LowErosion = -1.0f;
    private const float HighErosion = 1.0f;
    private const float NeutralValue = 0f;
    private const float DefaultTemperature = 0f;
    private const float DefaultHumidity = 0f;
    private const float DefaultDepth = 0f;
    private const float ZeroBlendWeight = 0f;
    private const float FullBlendWeight = 1.0f;
    private const float PrimaryBiomeOffset = 10f;
    private const float SecondaryBiomeOffset = -5f;
    private const float HeightPrecision = 0.5f;

    private readonly TerrainShaper _defaultShaper = TerrainShaper.CreateDefault();

    [Fact]
    public void GetHeight_AtOceanContinentalness_ReturnsBelowSeaLevel()
    {
        // Arrange
        ClimateParameters parameters = new ClimateParameters(
            OceanContinentalness,
            NeutralValue,
            NeutralValue,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        // Act
        float height = _defaultShaper.GetHeight(in parameters);

        // Assert
        height.Should().BeLessThan(SeaLevel);
    }

    [Fact]
    public void GetHeight_AtInlandContinentalness_ReturnsAboveSeaLevel()
    {
        // Arrange
        ClimateParameters parameters = new ClimateParameters(
            InlandContinentalness,
            NeutralValue,
            NeutralValue,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        // Act
        float height = _defaultShaper.GetHeight(in parameters);

        // Assert
        height.Should().BeGreaterThan(SeaLevel);
    }

    [Fact]
    public void GetHeight_WithHighPeaksAndLowErosion_ProducesHighTerrain()
    {
        // Arrange — PV=1.0 yields large positive offset; E=-1.0 yields erosionFactor=1.5
        ClimateParameters parameters = new ClimateParameters(
            InlandContinentalness,
            LowErosion,
            HighPeaksAndValleys,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        ClimateParameters flatParameters = new ClimateParameters(
            InlandContinentalness,
            NeutralValue,
            NeutralValue,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        // Act
        float peakyHeight = _defaultShaper.GetHeight(in parameters);
        float flatHeight = _defaultShaper.GetHeight(in flatParameters);

        // Assert — peaks with low erosion should be significantly higher than neutral terrain
        peakyHeight.Should().BeGreaterThan(flatHeight);
    }

    [Fact]
    public void GetHeight_WithHighErosion_FlattensTerrain()
    {
        // Arrange — E=1.0 yields erosionFactor=0.2, so PV offset barely matters
        ClimateParameters highErosionParameters = new ClimateParameters(
            InlandContinentalness,
            HighErosion,
            HighPeaksAndValleys,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        ClimateParameters lowErosionParameters = new ClimateParameters(
            InlandContinentalness,
            LowErosion,
            HighPeaksAndValleys,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        // Act
        float highErosionHeight = _defaultShaper.GetHeight(in highErosionParameters);
        float lowErosionHeight = _defaultShaper.GetHeight(in lowErosionParameters);

        // Assert — high erosion should produce lower terrain than low erosion at the same PV
        highErosionHeight.Should().BeLessThan(lowErosionHeight);
    }

    [Theory]
    [InlineData(-1.0f, -1.0f, -1.0f)]
    [InlineData(1.0f, -1.0f, 1.0f)]
    [InlineData(1.0f, 1.0f, 1.0f)]
    [InlineData(-1.0f, 1.0f, -1.0f)]
    [InlineData(0f, 0f, 0f)]
    public void GetHeight_ResultIsClamped(
        float continentalness,
        float erosion,
        float peaksAndValleys)
    {
        // Arrange
        ClimateParameters parameters = new ClimateParameters(
            continentalness,
            erosion,
            peaksAndValleys,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        // Act
        float height = _defaultShaper.GetHeight(in parameters);

        // Assert
        height.Should().BeInRange(MinClampHeight, MaxClampHeight);
    }

    [Fact]
    public void GetBlendedHeight_WithZeroBlendWeight_UsesPrimaryBiomeOffset()
    {
        // Arrange
        ClimateParameters parameters = new ClimateParameters(
            NeutralValue,
            NeutralValue,
            NeutralValue,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        BiomeDefinition primaryBiome = new BiomeDefinition
        {
            HeightSplinePoints =
            [
                new SplinePoint(-1f, PrimaryBiomeOffset),
                new SplinePoint(0f, PrimaryBiomeOffset),
                new SplinePoint(1f, PrimaryBiomeOffset),
            ],
        };

        BiomeDefinition secondaryBiome = new BiomeDefinition
        {
            HeightSplinePoints =
            [
                new SplinePoint(-1f, SecondaryBiomeOffset),
                new SplinePoint(0f, SecondaryBiomeOffset),
                new SplinePoint(1f, SecondaryBiomeOffset),
            ],
        };

        float baseHeight = _defaultShaper.GetHeight(in parameters);
        int expectedHeight = (int)MathF.Round(baseHeight + PrimaryBiomeOffset);

        // Act
        int blendedHeight = _defaultShaper.GetBlendedHeight(
            in parameters, primaryBiome, secondaryBiome, ZeroBlendWeight);

        // Assert
        blendedHeight.Should().Be(expectedHeight);
    }

    [Fact]
    public void GetBlendedHeight_WithFullBlendWeight_UsesSecondaryBiomeOffset()
    {
        // Arrange
        ClimateParameters parameters = new ClimateParameters(
            NeutralValue,
            NeutralValue,
            NeutralValue,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        BiomeDefinition primaryBiome = new BiomeDefinition
        {
            HeightSplinePoints =
            [
                new SplinePoint(-1f, PrimaryBiomeOffset),
                new SplinePoint(0f, PrimaryBiomeOffset),
                new SplinePoint(1f, PrimaryBiomeOffset),
            ],
        };

        BiomeDefinition secondaryBiome = new BiomeDefinition
        {
            HeightSplinePoints =
            [
                new SplinePoint(-1f, SecondaryBiomeOffset),
                new SplinePoint(0f, SecondaryBiomeOffset),
                new SplinePoint(1f, SecondaryBiomeOffset),
            ],
        };

        float baseHeight = _defaultShaper.GetHeight(in parameters);
        int expectedHeight = (int)MathF.Round(baseHeight + SecondaryBiomeOffset);

        // Act
        int blendedHeight = _defaultShaper.GetBlendedHeight(
            in parameters, primaryBiome, secondaryBiome, FullBlendWeight);

        // Assert
        blendedHeight.Should().Be(expectedHeight);
    }

    [Fact]
    public void CreateDefault_ReturnsValidShaper()
    {
        // Arrange & Act
        TerrainShaper shaper = TerrainShaper.CreateDefault();

        // Assert — should not throw and should produce a reasonable height near sea level
        ClimateParameters neutralParameters = new ClimateParameters(
            NeutralValue,
            NeutralValue,
            NeutralValue,
            DefaultTemperature,
            DefaultHumidity,
            DefaultDepth);

        float height = shaper.GetHeight(in neutralParameters);
        height.Should().BeInRange(MinClampHeight, MaxClampHeight);
        height.Should().BeApproximately(SeaLevel + 5f, precision: 10f,
            because: "neutral climate should produce height near sea level");
    }
}
