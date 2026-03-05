using System;
using System.IO;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Lifecycle;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;
using MineRPG.Core.StateMachine;

using MineRPG.Godot.UI.Screens;

namespace MineRPG.Game.Bootstrap.States;

/// <summary>
/// Autoload node that owns the top-level game state machine.
/// Drives scene transitions: MainMenu -> Main (gameplay) -> MainMenu.
/// Subscribes to WorldLoadRequestedEvent, ReturnToMainMenuEvent, GameQuitRequestedEvent.
/// Implements <see cref="IGameStateController"/> so UI nodes can request pause/resume.
/// </summary>
public sealed partial class GameStateOrchestrator : Node, IGameStateController
{
    private const string MainMenuScenePath = "res://Scenes/MainMenu.tscn";
    private const string GameplayScenePath = "res://Scenes/Main.tscn";

    private StateMachine _stateMachine = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private string _savesRoot = string.Empty;
    private string _dataRoot = string.Empty;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();

        _dataRoot = ProjectSettings.GlobalizePath("res://Data");
        _savesRoot = Path.Combine(
            Path.GetDirectoryName(_dataRoot) ?? _dataRoot, "Saves");

        _stateMachine = new StateMachine(_logger);
        ServiceLocator.Instance.Register<IStateMachine>(_stateMachine);
        ServiceLocator.Instance.Register<IGameStateController>(this);

        SubscribeEvents();

        // Start in the main menu (project.godot main_scene is already MainMenu.tscn)
        _stateMachine.ChangeState(new MainMenuState());

        _logger.Info("GameStateOrchestrator ready. Starting in MainMenuState.");
    }

    /// <inheritdoc />
    public override void _ExitTree() => UnsubscribeEvents();

    /// <summary>
    /// Called by PlayerNode when ESC is pressed during gameplay.
    /// Pushes the PausedState onto the state machine.
    /// </summary>
    public void RequestPause()
    {
        if (_stateMachine.CurrentState is PlayingState)
        {
            _stateMachine.PushState(new PausedState());
        }
    }

    /// <summary>
    /// Called by PauseMenuNode "Resume" button.
    /// Pops the PausedState, resuming PlayingState.
    /// </summary>
    public void RequestResume()
    {
        if (_stateMachine.CurrentState is PausedState)
        {
            _stateMachine.PopState();
        }
    }

    private void LoadMainMenuScene()
    {
        Error error = GetTree().ChangeSceneToFile(MainMenuScenePath);

        if (error != Error.Ok)
        {
            _logger.Error(
                "GameStateOrchestrator: Failed to load MainMenu scene — {0}",
                error.ToString());
        }
    }

    private void OnWorldLoadRequested(WorldLoadRequestedEvent evt)
    {
        WorldMeta meta = evt.Meta;
        meta.LastPlayedAt = DateTime.UtcNow;

        // Persist updated LastPlayedAt
        WorldRepository repository = ServiceLocator.Instance.Get<WorldRepository>();
        repository.SaveMeta(_savesRoot, meta);

        // Unsubscribe from the old bus before replacing it
        UnsubscribeEvents();

        // Capture global settings from the old locator before replacing it
        ISettingsRepository settingsRepo = ServiceLocator.Instance.Get<ISettingsRepository>();
        SettingsData settingsData = ServiceLocator.Instance.Get<SettingsData>();

        // Create a fresh ServiceLocator for the new world
        ServiceLocator freshLocator = new();
        ServiceLocator.SetInstance(freshLocator);

        // Re-register bootstrap-level services
        freshLocator.Register<ILogger>(_logger);

        EventBus freshEventBus = new(_logger);
        freshLocator.Register<IEventBus>(freshEventBus);
        _eventBus = freshEventBus;

        // Re-register orchestrator, state machine, and repository
        freshLocator.Register<IGameStateController>(this);
        freshLocator.Register<IStateMachine>(_stateMachine);

        // Carry over global settings (registered at bootstrap, survive world reloads)
        freshLocator.Register<ISettingsRepository>(settingsRepo);
        freshLocator.Register(settingsData);

        WorldRepository freshRepository = new(_logger);
        freshLocator.Register(freshRepository);

        // Wire all world-specific services
        CompositionRoot.Wire(freshLocator, meta, _dataRoot, _savesRoot);

        // Re-subscribe on fresh bus
        SubscribeEvents();

        // Transition to LoadingState — it will switch to PlayingState once chunks are preloaded
        string worldSaveDirectory = WorldRepository.GetSavePath(_savesRoot, meta.WorldId);
        LoadingState loadingState = new(
            meta, worldSaveDirectory, GetTree(), _eventBus, _logger, _stateMachine);
        _stateMachine.ChangeState(loadingState);

        // Load the gameplay scene
        Error error = GetTree().ChangeSceneToFile(GameplayScenePath);

        if (error != Error.Ok)
        {
            _logger.Error(
                "GameStateOrchestrator: Failed to load gameplay scene — {0}",
                error.ToString());
            return;
        }

        // Defer adding the loading screen until after the scene is in the tree
        Callable.From(AddLoadingScreen).CallDeferred();
    }

    private void OnReturnToMainMenu(ReturnToMainMenuEvent evt)
    {
        // Pop PausedState first if we're paused
        if (_stateMachine.CurrentState is PausedState)
        {
            _stateMachine.PopState();
        }

        // PlayingState.Exit() saves all dirty chunks
        _stateMachine.ChangeState(new MainMenuState());

        // Unpause the tree (in case the pop didn't fully clean up)
        GetTree().Paused = false;

        // Load main menu scene
        Error error = GetTree().ChangeSceneToFile(MainMenuScenePath);

        if (error != Error.Ok)
        {
            _logger.Error(
                "GameStateOrchestrator: Failed to load MainMenu scene — {0}",
                error.ToString());
        }
    }

    private void OnGameQuitRequested(GameQuitRequestedEvent evt)
    {
        if (_stateMachine.CurrentState is PlayingState or PausedState or LoadingState)
        {
            // Pop PausedState if present
            if (_stateMachine.CurrentState is PausedState)
            {
                _stateMachine.PopState();
            }

            // Transition through the state machine so PlayingState.Exit() fires properly
            _stateMachine.ChangeState(new MainMenuState());
        }

        _logger.Info("GameStateOrchestrator: Quit requested.");
        GetTree().Quit();
    }

    private void AddLoadingScreen()
    {
        LoadingScreenNode loadingScreen = new();
        loadingScreen.Name = "LoadingScreen";
        GetTree().Root.AddChild(loadingScreen);
        _logger.Info("GameStateOrchestrator: LoadingScreenNode added to scene tree.");
    }

    private void SubscribeEvents()
    {
        _eventBus.Subscribe<WorldLoadRequestedEvent>(OnWorldLoadRequested);
        _eventBus.Subscribe<ReturnToMainMenuEvent>(OnReturnToMainMenu);
        _eventBus.Subscribe<GameQuitRequestedEvent>(OnGameQuitRequested);
    }

    private void UnsubscribeEvents()
    {
        _eventBus.Unsubscribe<WorldLoadRequestedEvent>(OnWorldLoadRequested);
        _eventBus.Unsubscribe<ReturnToMainMenuEvent>(OnReturnToMainMenu);
        _eventBus.Unsubscribe<GameQuitRequestedEvent>(OnGameQuitRequested);
    }
}
