using System.Runtime.CompilerServices;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Generates ChunkData using multi-layer noise terrain and advanced caves.
/// Thread-safe: all inputs are readonly, all state is stack/local.
/// Called from Task.Run — never blocks the main thread.
/// </summary>
public sealed class WorldGenerator : IWorldGenerator
{
    private const int SeaLevel = 62;

    private readonly BlockRegistry _blockRegistry;
    private readonly TerrainSampler _terrainSampler;
    private readonly CaveCarver _caveCarver;
    private readonly ushort _waterBlockId;
    private readonly ushort _sandBlockId;
    private readonly ushort _bedrockBlockId;

    public WorldGenerator(BlockRegistry blockRegistry, TerrainSampler terrainSampler, CaveCarver caveCarver)
    {
        _blockRegistry = blockRegistry ?? throw new ArgumentNullException(nameof(blockRegistry));
        _terrainSampler = terrainSampler ?? throw new ArgumentNullException(nameof(terrainSampler));
        _caveCarver = caveCarver ?? throw new ArgumentNullException(nameof(caveCarver));

        _waterBlockId = _blockRegistry.TryGetByName("Water", out var waterDef) ? waterDef.Id : (ushort)0;
        _sandBlockId = _blockRegistry.TryGetByName("Sand", out var sandDef) ? sandDef.Id : (ushort)0;
        _bedrockBlockId = _blockRegistry.TryGetByName("Bedrock", out var bedrockDef) ? bedrockDef.Id : (ushort)1;
    }

    public void Generate(ChunkEntry entry, CancellationToken cancellationToken)
    {
        var data = entry.Data;
        var coord = entry.Coord;

        for (var localX = 0; localX < ChunkData.SizeX; localX++)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var worldX = coord.X * ChunkData.SizeX + localX;

            for (var localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                var worldZ = coord.Z * ChunkData.SizeZ + localZ;

                var column = _terrainSampler.SampleColumn(worldX, worldZ);

                for (var y = 0; y < ChunkData.SizeY; y++)
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
        var surfaceY = column.SurfaceY;

        // Bedrock layer
        if (y == 0)
            return _bedrockBlockId;

        // Above surface: air or water
        if (y > surfaceY)
            return y <= SeaLevel && _waterBlockId != 0 ? _waterBlockId : (ushort)0;

        // Surface and near-surface
        if (y >= surfaceY - 1)
            return SelectSurfaceBlock(y, in column);

        // Sub-surface layer (dirt/sand)
        if (y > surfaceY - column.SubSurfaceDepth)
            return BlendBlock(column.PrimaryBiome.SubSurfaceBlock,
                column.SecondaryBiome.SubSurfaceBlock, column.BlendWeight);

        // Underground: check cave carving
        if (_caveCarver.ShouldCarve(worldX, y, worldZ, surfaceY, column.Continentalness))
            return 0;

        return column.PrimaryBiome.StoneBlock;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort SelectSurfaceBlock(int y, in TerrainColumn column)
    {
        if (y <= SeaLevel + 1 && _sandBlockId != 0)
            return _sandBlockId;

        return BlendBlock(column.PrimaryBiome.SurfaceBlock,
            column.SecondaryBiome.SurfaceBlock, column.BlendWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort BlendBlock(ushort primaryBlock, ushort secondaryBlock, float blendWeight)
        => blendWeight < 0.5f ? primaryBlock : secondaryBlock;
}
