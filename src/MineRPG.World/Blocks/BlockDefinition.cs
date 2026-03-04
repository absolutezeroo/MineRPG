using Newtonsoft.Json;

namespace MineRPG.World.Blocks;

/// <summary>
/// Data-driven block definition. Loaded from Data/Blocks/*.json at startup.
/// Per-face UVs are computed at load time by BlockRegistry from the Textures config.
/// </summary>
public sealed class BlockDefinition
{
    private const int FaceUvLength = 24;

    /// <summary>Unique numeric block identifier.</summary>
    [JsonProperty("id")]
    public ushort Id { get; init; }

    /// <summary>Human-readable block name.</summary>
    [JsonProperty("name")]
    public string Name { get; init; } = "";

    /// <summary>Behavioral flags for this block type.</summary>
    [JsonProperty("flags")]
    public BlockFlags Flags { get; init; }

    /// <summary>Mining hardness value.</summary>
    [JsonProperty("hardness")]
    public float Hardness { get; init; }

    /// <summary>Per-face texture configuration.</summary>
    [JsonProperty("textures")]
    public BlockFaceTextures? Textures { get; init; }

    /// <summary>Reference key into the loot table registry.</summary>
    [JsonProperty("lootTableRef")]
    public string? LootTableRef { get; init; }

    /// <summary>Red tint component (0..1).</summary>
    [JsonProperty("tintR")]
    public float TintR { get; init; } = 1f;

    /// <summary>Green tint component (0..1).</summary>
    [JsonProperty("tintG")]
    public float TintG { get; init; } = 1f;

    /// <summary>Blue tint component (0..1).</summary>
    [JsonProperty("tintB")]
    public float TintB { get; init; } = 1f;

    /// <summary>
    /// Per-face UV coordinates, computed by BlockRegistry.
    /// Layout: 6 faces x 4 floats (u0, v0, u1, v1). Index: faceDir * 4.
    /// </summary>
    [JsonIgnore]
    public float[] FaceUvs { get; } = new float[FaceUvLength];

    /// <summary>Whether this block has the Solid flag.</summary>
    public bool IsSolid => (Flags & BlockFlags.Solid) != 0;

    /// <summary>Whether this block has the Transparent flag.</summary>
    public bool IsTransparent => (Flags & BlockFlags.Transparent) != 0;

    /// <summary>Whether this block has the Liquid flag.</summary>
    public bool IsLiquid => (Flags & BlockFlags.Liquid) != 0;

    /// <summary>Whether this block is air (ID 0).</summary>
    public bool IsAir => Id == 0;
}
