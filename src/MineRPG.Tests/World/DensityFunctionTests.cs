using FluentAssertions;

using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class DensityFunctionTests
{
    private const int WorldSeed = 42;
    private const int SurfaceY = 64;
    private const int WellAboveSurfaceOffset = 50;
    private const int WellBelowSurfaceOffset = 50;
    private const int FarBelowSurfaceOffset = 100;
    private const int TestWorldX = 100;
    private const int TestWorldZ = 100;
    private const float HighErosion = 0.5f;
    private const float FloatTolerance = 1.0f;

    [Fact]
    public void GetDensity_WellAboveSurface_ReturnsNegative()
    {
        // Arrange
        DensityFunction densityFunction = new DensityFunction(WorldSeed);
        int worldY = SurfaceY + WellAboveSurfaceOffset;

        // Act
        float density = densityFunction.GetDensity(TestWorldX, worldY, TestWorldZ, SurfaceY, HighErosion);

        // Assert
        density.Should().BeNegative(
            "positions well above the surface should be air (negative density)");
    }

    [Fact]
    public void GetDensity_WellBelowSurface_ReturnsPositive()
    {
        // Arrange
        DensityFunction densityFunction = new DensityFunction(WorldSeed);
        int worldY = SurfaceY - WellBelowSurfaceOffset;

        // Act
        float density = densityFunction.GetDensity(TestWorldX, worldY, TestWorldZ, SurfaceY, HighErosion);

        // Assert
        density.Should().BePositive(
            "positions well below the surface should be solid (positive density)");
    }

    [Fact]
    public void GetDensity_AtSurface_ReturnsApproximatelyZero()
    {
        // Arrange
        DensityFunction densityFunction = new DensityFunction(WorldSeed);

        // Act
        float density = densityFunction.GetDensity(TestWorldX, SurfaceY, TestWorldZ, SurfaceY, HighErosion);

        // Assert — at the surface, base density is 0; with high erosion overhang noise is suppressed
        density.Should().BeApproximately(0f, FloatTolerance,
            "density at the surface should be approximately zero");
    }

    [Fact]
    public void GetDensity_FarFromSurface_ReturnsBaseDensity()
    {
        // Arrange — far below surface, outside the overhang band, density = surfaceY - worldY
        DensityFunction densityFunction = new DensityFunction(WorldSeed);
        int worldY = SurfaceY - FarBelowSurfaceOffset;
        float expectedBaseDensity = SurfaceY - worldY;

        // Act
        float density = densityFunction.GetDensity(TestWorldX, worldY, TestWorldZ, SurfaceY, HighErosion);

        // Assert — outside the overhang band, no noise modulation is applied
        density.Should().Be(expectedBaseDensity,
            "far from the surface the density should equal the base density without overhang noise");
    }
}
