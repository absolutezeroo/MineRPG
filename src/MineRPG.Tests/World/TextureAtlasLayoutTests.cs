using FluentAssertions;
using MineRPG.World.Blocks;

namespace MineRPG.Tests.World;

public sealed class TextureAtlasLayoutTests
{
    [Fact]
    public void Constructor_WithFourTextures_Creates2x2Grid()
    {
        // Arrange & Act
        var layout = new TextureAtlasLayout(["stone", "dirt", "grass", "sand"]);

        // Assert
        layout.TextureNames.Should().HaveCount(4);
        layout.Columns.Should().Be(2);
        layout.Rows.Should().Be(2);
    }

    [Fact]
    public void Constructor_WithThreeTextures_Creates2x2Grid()
    {
        // Arrange & Act
        var layout = new TextureAtlasLayout(["a", "b", "c"]);

        // Assert
        layout.Columns.Should().Be(2);
        layout.Rows.Should().Be(2);
    }

    [Fact]
    public void Constructor_DeduplicatesNames_CaseInsensitive()
    {
        // Arrange & Act
        var layout = new TextureAtlasLayout(["Stone", "stone", "STONE"]);

        // Assert
        layout.TextureNames.Should().HaveCount(1);
    }

    [Fact]
    public void GetUvBounds_FirstTexture_ReturnsTopLeftTile()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["stone", "dirt", "grass", "sand"]);

        // Act
        var (u0, v0, u1, v1) = layout.GetUvBounds("stone");

        // Assert — 2x2 grid, first tile at (0,0)
        u0.Should().BeApproximately(0f, 0.0001f);
        v0.Should().BeApproximately(0f, 0.0001f);
        u1.Should().BeApproximately(0.5f, 0.0001f);
        v1.Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void GetUvBounds_SecondTexture_ReturnsCorrectTile()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["stone", "dirt", "grass", "sand"]);

        // Act
        var (u0, v0, u1, v1) = layout.GetUvBounds("dirt");

        // Assert — 2x2 grid, second tile at (1,0)
        u0.Should().BeApproximately(0.5f, 0.0001f);
        v0.Should().BeApproximately(0f, 0.0001f);
        u1.Should().BeApproximately(1f, 0.0001f);
        v1.Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void GetUvBounds_ThirdTexture_ReturnsSecondRow()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["stone", "dirt", "grass", "sand"]);

        // Act
        var (u0, v0, u1, v1) = layout.GetUvBounds("grass");

        // Assert — 2x2 grid, third tile at (0,1)
        u0.Should().BeApproximately(0f, 0.0001f);
        v0.Should().BeApproximately(0.5f, 0.0001f);
        u1.Should().BeApproximately(0.5f, 0.0001f);
        v1.Should().BeApproximately(1f, 0.0001f);
    }

    [Fact]
    public void GetUvBounds_UnknownTexture_Throws()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["stone"]);

        // Act
        var act = () => layout.GetUvBounds("unknown");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*unknown*");
    }

    [Fact]
    public void GetUvBounds_CaseInsensitive()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["Stone"]);

        // Act
        var (u0, v0, u1, v1) = layout.GetUvBounds("stone");

        // Assert
        u0.Should().Be(0f);
        v0.Should().Be(0f);
    }

    [Fact]
    public void GetGridPosition_ReturnsCorrectPosition()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["a", "b", "c", "d", "e"]);

        // Act
        var (col, row) = layout.GetGridPosition("e");

        // Assert — 3 columns, 5th element is at index 4 → (1, 1)
        col.Should().Be(1);
        row.Should().Be(1);
    }

    [Fact]
    public void Contains_ReturnsTrue_ForKnownTexture()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["stone"]);

        // Act & Assert
        layout.Contains("stone").Should().BeTrue();
        layout.Contains("Stone").Should().BeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_ForUnknownTexture()
    {
        // Arrange
        var layout = new TextureAtlasLayout(["stone"]);

        // Act & Assert
        layout.Contains("dirt").Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithEmptyInput_CreatesEmptyLayout()
    {
        // Arrange & Act
        var layout = new TextureAtlasLayout([]);

        // Assert
        layout.TextureNames.Should().BeEmpty();
        layout.Columns.Should().Be(0);
        layout.Rows.Should().Be(0);
    }
}
