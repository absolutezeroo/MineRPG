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

    public ChunkCoord Coord { get; private set; }

    /// <summary>
    /// Sets the shared material used by all chunk mesh instances for opaque terrain.
    /// Must be called once at startup before any ChunkNode enters the tree.
    /// </summary>
    public static void SetSharedMaterial(Material material)
    {
        _sharedMaterial = material;
    }

    /// <summary>
    /// Sets the shared material used for translucent liquid surfaces.
    /// Must be called once at startup before any ChunkNode enters the tree.
    /// </summary>
    public static void SetSharedWaterMaterial(Material material)
    {
        _sharedWaterMaterial = material;
    }

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

    private static StandardMaterial3D CreateFallbackMaterial()
    {
        return new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
        };
    }

    public void Initialize(ChunkCoord coord)
    {
        Coord = coord;
        Name = $"Chunk_{coord.X}_{coord.Z}";
        Position = new Vector3(
            coord.X * ChunkData.SizeX,
            0,
            coord.Z * ChunkData.SizeZ);
    }

    public void ApplyMesh(ChunkMeshResult meshResult)
    {
        var mesh = ChunkMeshApplier.Build(meshResult);
        _meshInstance.Mesh = mesh;

        if (mesh is not null)
        {
            var surfaceIdx = 0;

            if (!meshResult.Opaque.IsEmpty)
            {
                _meshInstance.SetSurfaceOverrideMaterial(
                    surfaceIdx, _sharedMaterial ?? CreateFallbackMaterial());
                surfaceIdx++;
            }

            if (!meshResult.Liquid.IsEmpty)
            {
                _meshInstance.SetSurfaceOverrideMaterial(
                    surfaceIdx, _sharedWaterMaterial ?? CreateFallbackMaterial());
            }
        }

        // Only opaque terrain generates collision
        var collision = ChunkMeshApplier.BuildCollision(meshResult.Opaque);
        _collisionShape.Shape = collision;
    }

    public void ClearMesh()
    {
        _meshInstance.Mesh = null;
        _collisionShape.Shape = null;
    }
}
