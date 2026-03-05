using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Core.StateMachine;

namespace MineRPG.Game.Bootstrap.States;

/// <summary>
/// Game state active while the initial chunk preload is in progress.
/// Subscribes to <see cref="WorldReadyEvent"/> and transitions to
/// <see cref="PlayingState"/> when preload completes.
/// The scene tree is NOT paused during loading — chunk workers must run.
/// </summary>
public sealed class LoadingState : IState
{
    private readonly WorldMeta _worldMeta;
    private readonly string _worldSaveDirectory;
    private readonly SceneTree _tree;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly IStateMachine _stateMachine;

    /// <summary>
    /// Initializes a new instance of <see cref="LoadingState"/>.
    /// </summary>
    /// <param name="worldMeta">The world metadata for the session being loaded.</param>
    /// <param name="worldSaveDirectory">Absolute path to the world's save directory.</param>
    /// <param name="tree">The Godot scene tree.</param>
    /// <param name="eventBus">Event bus for subscribing to world events.</param>
    /// <param name="logger">Logger for state transition diagnostics.</param>
    /// <param name="stateMachine">The state machine that owns this state.</param>
    public LoadingState(
        WorldMeta worldMeta,
        string worldSaveDirectory,
        SceneTree tree,
        IEventBus eventBus,
        ILogger logger,
        IStateMachine stateMachine)
    {
        _worldMeta = worldMeta;
        _worldSaveDirectory = worldSaveDirectory;
        _tree = tree;
        _eventBus = eventBus;
        _logger = logger;
        _stateMachine = stateMachine;
    }

    /// <inheritdoc />
    public void Enter()
    {
        _eventBus.Subscribe<WorldReadyEvent>(OnWorldReady);
        _logger.Info("LoadingState: Entered. Waiting for preload to complete.");
    }

    /// <inheritdoc />
    public void Exit()
    {
        _eventBus.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        _logger.Info("LoadingState: Exited.");
    }

    /// <inheritdoc />
    public void Tick(float deltaTime)
    {
    }

    private void OnWorldReady(WorldReadyEvent evt)
    {
        _logger.Info("LoadingState: WorldReadyEvent received — transitioning to PlayingState.");

        PlayingState playingState = new(
            _worldMeta,
            _worldSaveDirectory,
            _tree,
            _eventBus,
            _logger);

        _stateMachine.ChangeState(playingState);
    }
}
