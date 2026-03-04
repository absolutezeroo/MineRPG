using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators;

/// <summary>
/// Places decoration features (trees, vegetation, ores) on generated terrain.
/// Decorators run after terrain, caves, and surface building are complete.
/// </summary>
public interface IDecorator
{
    /// <summary>
    /// Decorates the given chunk data using biome information.
    /// </summary>
    /// <param name="data">The chunk data to decorate in-place.</param>
    /// <param name="biomeMap">Biome definitions indexed by column (16x16).</param>
    /// <param name="heightMap">Surface heights indexed by column (16x16).</param>
    /// <param name="chunkWorldX">World X of the chunk origin.</param>
    /// <param name="chunkWorldZ">World Z of the chunk origin.</param>
    /// <param name="random">Seeded random for deterministic placement.</param>
    public void Decorate(
        ChunkData data,
        BiomeDefinition[] biomeMap,
        int[] heightMap,
        int chunkWorldX,
        int chunkWorldZ,
        Random random);
}
