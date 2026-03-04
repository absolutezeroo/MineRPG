using System;
using System.Collections.Generic;

namespace MineRPG.World.Generation.Decorators.Trees;

/// <summary>
/// Registry of tree generators keyed by type ID.
/// Allows data-driven biome definitions to reference trees by string name.
/// </summary>
public sealed class TreeRegistry
{
    private readonly Dictionary<string, ITreeGenerator> _generators = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a tree generator.
    /// </summary>
    /// <param name="generator">The generator to register.</param>
    public void Register(ITreeGenerator generator)
    {
        if (generator == null)
        {
            throw new ArgumentNullException(nameof(generator));
        }

        _generators[generator.TypeId] = generator;
    }

    /// <summary>
    /// Attempts to get a tree generator by type ID.
    /// </summary>
    /// <param name="typeId">The tree type identifier.</param>
    /// <param name="generator">The found generator, or null.</param>
    /// <returns>True if found.</returns>
    public bool TryGet(string typeId, out ITreeGenerator? generator)
    {
        return _generators.TryGetValue(typeId, out generator);
    }
}
