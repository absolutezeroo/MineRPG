using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.World.Blocks;
using MineRPG.World.Spatial;

using MineRPG.Game.Bootstrap.Gameplay;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Attached to the gameplay scene root (Main.tscn). Registers scene-dependent
/// services after all nodes have had their _Ready() called.
/// Replaces the old GameBootstrapper.RegisterSceneReferences() logic.
/// </summary>
public sealed partial class GameplayBootstrap : Node
{
    /// <inheritdoc />
    public override void _Ready() => CallDeferred(MethodName.RegisterSceneReferences);

    private void RegisterSceneReferences()
    {
        ILogger logger = ServiceLocator.Instance.Get<ILogger>();

        if (GetTree().Root.FindChild("WorldNode", true, false) is not WorldNode worldNode)
        {
            logger.Warning("GameplayBootstrap: WorldNode not found in scene tree.");
            return;
        }

        IVoxelRaycaster raycaster = ServiceLocator.Instance.Get<IVoxelRaycaster>();
        BlockRegistry blockRegistry = ServiceLocator.Instance.Get<BlockRegistry>();
        PlayerData playerData = ServiceLocator.Instance.Get<PlayerData>();
        MiningState miningState = ServiceLocator.Instance.Get<MiningState>();
        IEventBus eventBus = ServiceLocator.Instance.Get<IEventBus>();

        BlockInteractionService blockInteraction = new(
            raycaster, worldNode, blockRegistry, playerData,
            miningState, eventBus, equippedTool: null, logger);

        ServiceLocator.Instance.Register<IBlockInteractionService>(blockInteraction);

        eventBus.Publish(new GameInitializedEvent());

        logger.Info("GameplayBootstrap: Scene references registered.");
    }
}
