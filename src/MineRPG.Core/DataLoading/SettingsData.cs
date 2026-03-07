using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Serializable snapshot of all user settings.
/// Written to and read from the settings config file.
/// </summary>
public sealed class SettingsData
{
    // --- Game tab ---

    /// <summary>Mouse look sensitivity in radians per pixel.</summary>
    public float MouseSensitivity { get; set; } = 0.002f;

    /// <summary>Master audio bus volume (0.0 to 1.0).</summary>
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>SFX audio bus volume (0.0 to 1.0).</summary>
    public float SfxVolume { get; set; } = 1.0f;

    /// <summary>Music audio bus volume (0.0 to 1.0).</summary>
    public float MusicVolume { get; set; } = 1.0f;

    // --- Graphics tab ---

    /// <summary>Chunk render distance in chunks.</summary>
    public int RenderDistance { get; set; } = 8;

    /// <summary>Whether VSync is enabled.</summary>
    public bool VSyncEnabled { get; set; } = true;

    /// <summary>Window display mode.</summary>
    public WindowModeOption WindowMode { get; set; } = WindowModeOption.Windowed;

    /// <summary>MSAA anti-aliasing quality.</summary>
    public MsaaQuality MsaaQuality { get; set; } = MsaaQuality.Msaa2x;

    /// <summary>Shadow rendering quality preset.</summary>
    public ShadowQuality ShadowQuality { get; set; } = ShadowQuality.Medium;

    /// <summary>Whether screen-space ambient occlusion is enabled.</summary>
    public bool SsaoEnabled { get; set; }

    /// <summary>Anisotropic texture filtering level.</summary>
    public AnisotropicFilteringLevel AnisotropicFiltering { get; set; } = AnisotropicFilteringLevel.AF4x;

    /// <summary>Camera field of view in degrees.</summary>
    public float FieldOfView { get; set; } = 75f;

    /// <summary>Display brightness/gamma multiplier.</summary>
    public float Brightness { get; set; } = 1.0f;

    // --- Controls tab ---

    /// <summary>Custom keybind overrides. Key is the action name, value is the binding data.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Setter required for JSON deserialization.")]
    public Dictionary<string, KeybindData> Keybinds { get; set; } = new();
}
