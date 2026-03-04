using System;

using FluentAssertions;

using MineRPG.Core.Extensions;

namespace MineRPG.Tests.Core;

public sealed class MathExtensionsTests
{
    [Theory]
    [InlineData(5, 1, 10, true)]
    [InlineData(0, 0, 10, true)]
    [InlineData(10, 0, 10, true)]
    [InlineData(-1, 0, 10, false)]
    [InlineData(11, 0, 10, false)]
    public void IsBetween_Int_ReturnsExpected(int value, int min, int max, bool expected)
    {
        value.IsBetween(min, max).Should().Be(expected);
    }

    [Theory]
    [InlineData(0.5f, 0f, 1f, true)]
    [InlineData(-0.1f, 0f, 1f, false)]
    [InlineData(1.1f, 0f, 1f, false)]
    public void IsBetween_Float_ReturnsExpected(float value, float min, float max, bool expected)
    {
        value.IsBetween(min, max).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(5, 8)]
    [InlineData(16, 16)]
    [InlineData(17, 32)]
    public void NextPowerOfTwo_ReturnsExpected(int value, int expected)
    {
        value.NextPowerOfTwo().Should().Be(expected);
    }

    [Fact]
    public void Remap_MapsValueCorrectly()
    {
        // Arrange
        float value = 0.5f;

        // Act
        float result = value.Remap(0f, 1f, 0f, 100f);

        // Assert
        result.Should().BeApproximately(50f, 0.001f);
    }

    [Theory]
    [InlineData(3, 4, 3)]
    [InlineData(-1, 4, 3)]
    [InlineData(0, 4, 0)]
    [InlineData(4, 4, 0)]
    [InlineData(-5, 3, 1)]
    public void Wrap_ReturnsExpected(int value, int max, int expected)
    {
        value.Wrap(max).Should().Be(expected);
    }

    [Fact]
    public void ToRadians_ConvertsCorrectly()
    {
        180f.ToRadians().Should().BeApproximately(MathF.PI, 0.0001f);
    }

    [Fact]
    public void ToDegrees_ConvertsCorrectly()
    {
        MathF.PI.ToDegrees().Should().BeApproximately(180f, 0.01f);
    }

    [Fact]
    public void ToRadians_ToDegrees_RoundTrip()
    {
        // Arrange
        float degrees = 45f;

        // Act
        float result = degrees.ToRadians().ToDegrees();

        // Assert
        result.Should().BeApproximately(degrees, 0.001f);
    }
}
