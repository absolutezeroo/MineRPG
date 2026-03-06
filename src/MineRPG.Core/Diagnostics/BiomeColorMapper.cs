using System.Collections.Generic;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Maps biome type names to consistent, colorblind-friendly colors
/// for the chunk map and biome overlay visualizations.
/// Colors are pre-computed and cached.
/// </summary>
public static class BiomeColorMapper
{
    // Colorblind-friendly palette (Wong 2011 + extensions)
    private static readonly DebugColor[] Palette =
    {
        new(0.00f, 0.45f, 0.70f),  // Blue
        new(0.90f, 0.62f, 0.00f),  // Orange
        new(0.00f, 0.62f, 0.45f),  // Teal
        new(0.80f, 0.47f, 0.65f),  // Purple
        new(0.94f, 0.89f, 0.26f),  // Yellow
        new(0.34f, 0.71f, 0.91f),  // Sky blue
        new(0.84f, 0.37f, 0.00f),  // Vermillion
        new(0.40f, 0.80f, 0.40f),  // Green
        new(0.70f, 0.30f, 0.30f),  // Red-brown
        new(0.50f, 0.50f, 0.80f),  // Periwinkle
        new(0.60f, 0.80f, 0.20f),  // Lime
        new(0.85f, 0.60f, 0.50f),  // Salmon
        new(0.30f, 0.50f, 0.60f),  // Steel
        new(0.70f, 0.70f, 0.30f),  // Olive
        new(0.55f, 0.35f, 0.65f),  // Violet
        new(0.65f, 0.85f, 0.70f),  // Mint
    };

    private static readonly Dictionary<string, DebugColor> Cache = new();
    private static int _nextPaletteIndex;

    /// <summary>
    /// Gets a consistent color for the given biome name.
    /// The same biome name always returns the same color.
    /// </summary>
    /// <param name="biomeName">The biome type name.</param>
    /// <returns>A colorblind-friendly debug color.</returns>
    public static DebugColor GetColor(string biomeName)
    {
        if (Cache.TryGetValue(biomeName, out DebugColor cached))
        {
            return cached;
        }

        DebugColor color = Palette[_nextPaletteIndex % Palette.Length];
        _nextPaletteIndex++;
        Cache[biomeName] = color;
        return color;
    }

    /// <summary>
    /// Resets the color cache. Call when biome definitions change.
    /// </summary>
    public static void Reset()
    {
        Cache.Clear();
        _nextPaletteIndex = 0;
    }
}
