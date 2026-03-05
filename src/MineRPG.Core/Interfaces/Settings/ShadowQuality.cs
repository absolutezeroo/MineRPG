namespace MineRPG.Core.Interfaces.Settings;

/// <summary>
/// Shadow rendering quality presets.
/// </summary>
public enum ShadowQuality
{
    /// <summary>Low quality shadows (1024px atlas, basic filtering).</summary>
    Low,

    /// <summary>Medium quality shadows (2048px atlas).</summary>
    Medium,

    /// <summary>High quality shadows (4096px atlas).</summary>
    High,

    /// <summary>Ultra quality shadows (8192px atlas, max filtering).</summary>
    Ultra,
}
