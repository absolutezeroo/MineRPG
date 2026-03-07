using System;
using System.Collections.Generic;

namespace MineRPG.RPG.Items;

/// <summary>
/// Pure C# atlas layout for item icons. Given a set of unique IconAtlasId strings,
/// assigns each a grid cell (row-major) and computes UV bounds.
/// Built by <see cref="ItemRegistry"/> when the registry is frozen.
/// </summary>
public sealed class ItemIconAtlasLayout
{
    /// <summary>Size of each tile in pixels.</summary>
    public const int TileSize = 16;

    private readonly Dictionary<string, int> _idToIndex;

    /// <summary>Ordered list of all icon atlas IDs in this layout.</summary>
    public IReadOnlyList<string> IconAtlasIds { get; }

    /// <summary>Number of columns in the atlas grid.</summary>
    public int Columns { get; }

    /// <summary>Number of rows in the atlas grid.</summary>
    public int Rows { get; }

    /// <summary>
    /// Creates an atlas layout from the given set of icon atlas IDs.
    /// Empty or null IDs are skipped. Duplicates are deduplicated.
    /// </summary>
    /// <param name="iconAtlasIds">All IconAtlasId values from the item registry.</param>
    public ItemIconAtlasLayout(IEnumerable<string> iconAtlasIds)
    {
        List<string> ids = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string id in iconAtlasIds)
        {
            if (!string.IsNullOrEmpty(id) && seen.Add(id))
            {
                ids.Add(id);
            }
        }

        IconAtlasIds = ids;
        _idToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < ids.Count; i++)
        {
            _idToIndex[ids[i]] = i;
        }

        Columns = ids.Count > 0 ? (int)Math.Ceiling(Math.Sqrt(ids.Count)) : 0;
        Rows = ids.Count > 0 ? (int)Math.Ceiling((double)ids.Count / Columns) : 0;
    }

    /// <summary>
    /// Returns the grid cell (column, row) for the given icon atlas ID.
    /// </summary>
    /// <param name="iconAtlasId">The icon atlas ID to look up.</param>
    /// <returns>Grid cell as (Column, Row).</returns>
    /// <exception cref="KeyNotFoundException">If the ID is not in the layout.</exception>
    public (int Column, int Row) GetGridPosition(string iconAtlasId)
    {
        if (!_idToIndex.TryGetValue(iconAtlasId, out int index))
        {
            throw new KeyNotFoundException(
                $"Icon atlas ID '{iconAtlasId}' not found in layout.");
        }

        return (index % Columns, index / Columns);
    }

    /// <summary>
    /// Returns (u0, v0, u1, v1) normalized UV bounds for the given icon atlas ID.
    /// </summary>
    /// <param name="iconAtlasId">The icon atlas ID to look up.</param>
    /// <returns>UV bounds as (U0, V0, U1, V1).</returns>
    public (float U0, float V0, float U1, float V1) GetUvBounds(string iconAtlasId)
    {
        (int column, int row) = GetGridPosition(iconAtlasId);
        float tileWidth = 1f / Columns;
        float tileHeight = 1f / Rows;

        return (column * tileWidth, row * tileHeight,
            (column + 1) * tileWidth, (row + 1) * tileHeight);
    }

    /// <summary>
    /// Checks whether the given icon atlas ID is present in this layout.
    /// </summary>
    /// <param name="iconAtlasId">The icon atlas ID to check.</param>
    /// <returns>True if found.</returns>
    public bool Contains(string iconAtlasId) => _idToIndex.ContainsKey(iconAtlasId);
}
