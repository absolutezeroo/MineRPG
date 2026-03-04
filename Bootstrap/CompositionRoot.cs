using System;
using System.IO;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Wires all dependency injections. Called once by GameBootstrapper.
/// Order matters: registries before systems that depend on them.
/// </summary>
public static class CompositionRoot
{
    /// <summary>
    /// Wires all services into the service locator.
    /// </summary>
    /// <param name="locator">The service locator to register services into.</param>
    /// <param name="worldSeed">The world generation seed.</param>
    /// <param name="dataRoot">The root path for data files.</param>
    public static void Wire(ServiceLocator locator, int worldSeed, string dataRoot)
    {
        ConsoleLogger logger = new ConsoleLogger { MinLevel = LogLevel.Debug };
        locator.Register<ILogger>(logger);

        EventBus eventBus = new EventBus(logger);
        locator.Register<IEventBus>(eventBus);

        JsonDataLoader dataLoader = new JsonDataLoader(logger, dataRoot);
        locator.Register<IDataLoader>(dataLoader);

        BlockRegistry blockRegistry = new BlockRegistry(dataLoader, logger);
        locator.Register<BlockRegistry>(blockRegistry);

        ImageTexture atlasTexture = TextureAtlasBuilder.Build(blockRegistry.AtlasLayout, logger);
        TextureAtlasLayout layout = blockRegistry.AtlasLayout;
        Shader shader = GD.Load<Shader>("res://Resources/Shaders/voxel_terrain.gdshader");
        ShaderMaterial terrainMaterial = new ShaderMaterial { Shader = shader };
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
        ShaderMaterial waterMaterial = new ShaderMaterial { Shader = waterShader };
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

        ChunkManager chunkManager = new ChunkManager(eventBus, logger);
        locator.Register<IChunkManager>(chunkManager);

        IReadOnlyList<BiomeDefinition> biomes = dataLoader.LoadAll<BiomeDefinition>("Biomes");
        BiomeBlockResolver.ResolveAll(biomes, blockRegistry, logger);
        BiomeSelector biomeSelector = new BiomeSelector(biomes, worldSeed);
        locator.Register<BiomeSelector>(biomeSelector);

        TerrainSampler terrainSampler = new TerrainSampler(biomeSelector, worldSeed);
        CaveCarver caveCarver = new CaveCarver(terrainSampler);

        WorldGenerator worldGenerator = new WorldGenerator(blockRegistry, terrainSampler, caveCarver);
        locator.Register<IWorldGenerator>(worldGenerator);

        ChunkMeshBuilder meshBuilder = new ChunkMeshBuilder(blockRegistry);
        locator.Register<IChunkMeshBuilder>(meshBuilder);

        VoxelRaycaster raycaster = new VoxelRaycaster(blockRegistry, chunkManager);
        locator.Register<IVoxelRaycaster>(raycaster);

        ChunkSerializer chunkSerializer = new ChunkSerializer();
        locator.Register<IChunkSerializer>(chunkSerializer);

        string saveRoot = Path.Combine(Path.GetDirectoryName(dataRoot) ?? dataRoot, "Saves", $"world_{worldSeed}");
        FileChunkStorage chunkStorage = new FileChunkStorage(saveRoot);
        locator.Register<IChunkStorage>(chunkStorage);

        ChunkPersistenceService persistence = new ChunkPersistenceService(chunkSerializer, chunkStorage, logger);
        locator.Register<ChunkPersistenceService>(persistence);

        PlayerMovementSettings movementSettings = TryLoadMovementSettings(dataLoader, logger);
        PlayerData playerData = new PlayerData(movementSettings);
        locator.Register<PlayerData>(playerData);

        PerformanceMonitor performanceMonitor = new PerformanceMonitor();
        locator.Register<PerformanceMonitor>(performanceMonitor);

        DebugDataProvider debugDataProvider = new DebugDataProvider(playerData, chunkManager, biomeSelector, performanceMonitor);
        locator.Register<IDebugDataProvider>(debugDataProvider);

        HotbarController hotbarController = new HotbarController(playerData);
        locator.Register<IHotbarController>(hotbarController);

        logger.Info("CompositionRoot: All services wired. Seed={0}, SaveRoot={1}", worldSeed, saveRoot);
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
}
