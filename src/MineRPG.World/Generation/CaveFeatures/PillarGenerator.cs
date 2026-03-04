using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.CaveFeatures;

/// <summary>
/// Generates stone pillars in large cave cavities (floor to ceiling).
/// Only creates pillars where the vertical air gap exceeds the configured minimum.
/// </summary>
public sealed class PillarGenerator
{
    private readonly CaveFeatureConfig _config;
    private readonly ushort _formationBlockId;

    /// <summary>
    /// Creates a pillar generator.
    /// </summary>
    /// <param name="config">Cave feature configuration.</param>
    /// <param name="formationBlockId">Block ID for pillar material.</param>
    public PillarGenerator(CaveFeatureConfig config, ushort formationBlockId)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _formationBlockId = formationBlockId;
    }

    /// <summary>
    /// Scans the chunk for large cavities and generates pillars.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="random">Seeded random for placement decisions.</param>
    public void Generate(ChunkData data, Random random)
    {
        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                if (random.NextDouble() > _config.PillarChance)
                {
                    continue;
                }

                TryPlacePillar(data, localX, localZ, random);
            }
        }
    }

    private void TryPlacePillar(ChunkData data, int localX, int localZ, Random random)
    {
        // Scan for large air gaps underground
        for (int y = 1; y < ChunkData.SizeY - 1; y++)
        {
            if (data.GetBlock(localX, y, localZ) != 0)
            {
                continue;
            }

            // Found air — find the ceiling
            int floorY = y;
            int ceilingY = y;

            while (ceilingY < ChunkData.SizeY - 1 && data.GetBlock(localX, ceilingY, localZ) == 0)
            {
                ceilingY++;
            }

            int gapHeight = ceilingY - floorY;

            if (gapHeight < _config.PillarMinHeight)
            {
                // Skip past this gap
                y = ceilingY;
                continue;
            }

            // Place a pillar from floor to ceiling
            int width = _config.PillarWidth;
            int halfWidth = width / 2;

            for (int pillarY = floorY; pillarY < ceilingY; pillarY++)
            {
                for (int dx = -halfWidth; dx <= halfWidth; dx++)
                {
                    for (int dz = -halfWidth; dz <= halfWidth; dz++)
                    {
                        int px = localX + dx;
                        int pz = localZ + dz;

                        if (ChunkData.IsInBounds(px, pillarY, pz)
                            && data.GetBlock(px, pillarY, pz) == 0)
                        {
                            data.SetBlock(px, pillarY, pz, _formationBlockId);
                        }
                    }
                }
            }

            // Only one pillar per column scan
            return;
        }
    }
}
