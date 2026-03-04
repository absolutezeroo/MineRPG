using Newtonsoft.Json;

namespace MineRPG.World.Blocks;

/// <summary>
/// Per-face texture configuration for a block. Supports shorthand groups
/// ("all", "top", "bottom", "side") and individual face overrides
/// ("east", "west", "north", "south").
///
/// Face direction indices match ChunkMeshBuilder convention:
/// 0=+X(east), 1=-X(west), 2=+Y(top), 3=-Y(bottom), 4=+Z(south), 5=-Z(north).
/// </summary>
public sealed class BlockFaceTextures
{
    /// <summary>Number of face directions on a cube.</summary>
    public const int FaceCount = 6;

    private const int EastIndex = 0;
    private const int WestIndex = 1;
    private const int TopIndex = 2;
    private const int BottomIndex = 3;
    private const int SouthIndex = 4;
    private const int NorthIndex = 5;

    /// <summary>Texture applied to all faces when set.</summary>
    [JsonProperty("all")]
    public string? All { get; init; }

    /// <summary>Texture applied to the top face (+Y).</summary>
    [JsonProperty("top")]
    public string? Top { get; init; }

    /// <summary>Texture applied to the bottom face (-Y).</summary>
    [JsonProperty("bottom")]
    public string? Bottom { get; init; }

    /// <summary>Texture applied to all four side faces.</summary>
    [JsonProperty("side")]
    public string? Side { get; init; }

    /// <summary>Texture applied to the east face (+X).</summary>
    [JsonProperty("east")]
    public string? East { get; init; }

    /// <summary>Texture applied to the west face (-X).</summary>
    [JsonProperty("west")]
    public string? West { get; init; }

    /// <summary>Texture applied to the north face (-Z).</summary>
    [JsonProperty("north")]
    public string? North { get; init; }

    /// <summary>Texture applied to the south face (+Z).</summary>
    [JsonProperty("south")]
    public string? South { get; init; }

    /// <summary>
    /// Resolves all 6 face directions to texture names.
    /// Priority: individual face > group (top/bottom/side) > all.
    /// Returns null entries for faces with no texture assigned.
    /// </summary>
    /// <returns>An array of 6 texture names, one per face direction.</returns>
    public string?[] Resolve()
    {
        string?[] result = new string?[FaceCount];

        // Layer 1: "all" fills every face
        if (All is not null)
        {
            for (int i = 0; i < FaceCount; i++)
            {
                result[i] = All;
            }
        }

        // Layer 2: groups override
        if (Top is not null)
        {
            result[TopIndex] = Top;
        }

        if (Bottom is not null)
        {
            result[BottomIndex] = Bottom;
        }

        if (Side is not null)
        {
            result[EastIndex] = Side;
            result[WestIndex] = Side;
            result[SouthIndex] = Side;
            result[NorthIndex] = Side;
        }

        // Layer 3: individual faces override everything
        if (East is not null)
        {
            result[EastIndex] = East;
        }

        if (West is not null)
        {
            result[WestIndex] = West;
        }

        if (North is not null)
        {
            result[NorthIndex] = North;
        }

        if (South is not null)
        {
            result[SouthIndex] = South;
        }

        return result;
    }
}
