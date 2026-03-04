using System;
using System.Collections.Generic;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators;

/// <summary>
/// Executes a sequence of decorators in order on a chunk.
/// Each decorator adds features (trees, vegetation, ores) to the terrain.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class DecoratorPipeline
{
    private readonly IDecorator[] _decorators;

    /// <summary>
    /// Creates a decorator pipeline with the given ordered decorators.
    /// </summary>
    /// <param name="decorators">Decorators to execute in order.</param>
    public DecoratorPipeline(IReadOnlyList<IDecorator> decorators)
    {
        if (decorators == null)
        {
            throw new ArgumentNullException(nameof(decorators));
        }

        _decorators = new IDecorator[decorators.Count];

        for (int i = 0; i < decorators.Count; i++)
        {
            _decorators[i] = decorators[i];
        }
    }

    /// <summary>
    /// Runs all decorators on the given chunk in sequence.
    /// </summary>
    /// <param name="data">The chunk data to decorate.</param>
    /// <param name="biomeMap">Biome per column (16x16 flat array).</param>
    /// <param name="heightMap">Surface height per column (16x16 flat array).</param>
    /// <param name="chunkWorldX">World X of the chunk origin.</param>
    /// <param name="chunkWorldZ">World Z of the chunk origin.</param>
    /// <param name="seed">World seed combined with chunk position for determinism.</param>
    public void DecorateChunk(
        ChunkData data,
        BiomeDefinition[] biomeMap,
        int[] heightMap,
        int chunkWorldX,
        int chunkWorldZ,
        int seed)
    {
        // Deterministic per-chunk random based on seed + chunk position
        int chunkSeed = HashCombine(seed, chunkWorldX, chunkWorldZ);
        Random random = new Random(chunkSeed);

        for (int i = 0; i < _decorators.Length; i++)
        {
            _decorators[i].Decorate(data, biomeMap, heightMap, chunkWorldX, chunkWorldZ, random);
        }
    }

    private static int HashCombine(int seed, int x, int z)
    {
        unchecked
        {
            int hash = seed;
            hash = hash * 31 + x;
            hash = hash * 31 + z;
            return hash;
        }
    }
}
