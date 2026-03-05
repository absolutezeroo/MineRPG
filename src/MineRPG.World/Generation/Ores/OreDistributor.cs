using System;
using System.Collections.Generic;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Ores;

/// <summary>
/// Places ore veins throughout a chunk using triangular height distribution.
/// Each ore type has configurable height range, peak, vein size, and frequency.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class OreDistributor
{
    private readonly IReadOnlyList<OreDefinition> _oreDefinitions;
    private readonly ushort _stoneBlockId;

    /// <summary>
    /// Creates an ore distributor with the given definitions.
    /// </summary>
    /// <param name="oreDefinitions">Ore definitions to distribute.</param>
    /// <param name="stoneBlockId">Block ID for stone (replaceable by ore).</param>
    public OreDistributor(IReadOnlyList<OreDefinition> oreDefinitions, ushort stoneBlockId)
    {
        _oreDefinitions = oreDefinitions ?? throw new ArgumentNullException(nameof(oreDefinitions));
        _stoneBlockId = stoneBlockId;
    }

    /// <summary>
    /// Distributes all ore types within the given chunk.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="chunkWorldX">World X of the chunk origin.</param>
    /// <param name="chunkWorldZ">World Z of the chunk origin.</param>
    /// <param name="random">Seeded random for placement.</param>
    public void Distribute(ChunkData data, int chunkWorldX, int chunkWorldZ, Random random)
    {
        for (int i = 0; i < _oreDefinitions.Count; i++)
        {
            OreDefinition ore = _oreDefinitions[i];

            if (ore.BlockId == 0)
            {
                continue;
            }

            for (int attempt = 0; attempt < ore.Frequency; attempt++)
            {
                int localX = random.Next(ChunkData.SizeX);
                int localZ = random.Next(ChunkData.SizeZ);
                int y = SampleHeight(ore, random);

                if (y < 0 || y >= ChunkData.SizeY)
                {
                    continue;
                }

                VeinGenerator.Generate(
                    data, localX, y, localZ,
                    ore.BlockId, ore.VeinSize, _stoneBlockId, random);
            }
        }
    }

    private static int SampleHeight(OreDefinition ore, Random random)
    {
        int minY = ore.MinHeight;
        int maxY = ore.MaxHeight;

        if (minY >= maxY)
        {
            return minY;
        }

        switch (ore.Distribution)
        {
            case OreDistribution.Uniform:
                return random.Next(minY, maxY + 1);

            case OreDistribution.Triangle:
                return SampleTriangular(random, minY, maxY, ore.PeakHeight);

            case OreDistribution.InvertedTriangle:
            {
                // Inverted: reflect triangular sample around midpoint of [minY, maxY]
                // so high probability at edges, low at center
                int sample = SampleTriangular(random, minY, maxY, ore.PeakHeight);
                return minY + maxY - sample;
            }

            default:
                return random.Next(minY, maxY + 1);
        }
    }

    /// <summary>
    /// Samples from a triangular distribution peaked at the given mode.
    /// </summary>
    private static int SampleTriangular(Random random, int min, int max, int peak)
    {
        float u = (float)random.NextDouble();
        float range = max - min;

        if (range <= 0f)
        {
            return min;
        }

        float peakNormalized = (float)(peak - min) / range;

        float sample;

        if (u < peakNormalized)
        {
            sample = min + MathF.Sqrt(u * range * (peak - min));
        }
        else
        {
            sample = max - MathF.Sqrt((1f - u) * range * (max - peak));
        }

        return Math.Clamp((int)sample, min, max);
    }
}
