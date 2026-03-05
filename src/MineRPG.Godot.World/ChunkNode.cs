using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World;

/// <summary>
/// Godot scene node for one chunk.
/// Owns the MeshInstance3D and StaticBody3D for terrain collision.
/// Supports multi-surface meshes: surface 0 = opaque terrain,
/// surface 1 = translucent liquid (if present).
/// </summary>
public sealed partial class ChunkNode : Node3D
{
    private static Material? _sharedMaterial;
    private static Material? _sharedWaterMaterial;

    private MeshInstance3D _meshInstance = null!;
    private StaticBody3D _staticBody = null!;
    private CollisionShape3D _collisionShape = null!;
    private ILogger _logger = null!;

    /// <summary>
    /// Gets the chunk coordinate this node represents.
    /// </summary>
    public ChunkCoord Coord { get; private set; }

    /// <summary>
    /// Sets the shared material used by all chunk mesh instances for opaque terrain.
    /// Must be called once at startup before any ChunkNode enters the tree.
    /// </summary>
    /// <param name="material">The shared opaque terrain material.</param>
    public static void SetSharedMaterial(Material material) => _sharedMaterial = material;

    /// <summary>
    /// Sets the shared material used for translucent liquid surfaces.
    /// Must be called once at startup before any ChunkNode enters the tree.
    /// </summary>
    /// <param name="material">The shared liquid material.</param>
    public static void SetSharedWaterMaterial(Material material) => _sharedWaterMaterial = material;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        _meshInstance = new MeshInstance3D();
        AddChild(_meshInstance);

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
    /// Applies the given mesh result to this chunk node, updating the visual mesh and collision shape.
    /// </summary>
    /// <param name="meshResult">The mesh result containing opaque and liquid surfaces.</param>
    public void ApplyMesh(ChunkMeshResult meshResult)
    {
        ArrayMesh? mesh = ChunkMeshApplier.Build(meshResult);
        _meshInstance.Mesh = mesh;

        if (mesh is not null)
        {
            int surfaceIndex = 0;

            if (!meshResult.Opaque.IsEmpty)
            {
                _meshInstance.SetSurfaceOverrideMaterial(
                    surfaceIndex, _sharedMaterial ?? CreateFallbackMaterial());
                surfaceIndex++;
            }

            if (!meshResult.Liquid.IsEmpty)
            {
                _meshInstance.SetSurfaceOverrideMaterial(
                    surfaceIndex, _sharedWaterMaterial ?? CreateFallbackMaterial());
            }
        }

        // Only opaque terrain generates collision
        ConcavePolygonShape3D? collision = ChunkMeshApplier.BuildCollision(meshResult.Opaque);
        _collisionShape.Shape = collision;
    }

    /// <summary>
    /// Clears the mesh and collision shape, resetting this node for pooling.
    /// </summary>
    public void ClearMesh()
    {
        _meshInstance.Mesh = null;
        _collisionShape.Shape = null;
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
