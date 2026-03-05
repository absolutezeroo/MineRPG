using System;

using MineRPG.World.Biomes;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators;

/// <summary>
/// Places small vegetation (grass, flowers, mushrooms) on surfaces.
/// Only places on air blocks directly above a solid surface.
/// </summary>
public sealed class VegetationDecorator : IDecorator
{
    private readonly ushort _tallGrassBlockId;

    /// <summary>
    /// Creates a vegetation decorator.
    /// </summary>
    /// <param name="tallGrassBlockId">Default block ID for tall grass placement.</param>
    public VegetationDecorator(ushort tallGrassBlockId)
    {
        _tallGrassBlockId = tallGrassBlockId;
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
                int placeY = surfaceY + 1;

                if (!ChunkData.IsInBounds(localX, placeY, localZ))
                {
                    continue;
                }

                if (data.GetBlock(localX, placeY, localZ) != 0)
                {
                    continue;
                }

                if (biome.Vegetation == null)
                {
                    continue;
                }

                foreach (VegetationEntry entry in biome.Vegetation)
                {
                    // Skip tree types (handled by TreeDecorator)
                    if (entry.Type.Contains("tree", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!(random.NextDouble() <= entry.Density))
                    {
                        continue;
                    }

                    data.SetBlock(localX, placeY, localZ, _tallGrassBlockId);

                    break;
                }
            }
        }
    }
}
