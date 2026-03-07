using Godot;

using MineRPG.Core.Logging;

namespace MineRPG.Game.Bootstrap.Settings;

/// <summary>
/// Applies audio settings to the Godot AudioServer.
/// </summary>
internal sealed class AudioOptionsApplicator
{
    private static readonly StringName MasterBusName = new("Master");
    private static readonly StringName SfxBusName = new("SFX");
    private static readonly StringName MusicBusName = new("Music");

    private readonly ILogger _logger;

    /// <summary>
    /// Creates an audio options applicator.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public AudioOptionsApplicator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>Gets or sets the master audio bus volume (0.0 to 1.0).</summary>
    public float MasterVolume
    {
        get => GetBusVolume(MasterBusName);
        set => SetBusVolume(MasterBusName, value, "MasterVolume");
    }

    /// <summary>Gets or sets the SFX audio bus volume (0.0 to 1.0).</summary>
    public float SfxVolume
    {
        get => GetBusVolume(SfxBusName);
        set => SetBusVolume(SfxBusName, value, "SfxVolume");
    }

    /// <summary>Gets or sets the music audio bus volume (0.0 to 1.0).</summary>
    public float MusicVolume
    {
        get => GetBusVolume(MusicBusName);
        set => SetBusVolume(MusicBusName, value, "MusicVolume");
    }

    private static float GetBusVolume(StringName busName)
    {
        int busIndex = AudioServer.GetBusIndex(busName);

        if (busIndex < 0)
        {
            return 1.0f;
        }

        return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
    }

    private void SetBusVolume(StringName busName, float value, string label)
    {
        int busIndex = AudioServer.GetBusIndex(busName);

        if (busIndex < 0)
        {
            _logger.Warning("AudioOptions: Bus '{0}' not found in AudioServer.", busName);
            return;
        }

        AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(value));
        _logger.Debug("AudioOptions: {0}={1}", label, value);
    }
}
