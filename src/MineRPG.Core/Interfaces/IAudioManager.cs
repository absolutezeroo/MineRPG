namespace MineRPG.Core.Interfaces;

/// <summary>
/// Contract for centralized audio playback. Implementations manage
/// bus volumes, SFX pooling, and music transitions.
/// Uses float coordinates instead of Vector3 to remain Godot-free.
/// </summary>
public interface IAudioManager
{
    /// <summary>
    /// Plays a 2D (non-spatialized) sound effect by its logical key.
    /// If the key is not registered or the audio file is missing, the
    /// call is silently skipped.
    /// </summary>
    /// <param name="key">The logical sound key (e.g. "item_pickup").</param>
    void PlaySfx(string key);

    /// <summary>
    /// Plays a spatialized 3D sound effect at the given world position.
    /// </summary>
    /// <param name="key">The logical sound key.</param>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    void PlaySfx3D(string key, float worldX, float worldY, float worldZ);

    /// <summary>
    /// Starts playing a music track by logical key, fading out the
    /// current track first. Passing null stops all music.
    /// </summary>
    /// <param name="key">The logical music key, or null to stop.</param>
    void PlayMusic(string? key);

    /// <summary>Gets or sets the SFX bus volume (0.0 to 1.0).</summary>
    float SfxVolume { get; set; }

    /// <summary>Gets or sets the Music bus volume (0.0 to 1.0).</summary>
    float MusicVolume { get; set; }
}
