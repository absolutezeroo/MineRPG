using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.CaveFeatures;

/// <summary>
/// Generates stalactites hanging from cave ceilings.
/// Stalactites grow downward from solid blocks with air below.
/// </summary>
public sealed class StalactiteGenerator
{
    private readonly CaveFeatureConfig _config;
    private readonly ushort _formationBlockId;

    /// <summary>
    /// Creates a stalactite generator.
    /// </summary>
    /// <param name="config">Cave feature configuration.</param>
    /// <param name="formationBlockId">Block ID for stalactite material.</param>
    public StalactiteGenerator(CaveFeatureConfig config, ushort formationBlockId)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _formationBlockId = formationBlockId;
    }

    /// <summary>
    /// Scans the chunk for cave ceilings and generates stalactites.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="random">Seeded random for placement decisions.</param>
    public void Generate(ChunkData data, Random random)
    {
        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                for (int y = ChunkData.SizeY - 2; y > 1; y--)
                {
                    // Look for solid block with air below (ceiling)
                    if (data.GetBlock(localX, y, localZ) == 0)
                    {
                        continue;
                    }

                    if (data.GetBlock(localX, y - 1, localZ) != 0)
                    {
                        continue;
                    }

                    if (random.NextDouble() > _config.StalactiteChance)
                    {
                        continue;
                    }

                    // Determine length
                    int maxLength = Math.Min(
                        _config.StalactiteMaxLength,
                        random.Next(2, _config.StalactiteMaxLength + 1));

                    // Place stalactite growing downward
                    for (int length = 1; length <= maxLength; length++)
                    {
                        int stalY = y - length;

                        if (stalY < 1)
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
