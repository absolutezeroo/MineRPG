namespace MineRPG.Core.Interfaces;

/// <summary>
/// Provides access to runtime-configurable game options.
/// Implemented at the Game/composition level to bridge engine and pure layers.
/// </summary>
public interface IOptionsProvider
{
    /// <summary>Gets or sets the mouse look sensitivity (radians per pixel).</summary>
    float MouseSensitivity { get; set; }

    /// <summary>Gets or sets the master audio bus volume (0.0 to 1.0).</summary>
    float MasterVolume { get; set; }

    /// <summary>Gets or sets the chunk render distance.</summary>
    int RenderDistance { get; set; }
}
