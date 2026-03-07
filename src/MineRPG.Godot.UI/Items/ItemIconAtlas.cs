using Godot;

using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Items;

/// <summary>
/// Holds the built item icon atlas texture and provides
/// <see cref="AtlasTexture"/> regions for individual item icons.
/// Registered in the service locator after the item registry is frozen.
/// </summary>
public sealed class ItemIconAtlas
{
    private readonly ImageTexture _atlasTexture;
    private readonly ItemIconAtlasLayout _layout;

    /// <summary>
    /// Creates an item icon atlas from a pre-built atlas texture and layout.
    /// </summary>
    /// <param name="atlasTexture">The packed atlas image texture.</param>
    /// <param name="layout">The layout describing grid positions.</param>
    public ItemIconAtlas(ImageTexture atlasTexture, ItemIconAtlasLayout layout)
    {
        _atlasTexture = atlasTexture;
        _layout = layout;
    }

    /// <summary>
    /// Creates an <see cref="AtlasTexture"/> for the given icon atlas ID.
    /// Returns null if the ID is not found in the layout.
    /// </summary>
    /// <param name="iconAtlasId">The icon atlas ID (e.g. "items/wooden_pickaxe").</param>
    /// <returns>An AtlasTexture pointing to the correct region, or null.</returns>
    public AtlasTexture? GetIconTexture(string iconAtlasId)
    {
        if (string.IsNullOrEmpty(iconAtlasId) || !_layout.Contains(iconAtlasId))
        {
            return null;
        }

        (int column, int row) = _layout.GetGridPosition(iconAtlasId);
        int tileSize = ItemIconAtlasLayout.TileSize;

        AtlasTexture atlas = new()
        {
            Atlas = _atlasTexture,
            Region = new Rect2(
                column * tileSize,
                row * tileSize,
                tileSize,
                tileSize),
        };

        return atlas;
    }
}
