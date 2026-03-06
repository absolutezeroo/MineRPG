using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.World.Blocks;
using MineRPG.World.Generation;

using NSubstitute;

namespace MineRPG.Tests.World;

public sealed class BiomeBlockResolverTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    private static BlockRegistry CreateRegistryWithBlocks(params (ushort id, string name)[] blocks)
    {
        IDataLoader dataLoader = Substitute.For<IDataLoader>();
        dataLoader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(blocks.Select(b => new BlockDefinition { Id = b.id, Name = b.name }).ToList());
        return new BlockRegistry(dataLoader, NullLogger.Instance);
    }

    [Fact]
    public void ResolveAll_WithValidNames_OverridesNumericIds()
    {
        // Arrange
        BlockRegistry registry = CreateRegistryWithBlocks((1, "Stone"), (2, "Dirt"), (3, "Grass"));
        BiomeDefinition biome = new BiomeDefinition
        {
            Id = "test",
            SurfaceBlock = 99,
            SubSurfaceBlock = 99,
            StoneBlock = 99,
            SurfaceBlockName = "Grass",
            SubSurfaceBlockName = "Dirt",
            StoneBlockName = "Stone",
        };

        // Act
        BiomeBlockResolver.ResolveAll([biome], registry, _logger);

        // Assert
        biome.SurfaceBlock.Should().Be(3);
        biome.SubSurfaceBlock.Should().Be(2);
        biome.StoneBlock.Should().Be(1);
    }

    [Fact]
    public void ResolveAll_WithNullNames_KeepsNumericIds()
    {
        // Arrange
        BlockRegistry registry = CreateRegistryWithBlocks((1, "Stone"));
        BiomeDefinition biome = new BiomeDefinition
        {
            Id = "test",
            SurfaceBlock = 5,
            SubSurfaceBlock = 6,
            StoneBlock = 7,
        };

        // Act
        BiomeBlockResolver.ResolveAll([biome], registry, _logger);

        // Assert - unchanged
        biome.SurfaceBlock.Should().Be(5);
        biome.SubSurfaceBlock.Should().Be(6);
        biome.StoneBlock.Should().Be(7);
    }

    [Fact]
    public void ResolveAll_WithUnknownName_FallsBackToNumericId()
    {
        // Arrange
        ILogger logger = Substitute.For<ILogger>();
        BlockRegistry registry = CreateRegistryWithBlocks((1, "Stone"));
        BiomeDefinition biome = new BiomeDefinition
        {
            Id = "test",
            SurfaceBlock = 42,
            SurfaceBlockName = "NonExistentBlock",
        };

        // Act
        BiomeBlockResolver.ResolveAll([biome], registry, logger);

        // Assert - falls back to numeric ID
        biome.SurfaceBlock.Should().Be(42);

        // Assert - warning was logged
        logger.Received(1).Warning(
            Arg.Is<string>(s => s.Contains("unknown block")),
            Arg.Any<object>(), Arg.Any<object>(), Arg.Any<object>(), Arg.Any<object>());
    }
}
