using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Core.StateMachine;
using MineRPG.Entities.Player;
using MineRPG.Godot.World.Chunks;

namespace MineRPG.Game.Bootstrap.States;

/// <summary>
/// Game state for active gameplay. Manages tree pause on Pause/Resume.
/// Exit saves all dirty chunks and the player state before the scene is unloaded.
/// </summary>
public sealed class PlayingState : IState
{
    private readonly WorldMeta _worldMeta;
    private readonly string _worldSaveDirectory;
    private readonly SceneTree _tree;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PlayingState"/>.
    /// </summary>
    /// <param name="worldMeta">The metadata of the world being played.</param>
    /// <param name="worldSaveDirectory">Absolute path to this world's save directory.</param>
    /// <param name="tree">The Godot scene tree for pause control.</param>
    /// <param name="eventBus">The event bus for game events.</param>
    /// <param name="logger">Logger for state transition diagnostics.</param>
    public PlayingState(
        WorldMeta worldMeta,
        string worldSaveDirectory,
        SceneTree tree,
        IEventBus eventBus,
        ILogger logger)
    {
        _worldMeta = worldMeta;
        _worldSaveDirectory = worldSaveDirectory;
        _tree = tree;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Enter() => _logger.Info("PlayingState: Entered. World='{0}', Seed={1}", _worldMeta.Name, _worldMeta.Seed);

    /// <inheritdoc />
    public void Exit()
    {
        if (ServiceLocator.Instance.TryGet<ChunkAutosaveScheduler>(out ChunkAutosaveScheduler? autosave))
        {
            autosave.EnqueueAllDirtyChunks();
        }

        SavePlayerState();

        _logger.Info("PlayingState: Exit save complete.");
    }

    /// <inheritdoc />
    public void Tick(float deltaTime)
    {
    }

    /// <inheritdoc />
    public void Pause()
    {
        _tree.Paused = true;
        _eventBus.Publish(new GamePausedEvent { IsPaused = true });
        _logger.Debug("PlayingState: Paused.");
    }

    /// <inheritdoc />
    public void Resume()
    {
        _tree.Paused = false;
        _eventBus.Publish(new GamePausedEvent { IsPaused = false });
        _logger.Debug("PlayingState: Resumed.");
    }

    private void SavePlayerState()
    {
        if (!ServiceLocator.Instance.TryGet<PlayerData>(out PlayerData? playerData)
            || playerData is null)
        {
            _logger.Warning("PlayingState: Cannot save player - PlayerData not found.");
            return;
        }

        if (!ServiceLocator.Instance.TryGet<PlayerRepository>(out PlayerRepository? repository)
            || repository is null)
        {
            _logger.Warning("PlayingState: Cannot save player - PlayerRepository not found.");
            return;
        }

        PlayerSaveData saveData = new()
        {
            PositionX = playerData.PositionX,
            PositionY = playerData.PositionY,
            PositionZ = playerData.PositionZ,
            VelocityX = playerData.VelocityX,
            VelocityY = playerData.VelocityY,
            VelocityZ = playerData.VelocityZ,
            CameraYaw = playerData.CameraYaw,
            CameraPitch = playerData.CameraPitch,
            IsSprinting = playerData.IsSprinting,
        };

        repository.Save(_worldSaveDirectory, saveData);
    }
}
