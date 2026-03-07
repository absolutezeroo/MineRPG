using Newtonsoft.Json;

namespace MineRPG.World.Blocks;

/// <summary>
/// Data-driven block definition. Loaded from Data/Blocks/*.json at startup.
/// The canonical key is <see cref="Id"/> (a namespaced string like "minerpg:stone").
/// A compact runtime <see cref="RuntimeId"/> is assigned by <see cref="BlockRegistry"/>
/// and used in <see cref="MineRPG.World.Chunks.ChunkData"/> for memory-efficient storage.
/// Per-face UVs are computed at load time by BlockRegistry from the Textures config.
/// </summary>
public sealed class BlockDefinition
{
    private const int FaceUvLength = 24;

    /// <summary>Canonical namespaced identifier (e.g., "minerpg:stone").</summary>
    [JsonProperty("id")]
    public string Id { get; init; } = "";

    /// <summary>Human-readable display name shown in the UI.</summary>
    [JsonProperty("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>Behavioral flags for this block type.</summary>
    [JsonProperty("flags")]
    public BlockFlags Flags { get; init; }

    /// <summary>Mining hardness value. Negative means indestructible.</summary>
    [JsonProperty("hardness")]
    public float Hardness { get; init; }

    /// <summary>
    /// Tool type required for efficient mining and loot drops.
    /// Examples: "pickaxe", "axe", "shovel". Null means any tool or bare hands.
    /// </summary>
    [JsonProperty("requiredToolType")]
    public string? RequiredToolType { get; init; }

    /// <summary>
    /// Required harvest level for efficient mining and loot drops.
    /// Aligned with ToolProperties.HarvestLevel:
    /// 0 = wood, 1 = stone, 2 = iron, 3 = diamond, 4 = netherite.
    /// </summary>
    [JsonProperty("requiredHarvestLevel")]
    public int RequiredHarvestLevel { get; init; }

    /// <summary>Per-face texture configuration.</summary>
    [JsonProperty("textures")]
    public BlockFaceTextures? Textures { get; init; }

    /// <summary>
    /// Reference key into the loot table registry. Convention: matches this block's
    /// namespaced ID (e.g., "minerpg:stone"). Null means no loot table (no drops).
    /// </summary>
    [JsonProperty("lootTableId")]
    public string? LootTableId { get; init; }

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
    /// Runtime-assigned sequential ushort ID. Set by <see cref="BlockRegistry"/> during load.
    /// Never read from JSON. Air is always 0.
    /// Used by ChunkData, meshing, generation, and all hot paths.
    /// </summary>
    [JsonIgnore]
    public ushort RuntimeId { get; internal set; }

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

    /// <summary>Whether this block is air (RuntimeId 0).</summary>
    public bool IsAir => RuntimeId == 0;
}
