using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;
using MineRPG.Game.Bootstrap.Debug;
using MineRPG.Game.Bootstrap.Settings;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Autoload node. First node initialized by Godot.
/// Sets up DataPath, creates the ServiceLocator, and registers minimal bootstrap
/// services (logger, EventBus, WorldRepository). Full world wiring happens later
/// when the player selects a world, via <see cref="GameStateOrchestrator"/>.
/// </summary>
public sealed partial class GameBootstrapper : Node
{
	/// <summary>
	/// Called when the node enters the scene tree. Initializes bootstrap services.
	/// </summary>
	public override void _Ready()
	{
		string dataRoot = ProjectSettings.GlobalizePath("res://Data");
		DataPath.SetRoot(dataRoot);

		ServiceLocator locator = new();
		ServiceLocator.SetInstance(locator);

		ConsoleLogger logger = new()
		{
			MinLevel = LogLevel.Debug,
		};
		locator.Register<ILogger>(logger);

		EventBus eventBus = new(logger);
		locator.Register<IEventBus>(eventBus);

		WorldRepository worldRepository = new(logger);
		locator.Register(worldRepository);

		JsonSettingsRepository settingsRepo = new(logger);
		locator.Register<ISettingsRepository>(settingsRepo);

		SettingsData settingsData = settingsRepo.Load();
		locator.Register(settingsData);

		KeybindApplicator.Apply(settingsData, logger);

#if DEBUG
		DebugInputRegistrar.RegisterAll(logger);
#endif

		logger.Info("GameBootstrapper: Bootstrap initialization complete.");
	}
}
