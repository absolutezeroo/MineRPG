using System;

using FluentAssertions;

using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class HeightSplineTests
{
    [Fact]
    public void Evaluate_AtControlPoint_ReturnsExactOutputY()
    {
        // Arrange
        HeightSpline spline = new HeightSpline(
        [
            new SplinePoint(-1f, 0f),
            new SplinePoint(0f, 62f),
            new SplinePoint(1f, 120f),
        ]);

        // Act & Assert
        spline.Evaluate(0f).Should().BeApproximately(62f, precision: 0.01f);
        spline.Evaluate(-1f).Should().BeApproximately(0f, precision: 0.01f);
        spline.Evaluate(1f).Should().BeApproximately(120f, precision: 0.01f);
    }

    [Fact]
    public void Evaluate_BelowRange_ClampsToFirstPoint()
    {
        // Arrange
        HeightSpline spline = new HeightSpline(
        [
            new SplinePoint(-1f, 10f),
            new SplinePoint(1f, 100f),
        ]);

        // Act
        float result = spline.Evaluate(-5f);

        // Assert
        result.Should().BeApproximately(10f, precision: 0.01f);
    }

    [Fact]
    public void Evaluate_AboveRange_ClampsToLastPoint()
    {
        // Arrange
        HeightSpline spline = new HeightSpline(
        [
            new SplinePoint(-1f, 10f),
            new SplinePoint(1f, 100f),
        ]);

        // Act
        float result = spline.Evaluate(5f);

        // Assert
        result.Should().BeApproximately(100f, precision: 0.01f);
    }

    [Fact]
    public void Evaluate_BetweenPoints_InterpolatesSmoothly()
    {
        // Arrange
        HeightSpline spline = new HeightSpline(
        [
            new SplinePoint(-1f, 0f),
            new SplinePoint(0f, 50f),
            new SplinePoint(1f, 100f),
        ]);

        // Act
        float result = spline.Evaluate(0.5f);

        // Assert - should be between 50 and 100
        result.Should().BeInRange(50f, 100f);
    }

    [Fact]
    public void Evaluate_IsMonotone_WhenControlPointsAreMonotone()
    {
        // Arrange - Fritsch-Carlson should prevent overshoot
        HeightSpline spline = HeightSpline.CreateDefault(64f, 30f);

        // Act & Assert
        float prev = float.MinValue;
        for (int i = 0; i <= 100; i++)
        {
            float input = -1f + i * 0.02f;
            float output = spline.Evaluate(input);
            output.Should().BeGreaterThanOrEqualTo(prev,
                $"spline should be monotone at input {input}");
            prev = output;
        }
    }

    [Fact]
    public void CreateDefault_ProducesExpectedBaseValue()
    {
        // Arrange & Act
        HeightSpline spline = HeightSpline.CreateDefault(64f, 20f);

        // Assert
        spline.Evaluate(0f).Should().BeApproximately(64f, precision: 0.01f);
        spline.Evaluate(-1f).Should().BeApproximately(44f, precision: 0.01f);
        spline.Evaluate(1f).Should().BeApproximately(84f, precision: 0.01f);
    }

    [Fact]
    public void Constructor_WithFewerThanTwoPoints_ThrowsArgumentException()
    {
        // Act
        Action act = () => new HeightSpline([new SplinePoint(0f, 50f)]);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least 2*");
    }
}
