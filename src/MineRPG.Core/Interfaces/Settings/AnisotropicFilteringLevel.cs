namespace MineRPG.Core.Interfaces.Settings;

/// <summary>
/// Anisotropic texture filtering levels.
/// </summary>
public enum AnisotropicFilteringLevel
{
    /// <summary>Anisotropic filtering disabled.</summary>
    Disabled,

    /// <summary>2x anisotropic filtering.</summary>
    AF2x,

    /// <summary>4x anisotropic filtering.</summary>
    AF4x,

    /// <summary>8x anisotropic filtering.</summary>
    AF8x,

    /// <summary>16x anisotropic filtering.</summary>
    AF16x,
}
