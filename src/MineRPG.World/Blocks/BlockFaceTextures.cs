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
    public const int FaceCount = 6;

    [JsonProperty("all")]
    public string? All { get; init; }

    [JsonProperty("top")]
    public string? Top { get; init; }

    [JsonProperty("bottom")]
    public string? Bottom { get; init; }

    [JsonProperty("side")]
    public string? Side { get; init; }

    [JsonProperty("east")]
    public string? East { get; init; }

    [JsonProperty("west")]
    public string? West { get; init; }

    [JsonProperty("north")]
    public string? North { get; init; }

    [JsonProperty("south")]
    public string? South { get; init; }

    /// <summary>
    /// Resolves all 6 face directions to texture names.
    /// Priority: individual face > group (top/bottom/side) > all.
    /// Returns null entries for faces with no texture assigned.
    /// </summary>
    public string?[] Resolve()
    {
        var result = new string?[FaceCount];

        // Layer 1: "all" fills every face
        if (All is not null)
        {
            for (var i = 0; i < FaceCount; i++)
                result[i] = All;
        }

        // Layer 2: groups override
        if (Top is not null)
            result[2] = Top;

        if (Bottom is not null)
            result[3] = Bottom;

        if (Side is not null)
        {
            result[0] = Side; // +X east
            result[1] = Side; // -X west
            result[4] = Side; // +Z south
            result[5] = Side; // -Z north
        }

        // Layer 3: individual faces override everything
        if (East is not null)
            result[0] = East;

        if (West is not null)
            result[1] = West;

        if (North is not null)
            result[5] = North;

        if (South is not null)
            result[4] = South;

        return result;
    }
}
