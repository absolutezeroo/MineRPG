using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World.Chunks;

/// <summary>
/// Godot scene node for one chunk.
/// Owns per-sub-chunk MeshInstance3D nodes for independent frustum culling
/// and a single StaticBody3D for terrain collision.
///
/// Each non-empty sub-chunk (16x16x16 vertical section) gets its own
/// MeshInstance3D. Empty sub-chunks have no mesh instance. The collision
/// shape covers all opaque surfaces across all sub-chunks as one shape.
/// </summary>
public sealed partial class ChunkNode : Node3D
{
    private static Material? _sharedMaterial;
    private static Material? _sharedWaterMaterial;

    private StaticBody3D _staticBody = null!;
    private CollisionShape3D _collisionShape = null!;
    private ILogger _logger = null!;

    /// <summary>
    /// Gets the chunk coordinate this node represents.
    /// </summary>
    public ChunkCoord Coord { get; private set; }

    /// <summary>
    /// Gets the per-sub-chunk MeshInstance3D array for frustum culling.
    /// Entries may be null for empty sub-chunks.
    /// </summary>
    public MeshInstance3D?[] SubChunkMeshInstances { get; } = new MeshInstance3D?[SubChunkConstants.SubChunkCount];

    /// <summary>
    /// Gets or sets the sub-chunk metadata for this chunk, used by vertical occlusion culling.
    /// Set after mesh apply from the chunk pipeline. Null until first mesh is applied.
    /// </summary>
    public SubChunkInfo[]? SubChunkMetadata { get; set; }

    /// <summary>
    /// Sets the shared material used by all chunk mesh instances for opaque terrain.
    /// Must be called once at startup before any ChunkNode enters the tree.
    /// </summary>
    /// <param name="material">The shared opaque terrain material.</param>
    public static void SetSharedMaterial(Material material)
    {
        _sharedMaterial?.Dispose();
        _sharedMaterial = material;
    }

    /// <summary>
    /// Sets the shared material used for translucent liquid surfaces.
    /// Must be called once at startup before any ChunkNode enters the tree.
    /// </summary>
    /// <param name="material">The shared liquid material.</param>
    public static void SetSharedWaterMaterial(Material material)
    {
        _sharedWaterMaterial?.Dispose();
        _sharedWaterMaterial = material;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        _staticBody = new StaticBody3D();
        _collisionShape = new CollisionShape3D();
        _staticBody.AddChild(_collisionShape);
        AddChild(_staticBody);
    }

    /// <summary>
    /// Initializes this chunk node with the given coordinate and positions it in world space.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    public void Initialize(ChunkCoord coord)
    {
        Coord = coord;
        Name = $"Chunk_{coord.X}_{coord.Z}";
        Position = new Vector3(
            coord.X * ChunkData.SizeX,
            0,
            coord.Z * ChunkData.SizeZ);
    }

    /// <summary>
    /// Applies the given mesh result to this chunk node, creating per-sub-chunk
    /// MeshInstance3D nodes and a combined collision shape.
    /// </summary>
    /// <param name="meshResult">The mesh result containing per-sub-chunk data.</param>
    public void ApplyMesh(ChunkMeshResult meshResult)
    {
        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            SubChunkMesh subChunkMesh = meshResult.SubChunks[i];

            if (subChunkMesh.IsEmpty)
            {
                ClearSubChunkMesh(i);
                continue;
            }

            ArrayMesh? mesh = ChunkMeshApplier.Build(subChunkMesh);

            if (mesh is null)
            {
                ClearSubChunkMesh(i);
                continue;
            }

            MeshInstance3D instance = GetOrCreateSubChunkMeshInstance(i);
            instance.Mesh = mesh;

            int surfaceIndex = 0;

            if (!subChunkMesh.Opaque.IsEmpty)
            {
                instance.SetSurfaceOverrideMaterial(
                    surfaceIndex, _sharedMaterial ?? CreateFallbackMaterial());
                surfaceIndex++;
            }

            if (!subChunkMesh.Liquid.IsEmpty)
            {
                instance.SetSurfaceOverrideMaterial(
                    surfaceIndex, _sharedWaterMaterial ?? CreateFallbackMaterial());
            }
        }

        // Build combined collision from all sub-chunk opaque surfaces
        ConcavePolygonShape3D? collision = ChunkMeshApplier.BuildCombinedCollision(meshResult);
        _collisionShape.Shape = collision;
    }

    /// <summary>
    /// Applies mesh data to a single sub-chunk without touching other sub-chunks.
    /// Used for incremental updates after block edits.
    /// </summary>
    /// <param name="subChunkIndex">The sub-chunk index to update.</param>
    /// <param name="subChunkMesh">The mesh data for the sub-chunk.</param>
    public void ApplySubChunkMesh(int subChunkIndex, SubChunkMesh subChunkMesh)
    {
        if (subChunkMesh.IsEmpty)
        {
            ClearSubChunkMesh(subChunkIndex);
            return;
        }

        ArrayMesh? mesh = ChunkMeshApplier.Build(subChunkMesh);

        if (mesh is null)
        {
            ClearSubChunkMesh(subChunkIndex);
            return;
        }

        MeshInstance3D instance = GetOrCreateSubChunkMeshInstance(subChunkIndex);
        instance.Mesh = mesh;

        int surfaceIndex = 0;

        if (!subChunkMesh.Opaque.IsEmpty)
        {
            instance.SetSurfaceOverrideMaterial(
                surfaceIndex, _sharedMaterial ?? CreateFallbackMaterial());
            surfaceIndex++;
        }

        if (!subChunkMesh.Liquid.IsEmpty)
        {
            instance.SetSurfaceOverrideMaterial(
                surfaceIndex, _sharedWaterMaterial ?? CreateFallbackMaterial());
        }
    }

    /// <summary>
    /// Clears all sub-chunk meshes and the collision shape, resetting this node for pooling.
    /// </summary>
    public void ClearMesh()
    {
        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            ClearSubChunkMesh(i);
        }

        _collisionShape.Shape = null;
        SubChunkMetadata = null;
    }

    private MeshInstance3D GetOrCreateSubChunkMeshInstance(int subChunkIndex)
    {
        MeshInstance3D? existing = SubChunkMeshInstances[subChunkIndex];

        if (existing is not null)
        {
            return existing;
        }

        MeshInstance3D instance = new();
        instance.Name = $"SubChunk_{subChunkIndex}";
        AddChild(instance);
        SubChunkMeshInstances[subChunkIndex] = instance;
        return instance;
    }

    private void ClearSubChunkMesh(int subChunkIndex)
    {
        MeshInstance3D? instance = SubChunkMeshInstances[subChunkIndex];

        if (instance is null)
        {
            return;
        }

        instance.Mesh = null;
        RemoveChild(instance);
        instance.QueueFree();
        SubChunkMeshInstances[subChunkIndex] = null;
    }

    private static StandardMaterial3D CreateFallbackMaterial()
    {
        return new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
        };
    }
}
