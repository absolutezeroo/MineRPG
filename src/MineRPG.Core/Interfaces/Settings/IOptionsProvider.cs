namespace MineRPG.Core.Interfaces.Settings;

/// <summary>
/// Provides access to runtime-configurable game options.
/// Implemented at the Game/composition level to bridge engine and pure layers.
/// </summary>
public interface IOptionsProvider
{
    // --- Game tab ---

    /// <summary>Gets or sets the mouse look sensitivity (radians per pixel).</summary>
    float MouseSensitivity { get; set; }

    /// <summary>Gets or sets the master audio bus volume (0.0 to 1.0).</summary>
    float MasterVolume { get; set; }

    /// <summary>Gets or sets the SFX audio bus volume (0.0 to 1.0).</summary>
    float SfxVolume { get; set; }

    /// <summary>Gets or sets the music audio bus volume (0.0 to 1.0).</summary>
    float MusicVolume { get; set; }

    // --- Graphics tab ---

    /// <summary>Gets or sets the chunk render distance.</summary>
    int RenderDistance { get; set; }

    /// <summary>Gets or sets whether VSync is enabled.</summary>
    bool VSyncEnabled { get; set; }

    /// <summary>Gets or sets the window display mode.</summary>
    WindowModeOption WindowMode { get; set; }

    /// <summary>Gets or sets the MSAA anti-aliasing quality.</summary>
    MsaaQuality MsaaQuality { get; set; }

    /// <summary>Gets or sets the shadow rendering quality preset.</summary>
    ShadowQuality ShadowQuality { get; set; }

    /// <summary>Gets or sets whether SSAO is enabled.</summary>
    bool SsaoEnabled { get; set; }

    /// <summary>Gets or sets the anisotropic filtering level.</summary>
    AnisotropicFilteringLevel AnisotropicFiltering { get; set; }

    /// <summary>Gets or sets the camera field of view in degrees (40 to 120).</summary>
    float FieldOfView { get; set; }

    /// <summary>Gets or sets the display brightness/gamma multiplier (0.5 to 2.0).</summary>
    float Brightness { get; set; }

    /// <summary>
    /// Updates the in-memory keybinds cache and persists the full settings snapshot.
    /// Called by ControlsTabPanel after rebinding a key.
    /// </summary>
    /// <param name="keybinds">The updated keybind dictionary.</param>
    void UpdateKeybindsAndSave(System.Collections.Generic.Dictionary<string, DataLoading.KeybindData> keybinds);
}
