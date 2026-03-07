using Godot;

using MineRPG.Core.Logging;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Items;

/// <summary>
/// Loads individual item icon PNGs from Assets/Textures/Items/{iconAtlasId}.png,
/// blits them into a single atlas Image, and returns an ImageTexture.
/// Missing textures are replaced with a magenta fallback.
/// Mirrors the pattern of <see cref="MineRPG.Godot.World.Rendering.TextureAtlasBuilder"/>.
/// </summary>
public static class ItemIconAtlasBuilder
{
    private const string TexturePath = "res://Assets/Textures/Items/";

    /// <summary>
    /// Builds an atlas texture from individual item icon textures arranged
    /// according to the layout.
    /// </summary>
    /// <param name="layout">The item icon atlas layout defining grid positions.</param>
    /// <param name="logger">Logger for reporting missing textures.</param>
    /// <returns>An ImageTexture containing all item icons packed into a single atlas.</returns>
    public static ImageTexture Build(ItemIconAtlasLayout layout, ILogger logger)
    {
        int tileSize = ItemIconAtlasLayout.TileSize;
        int columns = layout.Columns;
        int rows = layout.Rows;

        if (columns == 0 || rows == 0)
        {
            logger.Warning("ItemIconAtlasBuilder: No icons to pack -- returning 1x1 magenta.");
            Image fallback = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            fallback.SetPixel(0, 0, new Color(1f, 0f, 1f));
            return ImageTexture.CreateFromImage(fallback);
        }

        int atlasWidth = columns * tileSize;
        int atlasHeight = rows * tileSize;
        Image atlas = Image.CreateEmpty(atlasWidth, atlasHeight, false, Image.Format.Rgba8);

        foreach (string iconId in layout.IconAtlasIds)
        {
            (int column, int row) = layout.GetGridPosition(iconId);
            string filePath = $"{TexturePath}{iconId}.png";

            Image? tileImage = null;

            if (ResourceLoader.Exists(filePath))
            {
                Texture2D? texture = GD.Load<Texture2D>(filePath);

                if (texture is not null)
                {
                    tileImage = texture.GetImage();
                    texture.Dispose();
                    tileImage.Convert(Image.Format.Rgba8);

                    if (tileImage.GetWidth() != tileSize || tileImage.GetHeight() != tileSize)
                    {
                        tileImage.Resize(tileSize, tileSize, Image.Interpolation.Nearest);
                    }
                }
            }

            if (tileImage is null)
            {
                logger.Warning(
                    "ItemIconAtlasBuilder: Missing icon '{0}' at '{1}' -- using magenta fallback.",
                    iconId, filePath);
                tileImage = CreateMagentaTile(tileSize);
            }

            int destinationX = column * tileSize;
            int destinationY = row * tileSize;
            atlas.BlitRect(
                tileImage,
                new Rect2I(0, 0, tileSize, tileSize),
                new Vector2I(destinationX, destinationY));
        }

        logger.Info(
            "ItemIconAtlasBuilder: Packed {0} icons into {1}x{2} atlas ({3}x{4} grid).",
            layout.IconAtlasIds.Count, atlasWidth, atlasHeight, columns, rows);

        return ImageTexture.CreateFromImage(atlas);
    }

    private static Image CreateMagentaTile(int size)
    {
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(new Color(1f, 0f, 1f));
        return image;
    }
}
