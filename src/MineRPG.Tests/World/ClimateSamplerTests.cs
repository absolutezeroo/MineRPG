using FluentAssertions;

using MineRPG.World.Biomes.Climate;

namespace MineRPG.Tests.World;

public sealed class ClimateSamplerTests
{
    private const int DefaultSeed = 42;
    private const int TestWorldX = 100;
    private const int TestWorldZ = 200;
    private const int SampleStride = 50;
    private const int SampleRangeMin = -500;
    private const int SampleRangeMax = 500;
    private const float MinParameterValue = -1f;
    private const float MaxParameterValue = 1f;
    private const float SurfaceDepth = 0f;
    private const int SurfaceY = 64;
    private const int DeepUndergroundY = 10;
    private const int ShallowUndergroundY = 50;
    private const int AboveSurfaceY = 80;
    private const float WeirdnessAtZero = 0f;
    private const float WeirdnessAtOneThird = 1f / 3f;
    private const float WeirdnessAtTwoThirds = 2f / 3f;
    private const float ExpectedPvAtZero = -1f;
    private const float ExpectedPvAtOneThird = 0f;
    private const float ExpectedPvAtTwoThirds = 1f;
    private const float FloatTolerance = 0.0001f;

    private static ClimateSampler CreateSampler(int seed = DefaultSeed)
    {
        ClimateNoiseConfig config = ClimateNoiseConfig.CreateDefault();
        return new ClimateSampler(config, seed);
    }

    [Fact]
    public void SampleSurface_IsDeterministic()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act
        ClimateParameters first = sampler.SampleSurface(TestWorldX, TestWorldZ);
        ClimateParameters second = sampler.SampleSurface(TestWorldX, TestWorldZ);

        // Assert
        first.Continentalness.Should().Be(second.Continentalness);
        first.Erosion.Should().Be(second.Erosion);
        first.PeaksAndValleys.Should().Be(second.PeaksAndValleys);
        first.Temperature.Should().Be(second.Temperature);
        first.Humidity.Should().Be(second.Humidity);
        first.Depth.Should().Be(second.Depth);
    }

    [Fact]
    public void SampleSurface_AllParametersInRange()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act & Assert
        for (int x = SampleRangeMin; x <= SampleRangeMax; x += SampleStride)
        {
            for (int z = SampleRangeMin; z <= SampleRangeMax; z += SampleStride)
            {
                ClimateParameters parameters = sampler.SampleSurface(x, z);

                parameters.Continentalness.Should().BeInRange(MinParameterValue, MaxParameterValue,
                    $"continentalness at ({x},{z}) should be in [-1, 1]");
                parameters.Erosion.Should().BeInRange(MinParameterValue, MaxParameterValue,
                    $"erosion at ({x},{z}) should be in [-1, 1]");
                parameters.PeaksAndValleys.Should().BeInRange(MinParameterValue, MaxParameterValue,
                    $"peaks and valleys at ({x},{z}) should be in [-1, 1]");
                parameters.Temperature.Should().BeInRange(MinParameterValue, MaxParameterValue,
                    $"temperature at ({x},{z}) should be in [-1, 1]");
                parameters.Humidity.Should().BeInRange(MinParameterValue, MaxParameterValue,
                    $"humidity at ({x},{z}) should be in [-1, 1]");
            }
        }
    }

    [Fact]
    public void SampleSurface_DepthIsZero()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act
        ClimateParameters parameters = sampler.SampleSurface(TestWorldX, TestWorldZ);

        // Assert
        parameters.Depth.Should().Be(SurfaceDepth);
    }

    [Fact]
    public void SampleSurface_DifferentPositions_ProduceDifferentValues()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act
        ClimateParameters first = sampler.SampleSurface(0, 0);
        ClimateParameters second = sampler.SampleSurface(1000, 1000);

        // Assert — at least one parameter should differ between distant positions
        bool anyDifference =
            first.Continentalness != second.Continentalness ||
            first.Erosion != second.Erosion ||
            first.PeaksAndValleys != second.PeaksAndValleys ||
            first.Temperature != second.Temperature ||
            first.Humidity != second.Humidity;

        anyDifference.Should().BeTrue(
            "distant positions should produce different climate values");
    }

    [Fact]
    public void SampleFull_DepthIncreasesWithLowerY()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act
        ClimateParameters shallow = sampler.SampleFull(TestWorldX, ShallowUndergroundY, TestWorldZ, SurfaceY);
        ClimateParameters deep = sampler.SampleFull(TestWorldX, DeepUndergroundY, TestWorldZ, SurfaceY);

        // Assert
        deep.Depth.Should().BeGreaterThan(shallow.Depth,
            "deeper positions should have higher depth values");
    }

    [Fact]
    public void SampleFull_AtSurface_DepthIsZero()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act
        ClimateParameters parameters = sampler.SampleFull(TestWorldX, SurfaceY, TestWorldZ, SurfaceY);

        // Assert
        parameters.Depth.Should().Be(SurfaceDepth);
    }

    [Fact]
    public void SampleFull_AboveSurface_DepthIsZero()
    {
        // Arrange
        ClimateSampler sampler = CreateSampler();

        // Act
        ClimateParameters parameters = sampler.SampleFull(TestWorldX, AboveSurfaceY, TestWorldZ, SurfaceY);

        // Assert
        parameters.Depth.Should().Be(SurfaceDepth);
    }

    [Fact]
    public void WeirdnessToPeaksAndValleys_AtZero_ReturnsNegativeOne()
    {
        // Act
        float result = ClimateSampler.WeirdnessToPeaksAndValleys(WeirdnessAtZero);

        // Assert
        result.Should().BeApproximately(ExpectedPvAtZero, FloatTolerance);
    }

    [Fact]
    public void WeirdnessToPeaksAndValleys_AtOneThird_ReturnsZero()
    {
        // Act
        float result = ClimateSampler.WeirdnessToPeaksAndValleys(WeirdnessAtOneThird);

        // Assert
        result.Should().BeApproximately(ExpectedPvAtOneThird, FloatTolerance);
    }

    [Fact]
    public void WeirdnessToPeaksAndValleys_AtTwoThirds_ReturnsOne()
    {
        // Act
        float result = ClimateSampler.WeirdnessToPeaksAndValleys(WeirdnessAtTwoThirds);

        // Assert
        result.Should().BeApproximately(ExpectedPvAtTwoThirds, FloatTolerance);
    }
}
