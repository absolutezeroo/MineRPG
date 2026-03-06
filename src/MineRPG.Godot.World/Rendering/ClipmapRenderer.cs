using Godot;

using MineRPG.World.Meshing;
using MineRPG.World.Terrain;

namespace MineRPG.Godot.World.Rendering;

/// <summary>
/// Renders geometry clipmap rings as Godot MeshInstance3D nodes.
/// Each ring is a single mesh at progressively lower resolution,
/// creating a terrain horizon silhouette without loading chunk data.
///
/// Rebuilds only when the player moves beyond the configured threshold.
/// The ring meshes are generated on a background thread and applied
/// on the main thread via deferred call.
/// </summary>
public sealed partial class ClipmapRenderer : Node3D
{
    private readonly MeshInstance3D[] _ringInstances = new MeshInstance3D[ClipmapConfig.RingCount];

    private StandardMaterial3D? _clipmapMaterial;
    private ClipmapConfig _config = new();
    private ClipmapGenerator.HeightSampler? _heightSampler;
    private ClipmapGenerator.ColorSampler? _colorSampler;
    private float _lastRebuildX;
    private float _lastRebuildZ;
    private bool _isInitialized;

    /// <summary>
    /// Configures the clipmap renderer with sampling functions.
    /// Must be called before the first frame.
    /// </summary>
    /// <param name="config">Clipmap configuration.</param>
    /// <param name="heightSampler">Terrain height sampling function.</param>
    /// <param name="colorSampler">Biome color sampling function.</param>
    public void Configure(
        ClipmapConfig config,
        ClipmapGenerator.HeightSampler heightSampler,
        ClipmapGenerator.ColorSampler colorSampler)
    {
        _config = config;
        _heightSampler = heightSampler;
        _colorSampler = colorSampler;
        _isInitialized = true;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        _clipmapMaterial = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        for (int i = 0; i < ClipmapConfig.RingCount; i++)
        {
            MeshInstance3D instance = new();
            instance.Name = $"ClipmapRing_{i}";
            instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(instance);
            _ringInstances[i] = instance;
        }
    }

    /// <summary>
    /// Updates the clipmap inner radius and ring layout when render distance changes.
    /// Forces a full rebuild on the next position update.
    /// </summary>
    /// <param name="renderDistanceChunks">New voxel render distance in chunks.</param>
    public void UpdateRenderDistance(int renderDistanceChunks)
    {
        if (!_isInitialized || renderDistanceChunks == _config.VoxelCutoffChunks)
        {
            return;
        }

        _config = new ClipmapConfig
        {
            VoxelCutoffChunks = renderDistanceChunks,
            Rings = ClipmapConfig.CreateDefaultRings(renderDistanceChunks),
            RebuildThresholdBlocks = _config.RebuildThresholdBlocks,
        };

        // Force rebuild on next UpdatePlayerPosition call
        _lastRebuildX = float.MinValue;
        _lastRebuildZ = float.MinValue;
    }

    /// <summary>
    /// Updates the clipmap if the player has moved enough to warrant a rebuild.
    /// </summary>
    /// <param name="playerWorldX">Player world X coordinate.</param>
    /// <param name="playerWorldZ">Player world Z coordinate.</param>
    public void UpdatePlayerPosition(float playerWorldX, float playerWorldZ)
    {
        if (!_isInitialized || _heightSampler is null || _colorSampler is null)
        {
            return;
        }

        float deltaX = playerWorldX - _lastRebuildX;
        float deltaZ = playerWorldZ - _lastRebuildZ;
        float distanceSq = deltaX * deltaX + deltaZ * deltaZ;
        float threshold = _config.RebuildThresholdBlocks;

        if (distanceSq < threshold * threshold)
        {
            return;
        }

        _lastRebuildX = playerWorldX;
        _lastRebuildZ = playerWorldZ;

        RebuildRings(playerWorldX, playerWorldZ);
    }

    private void RebuildRings(float centerX, float centerZ)
    {
        MeshData[] ringMeshes = ClipmapGenerator.Generate(
            _config, centerX, centerZ, _heightSampler!, _colorSampler!);

        for (int i = 0; i < ClipmapConfig.RingCount; i++)
        {
            if (i >= ringMeshes.Length || ringMeshes[i].IsEmpty)
            {
                _ringInstances[i].Mesh = null;
                continue;
            }

            ArrayMesh arrayMesh = BuildArrayMesh(ringMeshes[i]);
            _ringInstances[i].Mesh = arrayMesh;
            _ringInstances[i].MaterialOverride = _clipmapMaterial;
        }
    }

    private static ArrayMesh BuildArrayMesh(MeshData meshData)
    {
        ArrayMesh mesh = new();
        global::Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);

        Vector3[] vertices = new Vector3[meshData.VertexCount];
        Vector3[] normals = new Vector3[meshData.VertexCount];
        Vector2[] uvs = new Vector2[meshData.VertexCount];
        Color[] colors = new Color[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            int vBase = i * 3;
            int uvBase = i * 2;
            int cBase = i * 4;

            vertices[i] = new Vector3(
                meshData.Vertices[vBase],
                meshData.Vertices[vBase + 1],
                meshData.Vertices[vBase + 2]);

            normals[i] = new Vector3(
                meshData.Normals[vBase],
                meshData.Normals[vBase + 1],
                meshData.Normals[vBase + 2]);

            uvs[i] = new Vector2(
                meshData.Uvs[uvBase],
                meshData.Uvs[uvBase + 1]);

            colors[i] = new Color(
                meshData.Colors[cBase],
                meshData.Colors[cBase + 1],
                meshData.Colors[cBase + 2],
                meshData.Colors[cBase + 3]);
        }

        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = meshData.Indices;

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
