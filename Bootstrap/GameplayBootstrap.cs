using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.Godot.World.Rendering;
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

        HotbarController hotbarController = ServiceLocator.Instance.Get<HotbarController>();

        BlockInteractionService blockInteraction = new(
            raycaster, worldNode, blockRegistry, hotbarController,
            playerData, miningState, eventBus, logger);

        ServiceLocator.Instance.Register<IBlockInteractionService>(blockInteraction);

        WireFrustumCulling(worldNode, logger);
        WireOcclusionCuller(worldNode, logger);
        WireRegionManager(worldNode, logger);
        WireClipmapRenderer(worldNode, logger);

        eventBus.Publish(new GameInitializedEvent());

        logger.Info("GameplayBootstrap: Scene references registered.");
    }

    private static void WireFrustumCulling(WorldNode worldNode, ILogger logger)
    {
        FrustumCullingSystem frustumCulling = new();
        frustumCulling.Name = "FrustumCullingSystem";
        worldNode.AddChild(frustumCulling);

        Camera3D? camera = FindCamera(worldNode);

        if (camera is not null)
        {
            frustumCulling.SetCamera(camera);
        }
        else
        {
            logger.Warning("GameplayBootstrap: Camera3D not found for FrustumCullingSystem.");
        }

        frustumCulling.SetWorldNode(worldNode);

        if (ServiceLocator.Instance.TryGet<OptimizationFlags>(out OptimizationFlags? flags)
            && flags is not null)
        {
            frustumCulling.SetOptimizationFlags(flags);
        }

        ServiceLocator.Instance.Register(frustumCulling);
        worldNode.SetFrustumCulling(frustumCulling);

        logger.Info("GameplayBootstrap: FrustumCullingSystem wired.");
    }

    private static void WireOcclusionCuller(WorldNode worldNode, ILogger logger)
    {
        if (!ServiceLocator.Instance.TryGet<FrustumCullingSystem>(out FrustumCullingSystem? frustumCulling)
            || frustumCulling is null)
        {
            return;
        }

        OcclusionCuller occlusionCuller = new();
        ServiceLocator.Instance.Register(occlusionCuller);
        frustumCulling.SetOcclusionCuller(occlusionCuller);
        worldNode.SetOcclusionCuller(occlusionCuller);

        logger.Info("GameplayBootstrap: OcclusionCuller wired.");
    }

    private static void WireRegionManager(WorldNode worldNode, ILogger logger)
    {
        if (!ServiceLocator.Instance.TryGet<OptimizationFlags>(out OptimizationFlags? flags)
            || flags is null || !flags.DrawCallBatchingEnabled)
        {
            return;
        }

        RegionManager regionManager = new();
        regionManager.Name = "RegionManager";
        worldNode.AddChild(regionManager);
        ServiceLocator.Instance.Register(regionManager);

        logger.Info("GameplayBootstrap: RegionManager wired.");
    }

    private static void WireClipmapRenderer(WorldNode worldNode, ILogger logger)
    {
        if (!ServiceLocator.Instance.TryGet<OptimizationFlags>(out OptimizationFlags? flags)
            || flags is null || !flags.ClipmapEnabled)
        {
            return;
        }

        if (!ServiceLocator.Instance.TryGet<MineRPG.World.Generation.TerrainSampler>(
                out MineRPG.World.Generation.TerrainSampler? terrainSampler)
            || terrainSampler is null)
        {
            logger.Warning("GameplayBootstrap: TerrainSampler not found, skipping ClipmapRenderer.");
            return;
        }

        ClipmapRenderer clipmap = new();
        clipmap.Name = "ClipmapRenderer";
        worldNode.AddChild(clipmap);

        World.Terrain.ClipmapGenerator.HeightSampler heightSampler =
            (float worldX, float worldZ) =>
            {
                MineRPG.World.Generation.TerrainColumn column =
                    terrainSampler.SampleColumn((int)worldX, (int)worldZ);
                return column.SurfaceY;
            };

        World.Terrain.ClipmapGenerator.ColorSampler colorSampler =
            (float worldX, float worldZ, out float r, out float g, out float b) =>
            {
                r = 0.486f;
                g = 0.741f;
                b = 0.420f;
            };

        clipmap.Configure(new World.Terrain.ClipmapConfig(), heightSampler, colorSampler);
        ServiceLocator.Instance.Register(clipmap);

        logger.Info("GameplayBootstrap: ClipmapRenderer wired.");
    }

    private static Camera3D? FindCamera(WorldNode worldNode)
    {
        Camera3D? camera = worldNode.GetViewport()?.GetCamera3D();

        if (camera is not null)
        {
            return camera;
        }

        Node? playerNode = worldNode.GetTree().Root.FindChild("PlayerNode", true, false);

        if (playerNode is not null)
        {
            camera = playerNode.FindChild("Camera3D", true, false) as Camera3D;
        }

        return camera;
    }
}
