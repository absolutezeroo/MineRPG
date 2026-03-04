using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using FluentAssertions;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class WorldGeneratorTests
{
    private readonly WorldGenerator _generator;

    public WorldGeneratorTests()
    {
        ILogger logger = NullLogger.Instance;
        JsonDataLoader loader = new JsonDataLoader(logger, FindDataRoot());
        BlockRegistry blockRegistry = new BlockRegistry(loader, logger);

        IReadOnlyList<BiomeDefinition> biomes = loader.LoadAll<BiomeDefinition>("Biomes");
        BiomeBlockResolver.ResolveAll(biomes, blockRegistry, logger);

        const int seed = 42;
        BiomeSelector biomeSelector = new BiomeSelector(biomes, seed);
        TerrainSampler terrainSampler = new TerrainSampler(biomeSelector, seed);
        CaveCarver caveCarver = new CaveCarver(terrainSampler);

        _generator = new WorldGenerator(blockRegistry, terrainSampler, caveCarver);
    }

    [Fact]
    public void Generate_ProducesNonEmptyChunk()
    {
        // Arrange
        ChunkEntry entry = new ChunkEntry(new ChunkCoord(0, 0));

        // Act
        _generator.Generate(entry, CancellationToken.None);

        // Assert — chunk should contain at least some solid blocks
        int solidCount = 0;
        for (int y = 0; y < ChunkData.SizeY; y++)
        {
            for (int x = 0; x < ChunkData.SizeX; x++)
            {
                for (int z = 0; z < ChunkData.SizeZ; z++)
                {
                    if (entry.Data.GetBlock(x, y, z) != 0)
                    {
                        solidCount++;
                    }
                }
            }
        }

        solidCount.Should().BeGreaterThan(0, "generated chunk should contain terrain blocks");
    }

    [Fact]
    public void Generate_BedrockAtY0()
    {
        // Arrange
        ChunkEntry entry = new ChunkEntry(new ChunkCoord(0, 0));

        // Act
        _generator.Generate(entry, CancellationToken.None);

        // Assert — y=0 should be bedrock (ID 8) everywhere
        for (int x = 0; x < ChunkData.SizeX; x++)
        {
            for (int z = 0; z < ChunkData.SizeZ; z++)
            {
                ushort blockId = entry.Data.GetBlock(x, 0, z);
                blockId.Should().Be(8, $"bedrock expected at ({x}, 0, {z})");
            }
        }
    }

    [Fact]
    public void Generate_IsDeterministic()
    {
        // Arrange
        ChunkCoord coord = new ChunkCoord(5, -3);
        ChunkEntry entry1 = new ChunkEntry(coord);
        ChunkEntry entry2 = new ChunkEntry(coord);

        // Act
        _generator.Generate(entry1, CancellationToken.None);
        _generator.Generate(entry2, CancellationToken.None);

        // Assert
        ReadOnlySpan<ushort> span1 = entry1.Data.GetRawSpan();
        ReadOnlySpan<ushort> span2 = entry2.Data.GetRawSpan();
        for (int i = 0; i < span1.Length; i++)
        {
            span1[i].Should().Be(span2[i],
                $"block at index {i} should be identical for same seed and coord");
        }
    }

    [Fact]
    public void Generate_RespectsCanellationToken()
    {
        // Arrange
        ChunkEntry entry = new ChunkEntry(new ChunkCoord(0, 0));
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        // Act — should not throw, but may produce incomplete data
        Action act = () => _generator.Generate(entry, cts.Token);

        // Assert — method should exit gracefully
        act.Should().NotThrow();
    }

    private static string FindDataRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "Data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data");
    }
}
