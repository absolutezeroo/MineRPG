using FluentAssertions;
using MineRPG.World.Blocks;

namespace MineRPG.Tests.World;

public sealed class BlockFaceTexturesTests
{
    [Fact]
    public void Resolve_WithAllOnly_FillsAllSixFaces()
    {
        // Arrange
        var textures = new BlockFaceTextures { All = "stone" };

        // Act
        var result = textures.Resolve();

        // Assert
        result.Should().HaveCount(6);
        result.Should().AllSatisfy(name => name.Should().Be("stone"));
    }

    [Fact]
    public void Resolve_WithTopBottomSide_AssignsCorrectly()
    {
        // Arrange
        var textures = new BlockFaceTextures
        {
            Top = "grass_top",
            Bottom = "dirt",
            Side = "grass_side",
        };

        // Act
        var result = textures.Resolve();

        // Assert
        result[0].Should().Be("grass_side"); // +X east
        result[1].Should().Be("grass_side"); // -X west
        result[2].Should().Be("grass_top");  // +Y top
        result[3].Should().Be("dirt");        // -Y bottom
        result[4].Should().Be("grass_side"); // +Z south
        result[5].Should().Be("grass_side"); // -Z north
    }

    [Fact]
    public void Resolve_IndividualFace_OverridesGroup()
    {
        // Arrange
        var textures = new BlockFaceTextures
        {
            Side = "wood_side",
            East = "wood_special",
        };

        // Act
        var result = textures.Resolve();

        // Assert
        result[0].Should().Be("wood_special"); // east overridden
        result[1].Should().Be("wood_side");     // west still from side
        result[4].Should().Be("wood_side");     // south still from side
        result[5].Should().Be("wood_side");     // north still from side
    }

    [Fact]
    public void Resolve_AllOverriddenByGroupAndIndividual()
    {
        // Arrange
        var textures = new BlockFaceTextures
        {
            All = "fallback",
            Top = "top_tex",
            North = "north_tex",
        };

        // Act
        var result = textures.Resolve();

        // Assert
        result[0].Should().Be("fallback");   // east from all
        result[1].Should().Be("fallback");   // west from all
        result[2].Should().Be("top_tex");    // top overridden by group
        result[3].Should().Be("fallback");   // bottom from all
        result[4].Should().Be("fallback");   // south from all
        result[5].Should().Be("north_tex");  // north overridden by individual
    }

    [Fact]
    public void Resolve_WithNoProperties_ReturnsAllNulls()
    {
        // Arrange
        var textures = new BlockFaceTextures();

        // Act
        var result = textures.Resolve();

        // Assert
        result.Should().HaveCount(6);
        result.Should().AllSatisfy(name => name.Should().BeNull());
    }
}
