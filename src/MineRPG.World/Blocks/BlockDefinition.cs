using Newtonsoft.Json;

namespace MineRPG.World.Blocks;

/// <summary>
/// Data-driven block definition. Loaded from Data/Blocks/*.json at startup.
/// Per-face UVs are computed at load time by BlockRegistry from the Textures config.
/// </summary>
public sealed class BlockDefinition
{
    [JsonProperty("id")]
    public ushort Id { get; init; }

    [JsonProperty("name")]
    public string Name { get; init; } = "";

    [JsonProperty("flags")]
    public BlockFlags Flags { get; init; }

    [JsonProperty("hardness")]
    public float Hardness { get; init; }

    [JsonProperty("textures")]
    public BlockFaceTextures? Textures { get; init; }

    [JsonProperty("lootTableRef")]
    public string? LootTableRef { get; init; }

    [JsonProperty("tintR")]
    public float TintR { get; init; } = 1f;

    [JsonProperty("tintG")]
    public float TintG { get; init; } = 1f;

    [JsonProperty("tintB")]
    public float TintB { get; init; } = 1f;

    /// <summary>
    /// Per-face UV coordinates, computed by BlockRegistry.
    /// Layout: 6 faces x 4 floats (u0, v0, u1, v1). Index: faceDir * 4.
    /// </summary>
    [JsonIgnore]
    public float[] FaceUvs { get; } = new float[24];

    public bool IsSolid => (Flags & BlockFlags.Solid) != 0;
    public bool IsTransparent => (Flags & BlockFlags.Transparent) != 0;
    public bool IsLiquid => (Flags & BlockFlags.Liquid) != 0;
    public bool IsAir => Id == 0;
}
