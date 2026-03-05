using Godot;

using MineRPG.Core.Logging;

namespace MineRPG.Game.Bootstrap.Options;

/// <summary>
/// Applies audio settings to the Godot AudioServer.
/// </summary>
internal sealed class AudioOptionsApplicator
{
    private static readonly StringName MasterBusName = new("Master");

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
        get
        {
            int busIndex = AudioServer.GetBusIndex(MasterBusName);
            return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
        }
        set
        {
            int busIndex = AudioServer.GetBusIndex(MasterBusName);
            AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(value));
            _logger.Debug("AudioOptions: MasterVolume={0}", value);
        }
    }
}
