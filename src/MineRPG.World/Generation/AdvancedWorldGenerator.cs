using System;
using System.Runtime.CompilerServices;
using System.Threading;

using MineRPG.Core.Math;
using MineRPG.World.Biomes.Climate;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Generation.Aquifers;
using MineRPG.World.Generation.Cache;
using MineRPG.World.Generation.CaveFeatures;
using MineRPG.World.Generation.Decorators;
using MineRPG.World.Generation.Ores;
using MineRPG.World.Generation.Surface;

namespace MineRPG.World.Generation;

/// <summary>
/// Advanced world generator integrating all complement systems:
/// heightmap cache, surface rules, aquifers, cave features, ore distribution,
/// and decorators. Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class AdvancedWorldGenerator : IWorldGenerator
{
    private const int SeaLevel = 62;
    private const int BedrockY = 0;
    private const float PatternNoiseFrequency = 0.05f;

    private readonly TerrainSampler _terrainSampler;
    private readonly CaveCarver _caveCarver;
    private readonly SurfaceRuleChain _surfaceRules;
    private readonly IAquiferSampler _aquiferSampler;
    private readonly CaveFeaturePipeline _caveFeatures;
    private readonly OreDistributor _oreDistributor;
    private readonly DecoratorPipeline _decorators;
    private readonly EnhancedRiverCarver _riverCarver;
    private readonly ushort _waterBlockId;
    private readonly ushort _bedrockBlockId;
    private readonly ushort _defaultStoneId;
    private readonly int _worldSeed;
    private readonly FastNoise _patternNoise;

    /// <summary>
    /// Creates an advanced world generator with all complement systems.
    /// </summary>
    /// <param name="terrainSampler">Terrain sampler for height and biome data.</param>
    /// <param name="caveCarver">Cave carver for underground cavities.</param>
    /// <param name="surfaceRules">Surface rule chain for block selection.</param>
    /// <param name="aquiferSampler">Aquifer sampler for underground fluids.</param>
    /// <param name="caveFeatures">Cave feature pipeline for formations.</param>
    /// <param name="oreDistributor">Ore distributor for mineral veins.</param>
    /// <param name="decorators">Decorator pipeline for trees and vegetation.</param>
    /// <param name="riverCarver">Enhanced river carver.</param>
    /// <param name="waterBlockId">Block ID for water.</param>
    /// <param name="bedrockBlockId">Block ID for bedrock.</param>
    /// <param name="defaultStoneId">Block ID for default stone.</param>
    /// <param name="worldSeed">World seed for deterministic generation.</param>
    public AdvancedWorldGenerator(
        TerrainSampler terrainSampler,
        CaveCarver caveCarver,
        SurfaceRuleChain surfaceRules,
        IAquiferSampler aquiferSampler,
        CaveFeaturePipeline caveFeatures,
        OreDistributor oreDistributor,
        DecoratorPipeline decorators,
        EnhancedRiverCarver riverCarver,
        ushort waterBlockId,
        ushort bedrockBlockId,
        ushort defaultStoneId,
        int worldSeed)
    {
        _terrainSampler = terrainSampler ?? throw new ArgumentNullException(nameof(terrainSampler));
        _caveCarver = caveCarver ?? throw new ArgumentNullException(nameof(caveCarver));
        _surfaceRules = surfaceRules ?? throw new ArgumentNullException(nameof(surfaceRules));
        _aquiferSampler = aquiferSampler ?? throw new ArgumentNullException(nameof(aquiferSampler));
        _caveFeatures = caveFeatures ?? throw new ArgumentNullException(nameof(caveFeatures));
        _oreDistributor = oreDistributor ?? throw new ArgumentNullException(nameof(oreDistributor));
        _decorators = decorators ?? throw new ArgumentNullException(nameof(decorators));
        _riverCarver = riverCarver ?? throw new ArgumentNullException(nameof(riverCarver));
        _waterBlockId = waterBlockId;
        _bedrockBlockId = bedrockBlockId;
        _defaultStoneId = defaultStoneId;
        _worldSeed = worldSeed;
        _patternNoise = new FastNoise(worldSeed ^ 0x12345678);
    }

    /// <inheritdoc />
    public void Generate(ChunkEntry entry, CancellationToken cancellationToken)
    {
        ChunkData data = entry.Data;
        ChunkCoord coord = entry.Coord;
        int chunkWorldX = coord.X * ChunkData.SizeX;
        int chunkWorldZ = coord.Z * ChunkData.SizeZ;

        // Phase 1: Cache heightmap and terrain columns
        HeightmapCache heightCache = new HeightmapCache(
            _terrainSampler, chunkWorldX, chunkWorldZ);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Phase 2: Base terrain pass (solid blocks, caves, surface rules)
        GenerateBaseTerrain(data, heightCache, chunkWorldX, chunkWorldZ, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Phase 3: Rivers
        ApplyRivers(data, heightCache, chunkWorldX, chunkWorldZ);

        // Phase 4: Ore distribution
        _oreDistributor.Distribute(data, chunkWorldX, chunkWorldZ,
            new Random(HashCode.Combine(_worldSeed, chunkWorldX, chunkWorldZ)));

        // Phase 5: Cave features (pillars, stalactites, stalagmites)
        _caveFeatures.Generate(data, chunkWorldX, chunkWorldZ, _worldSeed);

        // Phase 6: Decorators (trees, vegetation)
        BiomeDefinition[] biomeMap = heightCache.GetBiomeArray();
        int[] heightMap = heightCache.GetHeightArray();
        _decorators.DecorateChunk(data, biomeMap, heightMap,
            chunkWorldX, chunkWorldZ, _worldSeed);
    }

    private void GenerateBaseTerrain(
        ChunkData data,
        HeightmapCache heightCache,
        int chunkWorldX,
        int chunkWorldZ,
        CancellationToken cancellationToken)
    {
        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            int worldX = chunkWorldX + localX;

            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                int worldZ = chunkWorldZ + localZ;
                TerrainColumn column = heightCache.Get(localX, localZ);
                int surfaceY = column.SurfaceY;

                float patternNoise = _patternNoise.Sample2D(
                    worldX * PatternNoiseFrequency,
                    worldZ * PatternNoiseFrequency);

                for (int y = 0; y < ChunkData.SizeY; y++)
                {
                    ushort blockId = DetermineBlock(
                        worldX, y, worldZ, in column, surfaceY,
                        patternNoise);
                    data.SetBlock(localX, y, localZ, blockId);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort DetermineBlock(
        int worldX,
        int y,
        int worldZ,
        in TerrainColumn column,
        int surfaceY,
        float patternNoise)
    {
        // Bedrock
        if (y == BedrockY)
        {
            return _bedrockBlockId;
        }

        // Above surface: air or water
        if (y > surfaceY)
        {
            return y <= SeaLevel && _waterBlockId != 0 ? _waterBlockId : (ushort)0;
        }

        // Check cave carving underground
        if (y < surfaceY - 1
            && _caveCarver.ShouldCarve(worldX, y, worldZ, surfaceY, column.Continentalness))
        {
            // Carved cave - check aquifer for fluid
            ushort fluidBlock = _aquiferSampler.GetFluidBlock(worldX, y, worldZ, surfaceY);

            if (fluidBlock != 0)
            {
                return fluidBlock;
            }

            return 0;
        }

        // Surface rules determine the block
        SurfaceContext context = new SurfaceContext
        {
            WorldX = worldX,
            WorldY = y,
            WorldZ = worldZ,
            SurfaceY = surfaceY,
            IsSurface = y == surfaceY,
            IsCeiling = false,
            DepthBelowSurface = surfaceY - y,
            SlopeGradient = 0,
            Biome = column.PrimaryBiome,
            Climate = column.Climate,
            SeaLevel = SeaLevel,
            IsUnderwater = surfaceY <= SeaLevel,
            PatternNoise = patternNoise,
        };

        return _surfaceRules.Evaluate(in context);
    }

    private void ApplyRivers(
        ChunkData data,
        HeightmapCache heightCache,
        int chunkWorldX,
        int chunkWorldZ)
    {
        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            int worldX = chunkWorldX + localX;

            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                int worldZ = chunkWorldZ + localZ;
                TerrainColumn column = heightCache.Get(localX, localZ);

                _riverCarver.CarveColumn(
                    data, localX, localZ,
                    worldX, worldZ,
                    column.SurfaceY,
                    column.Climate.PeaksAndValleys,
                    column.Climate.Temperature);
            }
        }
    }
}
