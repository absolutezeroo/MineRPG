#if DEBUG
using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World.Debug;

/// <summary>
/// Renders wireframe borders around the chunk the player is currently standing in.
/// Uses ImmediateMesh for zero-allocation per-frame line drawing.
/// Added as a child of WorldNode. Toggled via F5.
/// </summary>
public sealed partial class ChunkBorderRenderer : MeshInstance3D
{
    private const float BorderHeight = 256f;
    private const float LineWidth = 2f;

    private static readonly Color BorderColor = new(1f, 1f, 0f, 0.6f);
    private static readonly Color NeighborBorderColor = new(0.5f, 0.5f, 0f, 0.3f);

    private IDebugDataProvider _debugData = null!;
    private ImmediateMesh _immediateMesh = null!;
    private StandardMaterial3D _material = null!;

    private int _lastChunkX = int.MinValue;
    private int _lastChunkZ = int.MinValue;

    /// <inheritdoc />
    public override void _Ready()
    {
        _debugData = ServiceLocator.Instance.Get<IDebugDataProvider>();

        _immediateMesh = new ImmediateMesh();
        Mesh = _immediateMesh;

        _material = new StandardMaterial3D();
        _material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        _material.VertexColorUseAsAlbedo = true;
        _material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        _material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _material.NoDepthTest = true;

        CastShadow = ShadowCastingSetting.Off;
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        int chunkX = _debugData.ChunkX;
        int chunkZ = _debugData.ChunkZ;

        if (chunkX == _lastChunkX && chunkZ == _lastChunkZ)
        {
            return;
        }

        _lastChunkX = chunkX;
        _lastChunkZ = chunkZ;

        RebuildMesh(chunkX, chunkZ);
    }

    private void RebuildMesh(int chunkX, int chunkZ)
    {
        _immediateMesh.ClearSurfaces();
        _immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _material);

        // Current chunk border (bright yellow)
        DrawChunkBorder(chunkX, chunkZ, BorderColor);

        // Adjacent chunk borders (dim yellow)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0)
                {
                    continue;
                }

                DrawChunkBorder(chunkX + dx, chunkZ + dz, NeighborBorderColor);
            }
        }

        _immediateMesh.SurfaceEnd();
    }

    private void DrawChunkBorder(int chunkX, int chunkZ, Color color)
    {
        float worldX = chunkX * ChunkData.SizeX;
        float worldZ = chunkZ * ChunkData.SizeZ;
        float maxX = worldX + ChunkData.SizeX;
        float maxZ = worldZ + ChunkData.SizeZ;

        // Bottom edges
        DrawLine3D(new Vector3(worldX, 0, worldZ), new Vector3(maxX, 0, worldZ), color);
        DrawLine3D(new Vector3(maxX, 0, worldZ), new Vector3(maxX, 0, maxZ), color);
        DrawLine3D(new Vector3(maxX, 0, maxZ), new Vector3(worldX, 0, maxZ), color);
        DrawLine3D(new Vector3(worldX, 0, maxZ), new Vector3(worldX, 0, worldZ), color);

        // Top edges
        DrawLine3D(new Vector3(worldX, BorderHeight, worldZ), new Vector3(maxX, BorderHeight, worldZ), color);
        DrawLine3D(new Vector3(maxX, BorderHeight, worldZ), new Vector3(maxX, BorderHeight, maxZ), color);
        DrawLine3D(new Vector3(maxX, BorderHeight, maxZ), new Vector3(worldX, BorderHeight, maxZ), color);
        DrawLine3D(new Vector3(worldX, BorderHeight, maxZ), new Vector3(worldX, BorderHeight, worldZ), color);

        // Vertical edges
        DrawLine3D(new Vector3(worldX, 0, worldZ), new Vector3(worldX, BorderHeight, worldZ), color);
        DrawLine3D(new Vector3(maxX, 0, worldZ), new Vector3(maxX, BorderHeight, worldZ), color);
        DrawLine3D(new Vector3(maxX, 0, maxZ), new Vector3(maxX, BorderHeight, maxZ), color);
        DrawLine3D(new Vector3(worldX, 0, maxZ), new Vector3(worldX, BorderHeight, maxZ), color);
    }

    private void DrawLine3D(Vector3 from, Vector3 to, Color color)
    {
        _immediateMesh.SurfaceSetColor(color);
        _immediateMesh.SurfaceAddVertex(from);
        _immediateMesh.SurfaceSetColor(color);
        _immediateMesh.SurfaceAddVertex(to);
    }
}
#endif
