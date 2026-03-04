using System;
using System.Collections.Generic;
using System.Linq;

namespace MineRPG.World.Blocks;

/// <summary>
/// Pure C# atlas layout. Given a set of unique texture names, assigns each
/// a grid position (row-major) and computes UV bounds.
/// The bridge layer uses this to blit individual PNGs into a single atlas image.
/// </summary>
public sealed class TextureAtlasLayout
{
    private readonly Dictionary<string, int> _nameToIndex;

    /// <summary>Ordered list of all texture names in this layout.</summary>
    public IReadOnlyList<string> TextureNames { get; }

    /// <summary>Number of columns in the atlas grid.</summary>
    public int Columns { get; }

    /// <summary>Number of rows in the atlas grid.</summary>
    public int Rows { get; }

    /// <summary>
    /// Creates an atlas layout from a set of texture names.
    /// </summary>
    /// <param name="textureNames">The unique texture names to include.</param>
    public TextureAtlasLayout(IEnumerable<string> textureNames)
    {
        List<string> names = textureNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        TextureNames = names;
        _nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < names.Count; i++)
        {
            _nameToIndex[names[i]] = i;
        }

        Columns = (int)Math.Ceiling(Math.Sqrt(names.Count));
        Rows = names.Count > 0 ? (int)Math.Ceiling((double)names.Count / Columns) : 0;
    }

    /// <summary>
    /// Returns (u0, v0, u1, v1) UV bounds for the given texture name.
    /// </summary>
    /// <param name="textureName">The texture name to look up.</param>
    /// <returns>UV bounds as a tuple of (U0, V0, U1, V1).</returns>
    public (float U0, float V0, float U1, float V1) GetUvBounds(string textureName)
    {
        if (!_nameToIndex.TryGetValue(textureName, out int index))
        {
            throw new KeyNotFoundException($"Texture '{textureName}' not found in atlas layout.");
        }

        int column = index % Columns;
        int row = index / Columns;
        float tileWidth = 1f / Columns;
        float tileHeight = 1f / Rows;

        return (column * tileWidth, row * tileHeight, (column + 1) * tileWidth, (row + 1) * tileHeight);
    }

    /// <summary>
    /// Returns the grid position (column, row) for the given texture name.
    /// </summary>
    /// <param name="textureName">The texture name to look up.</param>
    /// <returns>Grid position as (Column, Row).</returns>
    public (int Column, int Row) GetGridPosition(string textureName)
    {
        if (!_nameToIndex.TryGetValue(textureName, out int index))
        {
            throw new KeyNotFoundException($"Texture '{textureName}' not found in atlas layout.");
        }

        return (index % Columns, index / Columns);
    }

    /// <summary>
    /// Checks whether the atlas contains the given texture name.
    /// </summary>
    /// <param name="textureName">The texture name to check.</param>
    /// <returns>True if the texture is in the atlas.</returns>
    public bool Contains(string textureName) => _nameToIndex.ContainsKey(textureName);
}
