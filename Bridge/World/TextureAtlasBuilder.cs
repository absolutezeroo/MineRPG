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

    public static ImageTexture Build(TextureAtlasLayout layout, ILogger logger)
    {
        var cols = layout.Columns;
        var rows = layout.Rows;

        if (cols == 0 || rows == 0)
        {
            logger.Warning("TextureAtlasBuilder: No textures to pack — returning 1x1 magenta.");
            var fallback = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            fallback.SetPixel(0, 0, new Color(1f, 0f, 1f));
            return ImageTexture.CreateFromImage(fallback);
        }

        var atlasWidth = cols * TileSize;
        var atlasHeight = rows * TileSize;
        var atlas = Image.CreateEmpty(atlasWidth, atlasHeight, false, Image.Format.Rgba8);

        foreach (var textureName in layout.TextureNames)
        {
            var (col, row) = layout.GetGridPosition(textureName);
            var filePath = $"{TexturePath}{textureName}.png";

            Image? tileImage = null;
            if (ResourceLoader.Exists(filePath))
            {
                var tex = GD.Load<Texture2D>(filePath);
                if (tex is not null)
                {
                    tileImage = tex.GetImage();
                    if (tileImage.GetWidth() != TileSize || tileImage.GetHeight() != TileSize)
                    {
                        tileImage.Resize(TileSize, TileSize, Image.Interpolation.Nearest);
                    }
                }
            }

            if (tileImage is null)
            {
                logger.Warning("TextureAtlasBuilder: Missing texture '{0}' — using magenta fallback.", textureName);
                tileImage = CreateMagentaTile();
            }

            var destX = col * TileSize;
            var destY = row * TileSize;
            atlas.BlitRect(tileImage, new Rect2I(0, 0, TileSize, TileSize), new Vector2I(destX, destY));
        }

        logger.Info("TextureAtlasBuilder: Packed {0} textures into {1}x{2} atlas ({3}x{4} grid).",
            layout.TextureNames.Count, atlasWidth, atlasHeight, cols, rows);

        return ImageTexture.CreateFromImage(atlas);
    }

    private static Image CreateMagentaTile()
    {
        var img = Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);
        img.Fill(new Color(1f, 0f, 1f));
        return img;
    }
}
