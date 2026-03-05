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
/// Wires all world-specific dependency injections. Called by
/// <see cref="GameStateOrchestrator"/> when a world is loaded.
/// Order matters: registries before systems that depend on them.
/// </summary>
public static class CompositionRoot
{
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
        CaveCarver caveCarver = new(terrainSampler);

        WorldGenerator worldGenerator = new(blockRegistry, terrainSampler, caveCarver);
        locator.Register<IWorldGenerator>(worldGenerator);

        ChunkMeshBuilder meshBuilder = new(blockRegistry);
        locator.Register<IChunkMeshBuilder>(meshBuilder);

        VoxelRaycaster raycaster = new(blockRegistry, chunkManager);
        locator.Register<IVoxelRaycaster>(raycaster);

        ChunkSerializer chunkSerializer = new();
        locator.Register<IChunkSerializer>(chunkSerializer);

        string saveRoot = WorldRepository.GetSavePath(savesRoot, worldSeed);
        FileChunkStorage chunkStorage = new(saveRoot);
        locator.Register<IChunkStorage>(chunkStorage);

        ChunkPersistenceService persistence = new(chunkSerializer, chunkStorage, logger);
        locator.Register<ChunkPersistenceService>(persistence);

        PlayerMovementSettings movementSettings = TryLoadMovementSettings(dataLoader, logger);
        PlayerData playerData = new(movementSettings);
        locator.Register<PlayerData>(playerData);

        PerformanceMonitor performanceMonitor = new();
        locator.Register<PerformanceMonitor>(performanceMonitor);

        DebugDataProvider debugDataProvider = new(playerData, chunkManager, biomeSelector, performanceMonitor);
        locator.Register<IDebugDataProvider>(debugDataProvider);

        HotbarController hotbarController = new(playerData);
        locator.Register<IHotbarController>(hotbarController);

        OptionsProvider optionsProvider = new(playerData, logger);
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
}
