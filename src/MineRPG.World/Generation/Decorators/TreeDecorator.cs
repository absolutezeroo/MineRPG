using System;

using MineRPG.World.Biomes;
using MineRPG.World.Chunks;
using MineRPG.World.Generation.Decorators.Trees;

namespace MineRPG.World.Generation.Decorators;

/// <summary>
/// Places trees based on biome vegetation entries and density noise.
/// Verifies solid ground and free space above before placement.
/// </summary>
public sealed class TreeDecorator : IDecorator
{
    private const int MinTreeClearance = 4;

    private readonly TreeRegistry _treeRegistry;
    private readonly ushort _airBlockId;

    /// <summary>
    /// Creates a tree decorator.
    /// </summary>
    /// <param name="treeRegistry">Registry of available tree generators.</param>
    /// <param name="airBlockId">Block ID for air (typically 0).</param>
    public TreeDecorator(TreeRegistry treeRegistry, ushort airBlockId = 0)
    {
        _treeRegistry = treeRegistry ?? throw new ArgumentNullException(nameof(treeRegistry));
        _airBlockId = airBlockId;
    }

    /// <inheritdoc />
    public void Decorate(
        ChunkData data,
        BiomeDefinition[] biomeMap,
        int[] heightMap,
        int chunkWorldX,
        int chunkWorldZ,
        Random random)
    {
        for (int localX = 0; localX < ChunkData.SizeX; localX++)
        {
            for (int localZ = 0; localZ < ChunkData.SizeZ; localZ++)
            {
                int columnIndex = localX + localZ * ChunkData.SizeX;
                BiomeDefinition biome = biomeMap[columnIndex];
                int surfaceY = heightMap[columnIndex];

                if (biome.Vegetation == null)
                {
                    continue;
                }

                for (int v = 0; v < biome.Vegetation.Count; v++)
                {
                    VegetationEntry entry = biome.Vegetation[v];

                    if (!_treeRegistry.TryGet(entry.Type, out ITreeGenerator? generator) || generator == null)
                    {
                        continue;
                    }

                    // Density check — probability per column
                    if (random.NextDouble() > entry.Density)
                    {
                        continue;
                    }

                    // Verify clearance above surface
                    int treeBaseY = surfaceY + 1;

                    if (treeBaseY + MinTreeClearance >= ChunkData.SizeY)
                    {
                        continue;
                    }

                    bool hasClearance = true;

                    for (int checkY = treeBaseY; checkY < treeBaseY + MinTreeClearance; checkY++)
                    {
                        if (ChunkData.IsInBounds(localX, checkY, localZ)
                            && data.GetBlock(localX, checkY, localZ) != _airBlockId)
                        {
                            hasClearance = false;
                            break;
                        }
                    }

                    if (hasClearance)
                    {
                        generator.Generate(data, localX, treeBaseY, localZ, random);
                    }
                }
            }
        }
    }
}
