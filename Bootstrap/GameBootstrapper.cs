using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
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

    /// <summary>
    /// Called when the node enters the scene tree. Initializes all game systems.
    /// </summary>
    public override void _Ready()
    {
        string dataRoot = ProjectSettings.GlobalizePath("res://Data");
        DataPath.SetRoot(dataRoot);

        ServiceLocator locator = new ServiceLocator();
        ServiceLocator.SetInstance(locator);

        CompositionRoot.Wire(locator, _worldSeed, dataRoot);

        ILogger logger = locator.Get<ILogger>();
        logger.Info("GameBootstrapper: Initialization complete.");

        CallDeferred(MethodName.RegisterSceneReferences);
    }

    private void RegisterSceneReferences()
    {
        ServiceLocator locator = (ServiceLocator)ServiceLocator.Instance;
        ILogger logger = locator.Get<ILogger>();

        WorldNode? worldNode = GetTree().Root.FindChild("WorldNode", true, false) as WorldNode;
        if (worldNode is not null)
        {
            IVoxelRaycaster raycaster = locator.Get<IVoxelRaycaster>();
            PlayerData playerData = locator.Get<PlayerData>();
            BlockInteractionService blockInteraction = new BlockInteractionService(raycaster, worldNode, playerData, logger);
            locator.Register<IBlockInteractionService>(blockInteraction);

            logger.Info("GameBootstrapper: WorldNode and BlockInteractionService registered.");
        }
        else
        {
            logger.Warning("GameBootstrapper: WorldNode not found in scene tree.");
        }

        IEventBus eventBus = locator.Get<IEventBus>();
        eventBus.Publish(new GameInitializedEvent());
    }
}
