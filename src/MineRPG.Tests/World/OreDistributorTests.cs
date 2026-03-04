using System;
using System.Collections.Generic;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Generation.Ores;

namespace MineRPG.Tests.World;

public sealed class OreDistributorTests
{
    private const ushort StoneBlockId = 1;
    private const ushort IronOreId = 20;
    private const ushort DiamondOreId = 21;

    [Fact]
    public void Distribute_PlacesOresInChunk()
    {
        // Arrange
        List<OreDefinition> ores = new List<OreDefinition>
        {
            new OreDefinition
            {
                BlockName = "Iron Ore",
                BlockId = IronOreId,
                MinHeight = 10,
                MaxHeight = 80,
                PeakHeight = 40,
                VeinSize = 8,
                Frequency = 20,
                Distribution = OreDistribution.Triangle,
            },
        };
        OreDistributor distributor = new OreDistributor(ores, StoneBlockId);

        ChunkData data = new ChunkData(new ChunkCoord(0, 0));

        // Fill chunk with stone
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 0; y < ChunkData.SizeY; y++)
                {
                    data.SetBlock(x, y, z, StoneBlockId);
                }
            }
        }

        Random random = new Random(42);

        // Act
        distributor.Distribute(data, 0, 0, random);

        // Assert — count ore blocks
        int oreCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 0; y < ChunkData.SizeY; y++)
                {
                    if (data.GetBlock(x, y, z) == IronOreId)
                    {
                        oreCount++;
                    }
                }
            }
        }

        oreCount.Should().BeGreaterThan(0, "iron ore should be placed in the chunk");
    }

    [Fact]
    public void Distribute_RespectsHeightRange()
    {
        // Arrange
        List<OreDefinition> ores = new List<OreDefinition>
        {
            new OreDefinition
            {
                BlockName = "Diamond Ore",
                BlockId = DiamondOreId,
                MinHeight = 5,
                MaxHeight = 16,
                PeakHeight = 8,
                VeinSize = 4,
                Frequency = 10,
                Distribution = OreDistribution.Triangle,
            },
        };
        OreDistributor distributor = new OreDistributor(ores, StoneBlockId);

        ChunkData data = new ChunkData(new ChunkCoord(0, 0));

        // Fill with stone
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 0; y < ChunkData.SizeY; y++)
                {
                    data.SetBlock(x, y, z, StoneBlockId);
                }
            }
        }

        Random random = new Random(42);

        // Act
        distributor.Distribute(data, 0, 0, random);

        // Assert — no diamonds above Y=20 (some tolerance for vein spread)
        int highDiamondCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 25; y < ChunkData.SizeY; y++)
                {
                    if (data.GetBlock(x, y, z) == DiamondOreId)
                    {
                        highDiamondCount++;
                    }
                }
            }
        }

        highDiamondCount.Should().Be(0, "diamonds should not generate above Y=25");
    }

    [Fact]
    public void Distribute_ZeroBlockId_SkipsOre()
    {
        // Arrange
        List<OreDefinition> ores = new List<OreDefinition>
        {
            new OreDefinition
            {
                BlockName = "Missing",
                BlockId = 0,
                MinHeight = 0,
                MaxHeight = 100,
                PeakHeight = 50,
                VeinSize = 8,
                Frequency = 20,
                Distribution = OreDistribution.Uniform,
            },
        };
        OreDistributor distributor = new OreDistributor(ores, StoneBlockId);
        ChunkData data = new ChunkData(new ChunkCoord(0, 0));
        Random random = new Random(42);

        // Act — should not throw
        distributor.Distribute(data, 0, 0, random);

        // Assert — chunk should remain all air (no blocks placed)
        data.GetBlock(0, 0, 0).Should().Be(0);
    }
}
