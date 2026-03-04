using FluentAssertions;

using MineRPG.World.Biomes;

namespace MineRPG.Tests.World;

public sealed class BiomeColumnTests
{
    [Fact]
    public void Constructor_SetsAllCellsToDefault()
    {
        // Arrange & Act
        BiomeColumn column = new BiomeColumn("plains");

        // Assert
        for (int x = 0; x < BiomeColumn.CellsX; x++)
        {
            for (int y = 0; y < BiomeColumn.CellsY; y++)
            {
                for (int z = 0; z < BiomeColumn.CellsZ; z++)
                {
                    column.GetBiomeId(x, y, z).Should().Be("plains");
                }
            }
        }
    }

    [Fact]
    public void SetBiomeId_GetBiomeId_RoundTrips()
    {
        // Arrange
        BiomeColumn column = new BiomeColumn("plains");

        // Act
        column.SetBiomeId(2, 10, 3, "desert");

        // Assert
        column.GetBiomeId(2, 10, 3).Should().Be("desert");
        column.GetBiomeId(0, 0, 0).Should().Be("plains");
    }

    [Fact]
    public void GetBiomeIdAtBlock_ConvertsToCell()
    {
        // Arrange
        BiomeColumn column = new BiomeColumn("plains");
        column.SetBiomeId(1, 5, 2, "forest");

        // Act — block (6, 22, 10) maps to cell (1, 5, 2) with CellSize=4
        string result = column.GetBiomeIdAtBlock(6, 22, 10);

        // Assert
        result.Should().Be("forest");
    }

    [Fact]
    public void SetSurfaceBiome_SetsAllYLevels()
    {
        // Arrange
        BiomeColumn column = new BiomeColumn("plains");

        // Act
        column.SetSurfaceBiome(1, 2, "desert");

        // Assert
        for (int y = 0; y < BiomeColumn.CellsY; y++)
        {
            column.GetBiomeId(1, y, 2).Should().Be("desert");
        }

        // Other columns unchanged
        column.GetBiomeId(0, 0, 0).Should().Be("plains");
    }

    [Fact]
    public void SetCaveBiome_SetsOnlyBelowSurface()
    {
        // Arrange
        BiomeColumn column = new BiomeColumn("plains");
        int surfaceCellY = 10;

        // Act
        column.SetCaveBiome(1, 2, surfaceCellY, "dripstone_caves");

        // Assert — below surface
        for (int y = 0; y < surfaceCellY; y++)
        {
            column.GetBiomeId(1, y, 2).Should().Be("dripstone_caves");
        }

        // At and above surface — unchanged
        for (int y = surfaceCellY; y < BiomeColumn.CellsY; y++)
        {
            column.GetBiomeId(1, y, 2).Should().Be("plains");
        }
    }

    [Fact]
    public void TotalCells_EqualsExpected()
    {
        // 4x64x4 = 1024
        BiomeColumn.TotalCells.Should().Be(
            BiomeColumn.CellsX * BiomeColumn.CellsY * BiomeColumn.CellsZ);
    }

    [Fact]
    public void GetRawSpan_ReturnsAllCells()
    {
        // Arrange
        BiomeColumn column = new BiomeColumn("ocean");

        // Act
        System.ReadOnlySpan<string> span = column.GetRawSpan();

        // Assert
        span.Length.Should().Be(BiomeColumn.TotalCells);
    }
}
