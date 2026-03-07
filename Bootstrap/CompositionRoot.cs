using System;
using System.IO;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;
using MineRPG.Core.Registry;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.Godot.World.Chunks;
using MineRPG.Godot.World.Rendering;
using MineRPG.Godot.World.Storage;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Chunks.Serialization;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

using MineRPG.Game.Bootstrap.Debug;
using MineRPG.Game.Bootstrap.Gameplay;
using MineRPG.Game.Bootstrap.Settings;
using MineRPG.Game.Bootstrap.Validation;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Wires all world-specific dependency injections. Called by
/// <see cref="GameStateOrchestrator"/> when a world is loaded.
/// Order matters: registries before systems that depend on them.
/// </summary>
public static class CompositionRoot
{
    private const int PreloadRadius = 3;
    private const int PreloadDiameter = PreloadRadius * 2 + 1;
    private const int PreloadChunkCount = PreloadDiameter * PreloadDiameter;

    /// <summary>
    /// Wires all world-specific services into the service locator.
    /// </summary>
    /// <param name="locator">The service locator to register services into.</param>
    /// <param name="worldMeta">The metadata of the world being loaded.</param>
    /// <param name="dataRoot">The root path for data files.</param>
    /// <param name="savesRoot">The root path for world saves.</param>
    public static void Wire(ServiceLocator locator, WorldMeta worldMeta, string dataRoot, string savesRoot)
    {
        ILogger logger = locator.Get<ILogger>();
        IEventBus eventBus = locator.Get<IEventBus>();
        int worldSeed = worldMeta.Seed;

        JsonDataLoader dataLoader = new(logger, dataRoot);
        locator.Register<IDataLoader>(dataLoader);

        BlockRegistry blockRegistry = new(dataLoader, logger);
        locator.Register<BlockRegistry>(blockRegistry);

        ImageTexture atlasTexture = TextureAtlasBuilder.Build(blockRegistry.AtlasLayout, logger);
        TextureAtlasLayout layout = blockRegistry.AtlasLayout;
        Shader shader = GD.Load<Shader>("res://Resources/Shaders/voxel_terrain.gdshader");
        ShaderMaterial terrainMaterial = new() { Shader = shader };
        terrainMaterial.SetShaderParameter("atlas_texture", atlasTexture);
        terrainMaterial.SetShaderParameter("tile_size",
            new Vector2(
                layout.Columns > 0 ? 1f / layout.Columns : 1f,
                layout.Rows > 0 ? 1f / layout.Rows : 1f));
        int atlasWidth = atlasTexture.GetWidth();
        int atlasHeight = atlasTexture.GetHeight();
        terrainMaterial.SetShaderParameter("atlas_texel_size",
            new Vector2(
                atlasWidth > 0 ? 1f / atlasWidth : 1f,
                atlasHeight > 0 ? 1f / atlasHeight : 1f));
        ChunkNode.SetSharedMaterial(terrainMaterial);

        Shader waterShader = GD.Load<Shader>("res://Resources/Shaders/liquid.gdshader");
        ShaderMaterial waterMaterial = new() { Shader = waterShader };
        waterMaterial.SetShaderParameter("atlas_texture", atlasTexture);
        waterMaterial.SetShaderParameter("tile_size",
            new Vector2(
                layout.Columns > 0 ? 1f / layout.Columns : 1f,
                layout.Rows > 0 ? 1f / layout.Rows : 1f));
        waterMaterial.SetShaderParameter("atlas_texel_size",
            new Vector2(
                atlasWidth > 0 ? 1f / atlasWidth : 1f,
                atlasHeight > 0 ? 1f / atlasHeight : 1f));
        ChunkNode.SetSharedWaterMaterial(waterMaterial);

        ChunkManager chunkManager = new(eventBus, logger);
        locator.Register<IChunkManager>(chunkManager);

        IReadOnlyList<BiomeDefinition> biomes = dataLoader.LoadAll<BiomeDefinition>("Biomes");
        BiomeBlockResolver.ResolveAll(biomes, blockRegistry, logger);
        BiomeSelector biomeSelector = new(biomes, worldSeed);
        locator.Register<BiomeSelector>(biomeSelector);

        TerrainSampler terrainSampler = new(biomeSelector, worldSeed);
        locator.Register<TerrainSampler>(terrainSampler);

        SpawnPositionResolver spawnResolver = new(terrainSampler);
        locator.Register<SpawnPositionResolver>(spawnResolver);

        CaveCarver caveCarver = new(terrainSampler);

        WorldGenerator worldGenerator = new(blockRegistry, terrainSampler, caveCarver);
        locator.Register<IWorldGenerator>(worldGenerator);

        ChunkMeshBuilder meshBuilder = new(blockRegistry);
        locator.Register<IChunkMeshBuilder>(meshBuilder);

        VoxelRaycaster raycaster = new(blockRegistry, chunkManager);
        locator.Register<IVoxelRaycaster>(raycaster);

        ChunkSerializer chunkSerializer = new();
        locator.Register<IChunkSerializer>(chunkSerializer);

        string saveRoot = WorldRepository.GetSavePath(savesRoot, worldMeta.WorldId);
        FileChunkStorage chunkStorage = new(saveRoot);
        locator.Register<IChunkStorage>(chunkStorage);

        ChunkPersistenceService persistence = new(chunkSerializer, chunkStorage, logger);
        locator.Register<ChunkPersistenceService>(persistence);

        PlayerMovementSettings movementSettings = TryLoadMovementSettings(dataLoader, logger);
        PlayerData playerData = new(movementSettings);
        locator.Register<PlayerData>(playerData);

        MiningState miningState = new();
        locator.Register<MiningState>(miningState);

        PlayerRepository playerRepository = new(logger);
        locator.Register<PlayerRepository>(playerRepository);

        bool playerSaveExists = TryRestorePlayerSave(playerData, saveRoot, playerRepository, logger);

        if (!playerSaveExists)
        {
            int spawnY = spawnResolver.ComputeSpawnY();
            playerData.PositionX = SpawnPositionResolver.SpawnWorldX;
            playerData.PositionY = spawnY;
            playerData.PositionZ = SpawnPositionResolver.SpawnWorldZ;
            logger.Info("CompositionRoot: New world — spawn Y computed as {0}.", spawnY);
        }

        PreloadProgress preloadProgress = new(PreloadChunkCount);
        locator.Register<PreloadProgress>(preloadProgress);

        PerformanceMonitor performanceMonitor = new();
        locator.Register<PerformanceMonitor>(performanceMonitor);

        PipelineMetrics pipelineMetrics = new();
        locator.Register<PipelineMetrics>(pipelineMetrics);

        OptimizationFlags optimizationFlags = new();
        locator.Register<OptimizationFlags>(optimizationFlags);

        DebugDataProvider debugDataProvider = new(playerData, chunkManager, terrainSampler, performanceMonitor);
        locator.Register<IDebugDataProvider>(debugDataProvider);

        ItemRegistry itemRegistry = new();
        IReadOnlyList<ItemDefinition> itemDefinitions = dataLoader.LoadAll<ItemDefinition>("Items");

        for (int i = 0; i < itemDefinitions.Count; i++)
        {
            itemRegistry.Register(itemDefinitions[i]);
        }

        itemRegistry.Freeze();
        locator.Register<ItemRegistry>(itemRegistry);

        logger.Info("CompositionRoot: Loaded {0} item definitions.", itemDefinitions.Count);

        TagRegistry tagRegistry = new();
        IReadOnlyList<TagDefinition> tagDefinitions = dataLoader.LoadAll<TagDefinition>("Tags");

        for (int i = 0; i < tagDefinitions.Count; i++)
        {
            tagRegistry.Register(tagDefinitions[i]);
        }

        tagRegistry.Freeze();
        locator.Register<TagRegistry>(tagRegistry);

        logger.Info("CompositionRoot: Loaded {0} tag definitions.", tagDefinitions.Count);

        RegistryValidator.Validate(blockRegistry, itemRegistry, tagRegistry, logger);

        PlayerInventory playerInventory = new(itemRegistry);
        playerData.Inventory = playerInventory;
        locator.Register<PlayerInventory>(playerInventory);

        CursorItemHolder cursorItemHolder = new();
        locator.Register<CursorItemHolder>(cursorItemHolder);

        HotbarController hotbarController = new(playerData, itemRegistry);
        locator.Register<IHotbarController>(hotbarController);
        locator.Register<HotbarController>(hotbarController);

        ISettingsRepository settingsRepo = locator.Get<ISettingsRepository>();
        SettingsData settingsData = locator.Get<SettingsData>();
        OptionsProvider optionsProvider = new(playerData, settingsRepo, settingsData, logger);
        locator.Register<IOptionsProvider>(optionsProvider);

        logger.Info(
            "CompositionRoot: All services wired. World='{0}', Seed={1}, SaveRoot={2}",
            worldMeta.Name, worldSeed, saveRoot);
    }

    private static PlayerMovementSettings TryLoadMovementSettings(IDataLoader loader, ILogger logger)
    {
        try
        {
            return loader.Load<PlayerMovementSettings>("Player/movement_settings.json");
        }
        catch (Exception ex)
        {
            logger.Warning("Could not load movement_settings.json — using defaults. {0}", ex.Message);
            return new PlayerMovementSettings();
        }
    }

    private static bool TryRestorePlayerSave(
        PlayerData playerData,
        string saveRoot,
        PlayerRepository playerRepository,
        ILogger logger)
    {
        if (!playerRepository.TryLoad(saveRoot, out PlayerSaveData? save) || save is null)
        {
            logger.Info("CompositionRoot: No player save found — using spawn defaults.");
            return false;
        }

        playerData.PositionX = save.PositionX;
        playerData.PositionY = save.PositionY;
        playerData.PositionZ = save.PositionZ;
        playerData.VelocityX = save.VelocityX;
        playerData.VelocityY = save.VelocityY;
        playerData.VelocityZ = save.VelocityZ;
        playerData.CameraYaw = save.CameraYaw;
        playerData.CameraPitch = save.CameraPitch;
        playerData.IsSprinting = save.IsSprinting;
        playerData.SelectedBlockId = save.SelectedBlockId;

        logger.Info(
            "CompositionRoot: Restored player save — position ({0:F1}, {1:F1}, {2:F1}).",
            save.PositionX, save.PositionY, save.PositionZ);
        return true;
    }
}
