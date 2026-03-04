using System;
using System.Collections.Generic;

using MineRPG.World.Biomes;

namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Creates horizontal strata of varying block types using a 1D noise pattern.
/// Used for badlands biomes with colored terracotta bands.
/// Only applies when the biome category matches.
/// </summary>
public sealed class NoisePatternRule : ISurfaceRule
{
    private readonly BiomeCategory _targetCategory;
    private readonly ushort[] _strataBlocks;
    private readonly float _strataScale;
    private readonly int _maxDepth;

    /// <summary>
    /// Creates a noise pattern rule for strata generation.
    /// </summary>
    /// <param name="targetCategory">Biome category this rule applies to.</param>
    /// <param name="strataBlocks">Ordered list of block IDs forming the strata pattern.</param>
    /// <param name="strataScale">Vertical scale of the strata pattern.</param>
    /// <param name="maxDepth">Maximum depth below surface to apply strata.</param>
    public NoisePatternRule(
        BiomeCategory targetCategory,
        IReadOnlyList<ushort> strataBlocks,
        float strataScale,
        int maxDepth)
    {
        _targetCategory = targetCategory;
        _strataScale = strataScale;
        _maxDepth = maxDepth;

        _strataBlocks = new ushort[strataBlocks.Count];

        for (int i = 0; i < strataBlocks.Count; i++)
        {
            _strataBlocks[i] = strataBlocks[i];
        }
    }

    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        if (context.Biome == null || context.Biome.Category != _targetCategory)
        {
            return null;
        }

        if (_strataBlocks.Length == 0)
        {
            return null;
        }

        if (context.DepthBelowSurface < 0 || context.DepthBelowSurface > _maxDepth)
        {
            return null;
        }

        // Use Y + pattern noise to create offset strata bands
        float strataInput = (context.WorldY + context.PatternNoise * 4f) * _strataScale;
        int strataIndex = ((int)MathF.Floor(strataInput) % _strataBlocks.Length + _strataBlocks.Length)
                          % _strataBlocks.Length;

        return _strataBlocks[strataIndex];
    }
}
