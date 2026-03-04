using FluentAssertions;
using MineRPG.Core.Logging;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.Tests.World;

public sealed class WorldGeneratorTests
{
    private readonly WorldGenerator _generator;

    public WorldGeneratorTests()
    {
        var logger = NullLogger.Instance;
        var loader = new Core.DataLoading.JsonDataLoader(logger, FindDataRoot());
        var blockRegistry = new BlockRegistry(loader, logger);

        var biomes = loader.LoadAll<BiomeDefinition>("Biomes");
        BiomeBlockResolver.ResolveAll(biomes, blockRegistry, logger);

        const int seed = 42;
        var biomeSelector = new BiomeSelector(biomes, seed);
        var terrainSampler = new TerrainSampler(biomeSelector, seed);
        var caveCarver = new CaveCarver(terrainSampler);

        _generator = new WorldGenerator(blockRegistry, terrainSampler, caveCarver);
    }

    [Fact]
    public void Generate_ProducesNonEmptyChunk()
    {
        // Arrange
        var entry = new ChunkEntry(new Core.Math.ChunkCoord(0, 0));

        // Act
        _generator.Generate(entry, CancellationToken.None);

        // Assert — chunk should contain at least some solid blocks
        var solidCount = 0;
        for (var y = 0; y < ChunkData.SizeY; y++)
        for (var x = 0; x < ChunkData.SizeX; x++)
        for (var z = 0; z < ChunkData.SizeZ; z++)
        {
            if (entry.Data.GetBlock(x, y, z) != 0)
                solidCount++;
        }

        solidCount.Should().BeGreaterThan(0, "generated chunk should contain terrain blocks");
    }

    [Fact]
    public void Generate_BedrockAtY0()
    {
        // Arrange
        var entry = new ChunkEntry(new Core.Math.ChunkCoord(0, 0));

        // Act
        _generator.Generate(entry, CancellationToken.None);

        // Assert — y=0 should be bedrock (ID 8) everywhere
        for (var x = 0; x < ChunkData.SizeX; x++)
        for (var z = 0; z < ChunkData.SizeZ; z++)
        {
            var blockId = entry.Data.GetBlock(x, 0, z);
            blockId.Should().Be(8, $"bedrock expected at ({x}, 0, {z})");
        }
    }

    [Fact]
    public void Generate_IsDeterministic()
    {
        // Arrange
        var coord = new Core.Math.ChunkCoord(5, -3);
        var entry1 = new ChunkEntry(coord);
        var entry2 = new ChunkEntry(coord);

        // Act
        _generator.Generate(entry1, CancellationToken.None);
        _generator.Generate(entry2, CancellationToken.None);

        // Assert
        var span1 = entry1.Data.GetRawSpan();
        var span2 = entry2.Data.GetRawSpan();
        for (var i = 0; i < span1.Length; i++)
        {
            span1[i].Should().Be(span2[i],
                $"block at index {i} should be identical for same seed and coord");
        }
    }

    [Fact]
    public void Generate_RespectsCanellationToken()
    {
        // Arrange
        var entry = new ChunkEntry(new Core.Math.ChunkCoord(0, 0));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act — should not throw, but may produce incomplete data
        var act = () => _generator.Generate(entry, cts.Token);

        // Assert — method should exit gracefully
        act.Should().NotThrow();
    }

    private static string FindDataRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "Data");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data");
    }
}
