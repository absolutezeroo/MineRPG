namespace MineRPG.Entities.AI.Spawning;

/// <summary>
/// Data-driven spawn conditions for a mob type. Loaded from Data/Mobs/*.json.
/// </summary>
public sealed class SpawnRule
{
    public string MobId { get; init; } = "";
    public IReadOnlyList<string> Biomes { get; init; } = [];
    public int MinLightLevel { get; init; }
    public int MaxLightLevel { get; init; } = 15;
    public float Weight { get; init; } = 1f;
    public int MinGroupSize { get; init; } = 1;
    public int MaxGroupSize { get; init; } = 1;
}
