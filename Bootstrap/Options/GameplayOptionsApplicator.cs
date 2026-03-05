using MineRPG.Core.Logging;
using MineRPG.Entities.Player;

namespace MineRPG.Game.Bootstrap.Options;

/// <summary>
/// Applies gameplay settings such as mouse sensitivity and input preferences.
/// </summary>
internal sealed class GameplayOptionsApplicator
{
    private readonly PlayerData _playerData;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a gameplay options applicator.
    /// </summary>
    /// <param name="playerData">Player data containing movement settings.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public GameplayOptionsApplicator(PlayerData playerData, ILogger logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    /// <summary>Gets or sets the mouse look sensitivity (radians per pixel).</summary>
    public float MouseSensitivity
    {
        get => _playerData.MovementSettings.MouseSensitivity;
        set
        {
            _playerData.MovementSettings.MouseSensitivity = value;
            _logger.Debug("GameplayOptions: MouseSensitivity={0}", value);
        }
    }
}
