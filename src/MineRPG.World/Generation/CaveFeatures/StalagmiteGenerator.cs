using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.CaveFeatures;

/// <summary>
/// Generates stalagmites growing upward from cave floors.
/// Stalagmites grow from solid blocks with air above.
/// </summary>
public sealed class StalagmiteGenerator
{
    private readonly CaveFeatureConfig _config;
    private readonly ushort _formationBlockId;

    /// <summary>
    /// Creates a stalagmite generator.
    /// </summary>
    /// <param name="config">Cave feature configuration.</param>
    /// <param name="formationBlockId">Block ID for stalagmite material.</param>
    public StalagmiteGenerator(CaveFeatureConfig config, ushort formationBlockId)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _formationBlockId = formationBlockId;
    }

    /// <summary>
    /// Scans the chunk for cave floors and generates stalagmites.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="random">Seeded random for placement decisions.</param>
    public void Generate(ChunkData data, Random random)
    {
        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                for (int y = 1; y < ChunkData.SizeY - 2; y++)
                {
                    // Look for solid block with air above (floor)
                    if (data.GetBlock(localX, y, localZ) == 0)
                    {
                        continue;
                    }

                    if (data.GetBlock(localX, y + 1, localZ) != 0)
                    {
                        continue;
                    }

                    if (random.NextDouble() > _config.StalagmiteChance)
                    {
                        continue;
                    }

                    // Determine height
                    int maxHeight = Math.Min(
                        _config.StalagmiteMaxHeight,
                        random.Next(2, _config.StalagmiteMaxHeight + 1));

                    // Place stalagmite growing upward
                    for (int height = 1; height <= maxHeight; height++)
                    {
                        int stalY = y + height;

                        if (stalY >= ChunkData.SizeY - 1)
                        {
                            break;
                        }

                        if (data.GetBlock(localX, stalY, localZ) != 0)
                        {
                            break;
                        }

                        data.SetBlock(localX, stalY, localZ, _formationBlockId);
                    }
                }
            }
        }
    }
}
