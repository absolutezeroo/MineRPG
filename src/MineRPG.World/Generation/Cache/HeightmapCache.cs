using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Cache;

/// <summary>
/// Caches terrain column data (surface height, biome, blend) for all columns in a chunk.
/// Prevents redundant noise sampling during multi-pass generation.
/// </summary>
public sealed class HeightmapCache
{
    private const int ColumnCount = ChunkData.SizeX * ChunkData.SizeZ;

    private readonly TerrainColumn[] _columns;

    /// <summary>
    /// Creates a heightmap cache by pre-sampling all columns for the chunk.
    /// </summary>
    /// <param name="terrainSampler">Terrain sampler to query.</param>
    /// <param name="chunkWorldX">World X of the chunk origin.</param>
    /// <param name="chunkWorldZ">World Z of the chunk origin.</param>
    public HeightmapCache(TerrainSampler terrainSampler, int chunkWorldX, int chunkWorldZ)
    {
        _columns = new TerrainColumn[ColumnCount];

        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            int worldX = chunkWorldX + localX;

            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                int worldZ = chunkWorldZ + localZ;
                int index = localX + localZ * ChunkData.SizeX;
                _columns[index] = terrainSampler.SampleColumn(worldX, worldZ);
            }
        }
    }

    /// <summary>
    /// Gets the cached terrain column at a local position.
    /// </summary>
    /// <param name="localX">Local X in [0, 16).</param>
    /// <param name="localZ">Local Z in [0, 16).</param>
    /// <returns>The cached terrain column.</returns>
    public TerrainColumn Get(int localX, int localZ) => _columns[localX + localZ * ChunkData.SizeX];

    /// <summary>
    /// Gets the surface heights as a flat array for decorator use.
    /// </summary>
    /// <returns>Array of 256 surface heights indexed by localX + localZ * 16.</returns>
    public int[] GetHeightArray()
    {
        int[] heights = new int[ColumnCount];

        for (int i = 0; i < ColumnCount; i++)
        {
            heights[i] = _columns[i].SurfaceY;
        }

        return heights;
    }

    /// <summary>
    /// Gets the primary biome definitions as a flat array for decorator use.
    /// </summary>
    /// <returns>Array of 256 biome definitions indexed by localX + localZ * 16.</returns>
    public BiomeDefinition[] GetBiomeArray()
    {
        BiomeDefinition[] biomes = new BiomeDefinition[ColumnCount];

        for (int i = 0; i < ColumnCount; i++)
        {
            biomes[i] = _columns[i].PrimaryBiome;
        }

        return biomes;
    }
}
