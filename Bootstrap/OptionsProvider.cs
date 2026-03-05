using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Bridges <see cref="IOptionsProvider"/> to PlayerData (sensitivity),
/// Godot AudioServer (volume), and ChunkLoadingScheduler (render distance).
/// </summary>
public sealed class OptionsProvider : IOptionsProvider
{
    private const int MinRenderDistance = 4;
    private const int MaxRenderDistance = 64;

    private static readonly StringName MasterBusName = new("Master");

    private readonly PlayerData _playerData;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OptionsProvider"/>.
    /// </summary>
    /// <param name="playerData">The player data containing movement settings.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public OptionsProvider(PlayerData playerData, ILogger logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    /// <inheritdoc />
    public float MouseSensitivity
    {
        get => _playerData.MovementSettings.MouseSensitivity;
        set
        {
            _playerData.MovementSettings.MouseSensitivity = value;
            _logger.Debug("OptionsProvider: MouseSensitivity set to {0}", value);
        }
    }

    /// <inheritdoc />
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
            _logger.Debug("OptionsProvider: MasterVolume set to {0}", value);
        }
    }

    /// <inheritdoc />
    public int RenderDistance
    {
        get
        {
            if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(
                out ChunkLoadingScheduler? scheduler))
            {
                return scheduler.CurrentRenderDistance;
            }

            return ChunkLoadingScheduler.DefaultRenderDistance;
        }
        set
        {
            int clamped = System.Math.Clamp(value, MinRenderDistance, MaxRenderDistance);

            if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(
                out ChunkLoadingScheduler? scheduler))
            {
                scheduler.SetRenderDistance(clamped);
            }

            _logger.Debug("OptionsProvider: RenderDistance set to {0}", clamped);
        }
    }
}
