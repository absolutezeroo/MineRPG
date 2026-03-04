namespace MineRPG.World.Blocks;

/// <summary>
/// Pure C# atlas layout. Given a set of unique texture names, assigns each
/// a grid position (row-major) and computes UV bounds.
/// The bridge layer uses this to blit individual PNGs into a single atlas image.
/// </summary>
public sealed class TextureAtlasLayout
{
    private readonly Dictionary<string, int> _nameToIndex;

    public IReadOnlyList<string> TextureNames { get; }
    public int Columns { get; }
    public int Rows { get; }

    public TextureAtlasLayout(IEnumerable<string> textureNames)
    {
        var names = textureNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        TextureNames = names;
        _nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < names.Count; i++)
            _nameToIndex[names[i]] = i;

        Columns = (int)Math.Ceiling(Math.Sqrt(names.Count));
        Rows = names.Count > 0 ? (int)Math.Ceiling((double)names.Count / Columns) : 0;
    }

    /// <summary>
    /// Returns (u0, v0, u1, v1) UV bounds for the given texture name.
    /// </summary>
    public (float U0, float V0, float U1, float V1) GetUvBounds(string textureName)
    {
        if (!_nameToIndex.TryGetValue(textureName, out var index))
            throw new KeyNotFoundException($"Texture '{textureName}' not found in atlas layout.");

        var col = index % Columns;
        var row = index / Columns;
        var tileW = 1f / Columns;
        var tileH = 1f / Rows;

        return (col * tileW, row * tileH, (col + 1) * tileW, (row + 1) * tileH);
    }

    /// <summary>
    /// Returns the grid position (column, row) for the given texture name.
    /// </summary>
    public (int Column, int Row) GetGridPosition(string textureName)
    {
        if (!_nameToIndex.TryGetValue(textureName, out var index))
            throw new KeyNotFoundException($"Texture '{textureName}' not found in atlas layout.");

        return (index % Columns, index / Columns);
    }

    public bool Contains(string textureName) => _nameToIndex.ContainsKey(textureName);
}
