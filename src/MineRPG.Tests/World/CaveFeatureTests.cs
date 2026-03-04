using System;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Generation.CaveFeatures;

namespace MineRPG.Tests.World;

public sealed class CaveFeatureTests
{
    private const ushort StoneBlockId = 1;
    private const ushort FormationBlockId = 50;

    private static ChunkData CreateChunkWithCavity(int floorY, int ceilingY)
    {
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

        // Carve a cavity
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = floorY; y < ceilingY; y++)
                {
                    data.SetBlock(x, y, z, 0);
                }
            }
        }

        return data;
    }

    [Fact]
    public void StalactiteGenerator_PlacesBlocksDownwardFromCeiling()
    {
        // Arrange
        CaveFeatureConfig config = new CaveFeatureConfig
        {
            StalactiteChance = 1.0f,
            StalactiteMaxLength = 4,
        };
        StalactiteGenerator generator = new StalactiteGenerator(config, FormationBlockId);
        ChunkData data = CreateChunkWithCavity(10, 30);
        Random random = new Random(42);

        // Act
        generator.Generate(data, random);

        // Assert — some stalactite blocks should exist below the ceiling
        int formationCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 26; y < 30; y++)
                {
                    if (data.GetBlock(x, y, z) == FormationBlockId)
                    {
                        formationCount++;
                    }
                }
            }
        }

        formationCount.Should().BeGreaterThan(0, "stalactites should form below the ceiling");
    }

    [Fact]
    public void StalagmiteGenerator_PlacesBlocksUpwardFromFloor()
    {
        // Arrange
        CaveFeatureConfig config = new CaveFeatureConfig
        {
            StalagmiteChance = 1.0f,
            StalagmiteMaxHeight = 4,
        };
        StalagmiteGenerator generator = new StalagmiteGenerator(config, FormationBlockId);
        ChunkData data = CreateChunkWithCavity(10, 30);
        Random random = new Random(42);

        // Act
        generator.Generate(data, random);

        // Assert — some stalagmite blocks should exist above the floor
        int formationCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 10; y < 14; y++)
                {
                    if (data.GetBlock(x, y, z) == FormationBlockId)
                    {
                        formationCount++;
                    }
                }
            }
        }

        formationCount.Should().BeGreaterThan(0, "stalagmites should form above the floor");
    }

    [Fact]
    public void PillarGenerator_LargeCavity_PlacesPillars()
    {
        // Arrange
        CaveFeatureConfig config = new CaveFeatureConfig
        {
            PillarChance = 1.0f,
            PillarMinHeight = 15,
            PillarWidth = 1,
        };
        PillarGenerator generator = new PillarGenerator(config, FormationBlockId);
        ChunkData data = CreateChunkWithCavity(10, 40);
        Random random = new Random(42);

        // Act
        generator.Generate(data, random);

        // Assert — pillars span floor to ceiling
        int formationCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 10; y < 40; y++)
                {
                    if (data.GetBlock(x, y, z) == FormationBlockId)
                    {
                        formationCount++;
                    }
                }
            }
        }

        formationCount.Should().BeGreaterThan(0, "pillars should be placed in large cavities");
    }

    [Fact]
    public void PillarGenerator_SmallCavity_DoesNotPlacePillars()
    {
        // Arrange
        CaveFeatureConfig config = new CaveFeatureConfig
        {
            PillarChance = 1.0f,
            PillarMinHeight = 15,
        };
        PillarGenerator generator = new PillarGenerator(config, FormationBlockId);

        // Cavity only 5 blocks tall — below the 15-block minimum
        ChunkData data = CreateChunkWithCavity(10, 15);
        Random random = new Random(42);

        // Act
        generator.Generate(data, random);

        // Assert — no formations in the small cavity
        int formationCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 10; y < 15; y++)
                {
                    if (data.GetBlock(x, y, z) == FormationBlockId)
                    {
                        formationCount++;
                    }
                }
            }
        }

        formationCount.Should().Be(0, "cavity is too small for pillars");
    }

    [Fact]
    public void CaveFeaturePipeline_RunsAllGenerators()
    {
        // Arrange
        CaveFeatureConfig config = new CaveFeatureConfig
        {
            PillarChance = 0.5f,
            PillarMinHeight = 15,
            StalactiteChance = 0.5f,
            StalactiteMaxLength = 4,
            StalagmiteChance = 0.5f,
            StalagmiteMaxHeight = 4,
        };
        CaveFeaturePipeline pipeline = new CaveFeaturePipeline(config, FormationBlockId);
        ChunkData data = CreateChunkWithCavity(5, 40);

        // Act
        pipeline.Generate(data, 0, 0, 42);

        // Assert — at least some formations
        int formationCount = 0;

        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                for (int y = 5; y < 40; y++)
                {
                    if (data.GetBlock(x, y, z) == FormationBlockId)
                    {
                        formationCount++;
                    }
                }
            }
        }

        formationCount.Should().BeGreaterThan(0, "pipeline should generate some formations");
    }
}
