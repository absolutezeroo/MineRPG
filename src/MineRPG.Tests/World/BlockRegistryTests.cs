using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.World.Blocks;

using NSubstitute;

namespace MineRPG.Tests.World;

public sealed class BlockRegistryTests
{
    [Fact]
    public void Constructor_AlwaysRegistersAirBlock()
    {
        // Arrange
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([]);

        // Act
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Assert
        registry.Get(0).Should().NotBeNull();
        registry.Get(0).DisplayName.Should().Be("Air");
    }

    [Fact]
    public void Get_WithUnknownRuntimeId_ReturnsAir()
    {
        // Arrange
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([]);
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Act
        BlockDefinition result = registry.Get(99);

        // Assert
        result.Id.Should().Be("minerpg:air");
    }

    [Fact]
    public void Constructor_AssignsSequentialRuntimeIds()
    {
        // Arrange
        BlockDefinition stoneDef = new BlockDefinition
        {
            Id = "minerpg:stone",
            DisplayName = "Stone",
            Flags = BlockFlags.Solid,
            Textures = new BlockFaceTextures { All = "stone" },
        };
        BlockDefinition dirtDef = new BlockDefinition
        {
            Id = "minerpg:dirt",
            DisplayName = "Dirt",
            Flags = BlockFlags.Solid,
            Textures = new BlockFaceTextures { All = "dirt" },
        };
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([stoneDef, dirtDef]);

        // Act
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Assert — RuntimeId 0 = air, 1 = stone, 2 = dirt
        registry.Get(0).Id.Should().Be("minerpg:air");
        registry.Get(1).Id.Should().Be("minerpg:stone");
        registry.Get(1).RuntimeId.Should().Be(1);
        registry.Get(2).Id.Should().Be("minerpg:dirt");
        registry.Get(2).RuntimeId.Should().Be(2);
    }

    [Fact]
    public void Constructor_ComputesPerFaceUvsFromTextures()
    {
        // Arrange
        BlockDefinition stoneDef = new BlockDefinition
        {
            Id = "minerpg:stone",
            DisplayName = "Stone",
            Flags = BlockFlags.Solid,
            Textures = new BlockFaceTextures { All = "stone" },
        };
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([stoneDef]);

        // Act
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);
        BlockDefinition def = registry.Get(1);

        // Assert - single texture = 1x1 grid, full UV range
        def.FaceUvs[0].Should().BeApproximately(0f, 0.0001f);  // face 0 u0
        def.FaceUvs[1].Should().BeApproximately(0f, 0.0001f);  // face 0 v0
        def.FaceUvs[2].Should().BeApproximately(1f, 0.0001f);  // face 0 u1
        def.FaceUvs[3].Should().BeApproximately(1f, 0.0001f);  // face 0 v1
    }

    [Fact]
    public void Constructor_WithMultipleTextures_AssignsDistinctUvs()
    {
        // Arrange
        BlockDefinition grassDef = new BlockDefinition
        {
            Id = "minerpg:grass",
            DisplayName = "Grass",
            Flags = BlockFlags.Solid,
            Textures = new BlockFaceTextures
            {
                Top = "grass_top",
                Bottom = "dirt",
                Side = "grass_side",
            },
        };
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([grassDef]);

        // Act
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);
        BlockDefinition def = registry.Get(1);

        // Assert - top face (index 2) should differ from side faces (index 0)
        float topU0 = def.FaceUvs[2 * 4 + 0];
        float sideU0 = def.FaceUvs[0 * 4 + 0];
        float bottomU0 = def.FaceUvs[3 * 4 + 0];

        // All 4 side faces should have same UVs
        def.FaceUvs[0 * 4 + 0].Should().Be(def.FaceUvs[1 * 4 + 0]); // east == west
        def.FaceUvs[0 * 4 + 0].Should().Be(def.FaceUvs[4 * 4 + 0]); // east == south
        def.FaceUvs[0 * 4 + 0].Should().Be(def.FaceUvs[5 * 4 + 0]); // east == north

        // top, bottom, side should not all be the same
        new[] { topU0, bottomU0, sideU0 }.Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void TryGet_WithValidId_ReturnsTrue()
    {
        // Arrange
        BlockDefinition stoneDef = new BlockDefinition
        {
            Id = "minerpg:stone",
            DisplayName = "Stone",
            Flags = BlockFlags.Solid,
        };
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([stoneDef]);
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Act
        bool isFound = registry.TryGet("minerpg:stone", out BlockDefinition def);

        // Assert
        isFound.Should().BeTrue();
        def.DisplayName.Should().Be("Stone");
        def.RuntimeId.Should().Be(1);
    }

    [Fact]
    public void TryGet_WithUnknownId_ReturnsFalse()
    {
        // Arrange
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([]);
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Act
        bool isFound = registry.TryGet("minerpg:unknown", out _);

        // Assert
        isFound.Should().BeFalse();
    }

    [Fact]
    public void GetById_WithValidId_ReturnsBlock()
    {
        // Arrange
        BlockDefinition stoneDef = new BlockDefinition
        {
            Id = "minerpg:stone",
            DisplayName = "Stone",
            Flags = BlockFlags.Solid,
        };
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([stoneDef]);
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Act
        BlockDefinition result = registry.GetById("minerpg:stone");

        // Assert
        result.DisplayName.Should().Be("Stone");
    }

    [Fact]
    public void GetById_WithUnknownId_Throws()
    {
        // Arrange
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([]);
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Act
        Action act = () => registry.GetById("minerpg:unknown");

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void AtlasLayout_ExposesCorrectTextureCount()
    {
        // Arrange
        BlockDefinition grassDef = new BlockDefinition
        {
            Id = "minerpg:grass",
            DisplayName = "Grass",
            Flags = BlockFlags.Solid,
            Textures = new BlockFaceTextures
            {
                Top = "grass_top",
                Bottom = "dirt",
                Side = "grass_side",
            },
        };
        IDataLoader loader = Substitute.For<IDataLoader>();
        loader.LoadAll<BlockDefinition>(Arg.Any<string>(), Arg.Any<bool>())
            .Returns([grassDef]);

        // Act
        BlockRegistry registry = new BlockRegistry(loader, NullLogger.Instance);

        // Assert
        registry.AtlasLayout.TextureNames.Should().HaveCount(3);
        registry.AtlasLayout.Contains("grass_top").Should().BeTrue();
        registry.AtlasLayout.Contains("dirt").Should().BeTrue();
        registry.AtlasLayout.Contains("grass_side").Should().BeTrue();
    }
}
