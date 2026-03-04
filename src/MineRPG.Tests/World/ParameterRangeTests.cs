using FluentAssertions;

using MineRPG.World.Biomes.Climate;

namespace MineRPG.Tests.World;

public sealed class ParameterRangeTests
{
    private const float RangeMin = -0.5f;
    private const float RangeMax = 0.5f;
    private const float ValueInsideRange = 0.0f;
    private const float ValueBelowMin = -0.8f;
    private const float ValueAboveMax = 0.9f;
    private const float FloatTolerance = 0.0001f;

    [Fact]
    public void DistanceTo_ValueInside_ReturnsZero()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);

        // Act
        float distance = range.DistanceTo(ValueInsideRange);

        // Assert
        distance.Should().Be(0f);
    }

    [Fact]
    public void DistanceTo_ValueBelowMin_ReturnsDistanceToMin()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);
        float expectedDistance = RangeMin - ValueBelowMin;

        // Act
        float distance = range.DistanceTo(ValueBelowMin);

        // Assert
        distance.Should().BeApproximately(expectedDistance, FloatTolerance);
    }

    [Fact]
    public void DistanceTo_ValueAboveMax_ReturnsDistanceToMax()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);
        float expectedDistance = ValueAboveMax - RangeMax;

        // Act
        float distance = range.DistanceTo(ValueAboveMax);

        // Assert
        distance.Should().BeApproximately(expectedDistance, FloatTolerance);
    }

    [Fact]
    public void Contains_ValueInside_ReturnsTrue()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);

        // Act
        bool isContained = range.Contains(ValueInsideRange);

        // Assert
        isContained.Should().BeTrue();
    }

    [Fact]
    public void Contains_ValueOutside_ReturnsFalse()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);

        // Act
        bool isBelowContained = range.Contains(ValueBelowMin);
        bool isAboveContained = range.Contains(ValueAboveMax);

        // Assert
        isBelowContained.Should().BeFalse();
        isAboveContained.Should().BeFalse();
    }

    [Fact]
    public void Center_ReturnsMiddleOfRange()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);
        float expectedCenter = (RangeMin + RangeMax) * 0.5f;

        // Act
        float center = range.Center;

        // Assert
        center.Should().BeApproximately(expectedCenter, FloatTolerance);
    }

    [Fact]
    public void Contains_ValueAtMin_ReturnsTrue()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);

        // Act
        bool isContained = range.Contains(RangeMin);

        // Assert
        isContained.Should().BeTrue();
    }

    [Fact]
    public void Contains_ValueAtMax_ReturnsTrue()
    {
        // Arrange
        ParameterRange range = new ParameterRange(RangeMin, RangeMax);

        // Act
        bool isContained = range.Contains(RangeMax);

        // Assert
        isContained.Should().BeTrue();
    }

    [Fact]
    public void Full_CoversMinus1To1()
    {
        // Act
        ParameterRange full = ParameterRange.Full;

        // Assert
        full.Min.Should().Be(-1f);
        full.Max.Should().Be(1f);
    }

    [Fact]
    public void FullDepth_Covers0To1()
    {
        // Act
        ParameterRange fullDepth = ParameterRange.FullDepth;

        // Assert
        fullDepth.Min.Should().Be(0f);
        fullDepth.Max.Should().Be(1f);
    }
}
