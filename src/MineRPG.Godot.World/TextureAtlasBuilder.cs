using Godot;

using MineRPG.Core.Logging;
using MineRPG.World.Blocks;

namespace MineRPG.Godot.World;

/// <summary>
/// Loads individual block texture PNGs from Assets/Textures/Blocks/{name}.png,
/// blits them into a single atlas Image, and returns an ImageTexture.
/// Missing textures are replaced with a magenta fallback.
/// </summary>
public static class TextureAtlasBuilder
{
    private const int TileSize = 16;
    private const string TexturePath = "res://Assets/Textures/Blocks/";

    /// <summary>
    /// Builds an atlas texture from individual block textures arranged according to the layout.
    /// </summary>
    /// <param name="layout">The texture atlas layout defining grid positions for each texture.</param>
    /// <param name="logger">The logger for reporting missing textures and build results.</param>
    /// <returns>An ImageTexture containing all block textures packed into a single atlas.</returns>
    public static ImageTexture Build(TextureAtlasLayout layout, ILogger logger)
    {
        int columns = layout.Columns;
        int rows = layout.Rows;

        if (columns == 0 || rows == 0)
        {
            logger.Warning("TextureAtlasBuilder: No textures to pack -- returning 1x1 magenta.");
            Image fallback = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            fallback.SetPixel(0, 0, new Color(1f, 0f, 1f));
            return ImageTexture.CreateFromImage(fallback);
        }

        int atlasWidth = columns * TileSize;
        int atlasHeight = rows * TileSize;
        Image atlas = Image.CreateEmpty(atlasWidth, atlasHeight, false, Image.Format.Rgba8);

        foreach (string textureName in layout.TextureNames)
        {
            (int column, int row) = layout.GetGridPosition(textureName);
            string filePath = $"{TexturePath}{textureName}.png";

            Image? tileImage = null;

            if (ResourceLoader.Exists(filePath))
            {
                Texture2D? texture = GD.Load<Texture2D>(filePath);

                if (texture is not null)
                {
                    tileImage = texture.GetImage();
                    tileImage.Convert(Image.Format.Rgba8);

                    if (tileImage.GetWidth() != TileSize || tileImage.GetHeight() != TileSize)
                    {
                        tileImage.Resize(TileSize, TileSize, Image.Interpolation.Nearest);
                    }
                }
            }

            if (tileImage is null)
            {
                logger.Warning("TextureAtlasBuilder: Missing texture '{0}' -- using magenta fallback.", textureName);
                tileImage = CreateMagentaTile();
            }

            int destinationX = column * TileSize;
            int destinationY = row * TileSize;
            atlas.BlitRect(tileImage, new Rect2I(0, 0, TileSize, TileSize), new Vector2I(destinationX, destinationY));
        }

        logger.Info("TextureAtlasBuilder: Packed {0} textures into {1}x{2} atlas ({3}x{4} grid).",
            layout.TextureNames.Count, atlasWidth, atlasHeight, columns, rows);

        return ImageTexture.CreateFromImage(atlas);
    }

    private static Image CreateMagentaTile()
    {
        Image image = Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);
        image.Fill(new Color(1f, 0f, 1f));
        return image;
    }
}
