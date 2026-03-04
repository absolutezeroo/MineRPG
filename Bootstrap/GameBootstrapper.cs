using Godot;
using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.Godot.World;
using MineRPG.World.Spatial;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Autoload node. First node initialized by Godot.
/// Sets up DataPath, wires DI via CompositionRoot, then registers scene references.
/// </summary>
public sealed partial class GameBootstrapper : Node
{
	[Export] private int _worldSeed = 12345;

	public override void _Ready()
	{
		var dataRoot = ProjectSettings.GlobalizePath("res://Data");
		DataPath.SetRoot(dataRoot);

		var locator = new ServiceLocator();
		ServiceLocator.SetInstance(locator);

		CompositionRoot.Wire(locator, _worldSeed, dataRoot);

		var logger = locator.Get<ILogger>();
		logger.Info("GameBootstrapper: Initialization complete.");

		CallDeferred(MethodName.RegisterSceneReferences);
	}

	private void RegisterSceneReferences()
	{
		var locator = (ServiceLocator)ServiceLocator.Instance;
		var logger = locator.Get<ILogger>();

		var worldNode = GetTree().Root.FindChild("WorldNode", true, false) as WorldNode;
		if (worldNode is not null)
		{
			var raycaster = locator.Get<IVoxelRaycaster>();
			var blockInteraction = new BlockInteractionService(raycaster, worldNode);
			locator.Register<IBlockInteractionService>(blockInteraction);

			logger.Info("GameBootstrapper: WorldNode and BlockInteractionService registered.");
		}
		else
		{
			logger.Warning("GameBootstrapper: WorldNode not found in scene tree.");
		}

		var eventBus = locator.Get<IEventBus>();
		eventBus.Publish(new GameInitializedEvent());
	}
}
