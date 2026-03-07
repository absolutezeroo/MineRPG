using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Game.Bootstrap.Input;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Handles mining and block placement input for the player.
/// Lazily resolves IBlockInteractionService from the ServiceLocator.
/// </summary>
internal sealed class PlayerInteractionController
{
    private readonly PlayerData _playerData;
    private readonly Camera3D _camera;
    private readonly ILogger _logger;

    private IBlockInteractionService? _blockInteraction;

    /// <summary>
    /// Creates an interaction controller for the given player and camera.
    /// </summary>
    /// <param name="playerData">Player data containing selected block and settings.</param>
    /// <param name="camera">The camera used for ray origin and direction.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="blockInteraction">Optional pre-resolved interaction service.</param>
    public PlayerInteractionController(
        PlayerData playerData,
        Camera3D camera,
        ILogger logger,
        IBlockInteractionService? blockInteraction)
    {
        _playerData = playerData;
        _camera = camera;
        _logger = logger;
        _blockInteraction = blockInteraction;
    }

    /// <summary>
    /// Ticks mining logic each physics frame. Starts or continues mining
    /// when attack is held, cancels when released.
    /// </summary>
    /// <param name="deltaTime">Physics frame delta in seconds.</param>
    public void TickMiningInput(float deltaTime)
    {
        if (!ResolveBlockInteraction())
        {
            return;
        }

        if (Input.IsActionPressed(InputActions.Attack))
        {
            Vector3 origin = _camera.GlobalPosition;
            Vector3 forward = -_camera.GlobalTransform.Basis.Z;
            float range = _playerData.MovementSettings.InteractionRange;

            _blockInteraction!.TickMining(
                origin.X, origin.Y, origin.Z,
                forward.X, forward.Y, forward.Z,
                range, deltaTime);
        }
        else
        {
            _blockInteraction!.CancelMining();
        }
    }

    /// <summary>
    /// Attempts to place a block at the position the player is looking at.
    /// </summary>
    public void TryPlaceBlock()
    {
        if (!ResolveBlockInteraction())
        {
            return;
        }

        Vector3 origin = _camera.GlobalPosition;
        Vector3 forward = -_camera.GlobalTransform.Basis.Z;
        float range = _playerData.MovementSettings.InteractionRange;

        _blockInteraction!.TryPlaceBlock(
            origin.X, origin.Y, origin.Z,
            forward.X, forward.Y, forward.Z,
            range, _playerData.SelectedBlockId);
    }

    /// <summary>
    /// Lazily resolves IBlockInteractionService from the ServiceLocator.
    /// Needed because GameBootstrapper registers it via CallDeferred,
    /// which runs after all nodes have had their _Ready() called.
    /// </summary>
    private bool ResolveBlockInteraction()
    {
        if (_blockInteraction is not null)
        {
            return true;
        }

        if (ServiceLocator.Instance.TryGet<IBlockInteractionService>(out IBlockInteractionService? service))
        {
            _blockInteraction = service;
            _logger.Info("PlayerNode: IBlockInteractionService resolved (lazy).");
            return true;
        }

        return false;
    }
}
