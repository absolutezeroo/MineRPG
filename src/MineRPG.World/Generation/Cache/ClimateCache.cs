using System;

using MineRPG.World.Biomes.Climate;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Cache;

/// <summary>
/// Pre-computes and caches climate parameters for all columns in a chunk.
/// Avoids redundant noise evaluations when multiple systems need climate data.
/// Stores a 16x16 grid of <see cref="ClimateParameters"/> per chunk.
/// </summary>
public sealed class ClimateCache
{
    private const int ColumnCount = ChunkData.SizeX * ChunkData.SizeZ;

    private readonly ClimateParameters[] _climateData;
    private readonly int _chunkWorldX;
    private readonly int _chunkWorldZ;

    /// <summary>
    /// Creates a climate cache by pre-sampling all columns for the chunk.
    /// </summary>
    /// <param name="climateSampler">Climate sampler to query.</param>
    /// <param name="chunkWorldX">World X of the chunk origin.</param>
    /// <param name="chunkWorldZ">World Z of the chunk origin.</param>
    public ClimateCache(ClimateSampler climateSampler, int chunkWorldX, int chunkWorldZ)
    {
        if (climateSampler == null)
        {
            throw new ArgumentNullException(nameof(climateSampler));
        }

        _chunkWorldX = chunkWorldX;
        _chunkWorldZ = chunkWorldZ;
        _climateData = new ClimateParameters[ColumnCount];

        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            int worldX = chunkWorldX + localX;

            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                int worldZ = chunkWorldZ + localZ;
                int index = localX + localZ * ChunkData.SizeX;
                _climateData[index] = climateSampler.SampleSurface(worldX, worldZ);
            }
        }
    }

    /// <summary>
    /// Gets the cached climate parameters at a local chunk position.
    /// </summary>
    /// <param name="localX">Local X in [0, 16).</param>
    /// <param name="localZ">Local Z in [0, 16).</param>
    /// <returns>The cached climate parameters.</returns>
    public ClimateParameters Get(int localX, int localZ) => _climateData[localX + localZ * ChunkData.SizeX];
}
