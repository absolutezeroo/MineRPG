using Godot;
using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
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
    public static void Wire(ServiceLocator locator, int worldSeed, string dataRoot)
    {
        var logger = new ConsoleLogger { MinLevel = LogLevel.Debug };
        locator.Register<ILogger>(logger);

        var eventBus = new EventBus(logger);
        locator.Register<IEventBus>(eventBus);

        var dataLoader = new JsonDataLoader(logger, dataRoot);
        locator.Register<IDataLoader>(dataLoader);

        var blockRegistry = new BlockRegistry(dataLoader, logger);
        locator.Register<BlockRegistry>(blockRegistry);

        var atlasTexture = TextureAtlasBuilder.Build(blockRegistry.AtlasLayout, logger);
        var layout = blockRegistry.AtlasLayout;
        var shader = GD.Load<Shader>("res://Resources/Shaders/voxel_terrain.gdshader");
        var terrainMaterial = new ShaderMaterial { Shader = shader };
        terrainMaterial.SetShaderParameter("atlas_texture", atlasTexture);
        terrainMaterial.SetShaderParameter("tile_size",
            new Vector2(
                layout.Columns > 0 ? 1f / layout.Columns : 1f,
                layout.Rows > 0 ? 1f / layout.Rows : 1f));
        var atlasWidth = atlasTexture.GetWidth();
        var atlasHeight = atlasTexture.GetHeight();
        terrainMaterial.SetShaderParameter("atlas_texel_size",
            new Vector2(
                atlasWidth > 0 ? 1f / atlasWidth : 1f,
                atlasHeight > 0 ? 1f / atlasHeight : 1f));
        ChunkNode.SetSharedMaterial(terrainMaterial);

        var waterShader = GD.Load<Shader>("res://Resources/Shaders/liquid.gdshader");
        var waterMaterial = new ShaderMaterial { Shader = waterShader };
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

        var chunkManager = new ChunkManager(eventBus, logger);
        locator.Register<IChunkManager>(chunkManager);

        var biomes = dataLoader.LoadAll<BiomeDefinition>("Biomes");
        BiomeBlockResolver.ResolveAll(biomes, blockRegistry, logger);
        var biomeSelector = new BiomeSelector(biomes, worldSeed);
        locator.Register<BiomeSelector>(biomeSelector);

        var terrainSampler = new TerrainSampler(biomeSelector, worldSeed);
        var caveCarver = new CaveCarver(terrainSampler);

        var worldGenerator = new WorldGenerator(blockRegistry, terrainSampler, caveCarver);
        locator.Register<IWorldGenerator>(worldGenerator);

        var meshBuilder = new ChunkMeshBuilder(blockRegistry);
        locator.Register<IChunkMeshBuilder>(meshBuilder);

        var raycaster = new VoxelRaycaster(blockRegistry, chunkManager);
        locator.Register<IVoxelRaycaster>(raycaster);

        var chunkSerializer = new ChunkSerializer();
        locator.Register<IChunkSerializer>(chunkSerializer);

        var saveRoot = Path.Combine(Path.GetDirectoryName(dataRoot) ?? dataRoot, "Saves", $"world_{worldSeed}");
        var chunkStorage = new FileChunkStorage(saveRoot);
        locator.Register<IChunkStorage>(chunkStorage);

        var persistence = new ChunkPersistenceService(chunkSerializer, chunkStorage, logger);
        locator.Register<ChunkPersistenceService>(persistence);

        var movementSettings = TryLoadMovementSettings(dataLoader, logger);
        var playerData = new PlayerData(movementSettings);
        locator.Register<PlayerData>(playerData);

        var debugDataProvider = new DebugDataProvider(playerData, chunkManager, biomeSelector);
        locator.Register<IDebugDataProvider>(debugDataProvider);

        var hotbarController = new HotbarController(playerData);
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
