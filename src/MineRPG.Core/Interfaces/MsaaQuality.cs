namespace MineRPG.Core.Interfaces;

/// <summary>
/// MSAA anti-aliasing sample count options.
/// </summary>
public enum MsaaQuality
{
    /// <summary>MSAA disabled.</summary>
    Disabled,

    /// <summary>2x multi-sample anti-aliasing.</summary>
    Msaa2x,

    /// <summary>4x multi-sample anti-aliasing.</summary>
    Msaa4x,

    /// <summary>8x multi-sample anti-aliasing.</summary>
    Msaa8x,
}
