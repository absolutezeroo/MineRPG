namespace MineRPG.Core.DataLoading;

/// <summary>
/// Data definition for a single sound entry in the audio bank.
/// Loaded from Data/Audio/sfx_bank.json or music_bank.json.
/// </summary>
public sealed class SoundBankEntry
{
    /// <summary>Logical key used to reference this sound in code (e.g. "item_pickup").</summary>
    public string Key { get; set; } = "";

    /// <summary>Path to the audio file, relative to res:// (e.g. "Assets/Audio/SFX/item_pickup.wav").</summary>
    public string Path { get; set; } = "";

    /// <summary>Volume adjustment in decibels relative to the bus volume. Default 0.</summary>
    public float VolumeDb { get; set; }

    /// <summary>Pitch scale for this sound (1.0 = normal).</summary>
    public float PitchScale { get; set; } = 1.0f;

    /// <summary>Maximum pitch randomization range (+/- this amount). Default 0.</summary>
    public float PitchRandomness { get; set; }
}
