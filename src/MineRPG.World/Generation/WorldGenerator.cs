using System;
using System.Runtime.CompilerServices;
using System.Threading;

using MineRPG.Core.Math;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Generates ChunkData using multi-layer noise terrain and advanced caves.
/// Thread-safe: all inputs are readonly, all state is stack/local.
/// Called from Task.Run -- never blocks the main thread.
/// </summary>
public sealed class WorldGenerator : IWorldGenerator
{
    private const int SeaLevel = 62;
    private const int BedrockY = 0;
    private const int BeachMaxY = SeaLevel + 1;
    private const float BlendThreshold = 0.5f;

    private readonly BlockRegistry _blockRegistry;
    private readonly TerrainSampler _terrainSampler;
    private readonly CaveCarver _caveCarver;
    private readonly ushort _waterBlockId;
    private readonly ushort _sandBlockId;
    private readonly ushort _bedrockBlockId;

    /// <summary>
    /// Creates a world generator with the given dependencies.
    /// </summary>
    /// <param name="blockRegistry">Block registry for block lookups.</param>
    /// <param name="terrainSampler">Terrain sampler for noise-based height computation.</param>
    /// <param name="caveCarver">Cave carver for underground cavities.</param>
    public WorldGenerator(BlockRegistry blockRegistry, TerrainSampler terrainSampler, CaveCarver caveCarver)
    {
        _blockRegistry = blockRegistry ?? throw new ArgumentNullException(nameof(blockRegistry));
        _terrainSampler = terrainSampler ?? throw new ArgumentNullException(nameof(terrainSampler));
        _caveCarver = caveCarver ?? throw new ArgumentNullException(nameof(caveCarver));

        _waterBlockId = _blockRegistry.TryGet("minerpg:water", out BlockDefinition? waterDefinition)
            ? waterDefinition.RuntimeId : (ushort)0;
        _sandBlockId = _blockRegistry.TryGet("minerpg:sand", out BlockDefinition? sandDefinition)
            ? sandDefinition.RuntimeId : (ushort)0;
        _bedrockBlockId = _blockRegistry.TryGet("minerpg:bedrock", out BlockDefinition? bedrockDefinition)
            ? bedrockDefinition.RuntimeId : (ushort)0;
    }

    /// <summary>
    /// Generates terrain blocks for the given chunk entry.
    /// </summary>
    /// <param name="entry">The chunk entry to populate.</param>
    /// <param name="cancellationToken">Token to cancel generation.</param>
    public void Generate(ChunkEntry entry, CancellationToken cancellationToken)
    {
        ChunkData data = entry.Data;
        ChunkCoord coord = entry.Coord;

        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            int worldX = coord.X * ChunkData.SizeX + localX;

            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                int worldZ = coord.Z * ChunkData.SizeZ + localZ;
                TerrainColumn column = _terrainSampler.SampleColumn(worldX, worldZ);

                for (int y = 0; y < ChunkData.SizeY; y++)
                {
                    data.SetBlock(localX, y, localZ,
                        DetermineBlock(worldX, y, worldZ, in column));
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort DetermineBlock(int worldX, int y, int worldZ, in TerrainColumn column)
    {
        int surfaceY = column.SurfaceY;

        // Bedrock layer
        if (y == BedrockY)
        {
            return _bedrockBlockId;
        }

        // Above surface: air or water
        if (y > surfaceY)
        {
            return y <= SeaLevel && _waterBlockId != 0 ? _waterBlockId : (ushort)0;
        }

        // Surface and near-surface
        if (y >= surfaceY - 1)
        {
            return SelectSurfaceBlock(y, in column);
        }

        // Sub-surface layer (dirt/sand)
        if (y > surfaceY - column.SubSurfaceDepth)
        {
            return BlendBlock(column.PrimaryBiome.SubSurfaceBlock,
                column.SecondaryBiome.SubSurfaceBlock, column.BlendWeight);
        }

        // Underground: check cave carving
        if (_caveCarver.ShouldCarve(worldX, y, worldZ, surfaceY, column.Continentalness))
        {
            return 0;
        }

        return column.PrimaryBiome.StoneBlock;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort SelectSurfaceBlock(int y, in TerrainColumn column)
    {
        // Only apply beach sand near sea level for non-desert/non-snow biomes
        // where water is adjacent (surface at or below sea level)
        if (y <= BeachMaxY && _sandBlockId != 0 && column.SurfaceY <= SeaLevel)
        {
            return _sandBlockId;
        }

        return BlendBlock(column.PrimaryBiome.SurfaceBlock,
            column.SecondaryBiome.SurfaceBlock, column.BlendWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort BlendBlock(ushort primaryBlock, ushort secondaryBlock, float blendWeight)
        => blendWeight < BlendThreshold ? primaryBlock : secondaryBlock;
}
