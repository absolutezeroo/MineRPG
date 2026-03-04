using System;

using Godot;

using MineRPG.World.Chunks;
using MineRPG.World.Spatial;

namespace MineRPG.Godot.World;

/// <summary>
/// Extracts the camera frustum planes each frame and sets chunk node visibility.
/// Chunks outside the frustum are hidden to skip rendering (Godot respects Visible=false).
/// Uses a position/rotation threshold to avoid recalculating when the camera barely moves.
/// </summary>
public sealed partial class FrustumCullingSystem : Node
{
    private const float PositionThreshold = 0.5f;
    private const float RotationThresholdDegrees = 1.0f;
    private const int MaxPlanesCount = 6;

    private Camera3D? _camera;
    private WorldNode? _worldNode;
    private Vector3 _lastCameraPosition;
    private Vector3 _lastCameraForward;
    private bool _shouldForceUpdate = true;
    private int _visibleChunks;
    private int _totalChunks;

    /// <summary>
    /// Gets the number of chunks currently visible after frustum culling.
    /// </summary>
    public int VisibleChunks => _visibleChunks;

    /// <summary>
    /// Gets the total number of chunk nodes evaluated during the last culling pass.
    /// </summary>
    public int TotalChunks => _totalChunks;

    /// <summary>
    /// Sets the camera used for frustum extraction.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D camera) => _camera = camera;

    /// <summary>
    /// Sets the world node that owns the chunk nodes to cull.
    /// </summary>
    /// <param name="worldNode">The world node containing chunk nodes.</param>
    public void SetWorldNode(WorldNode worldNode) => _worldNode = worldNode;

    /// <summary>
    /// Forces a full frustum update on the next frame.
    /// Call when chunks are added or removed.
    /// </summary>
    public void Invalidate() => _shouldForceUpdate = true;

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_camera is null || _worldNode is null || !_camera.IsInsideTree())
        {
            return;
        }

        Vector3 cameraPosition = _camera.GlobalPosition;
        Vector3 cameraForward = -_camera.GlobalTransform.Basis.Z;

        if (!_shouldForceUpdate && !HasCameraMoved(cameraPosition, cameraForward))
        {
            return;
        }

        _shouldForceUpdate = false;
        _lastCameraPosition = cameraPosition;
        _lastCameraForward = cameraForward;

        UpdateVisibility();
    }

    private bool HasCameraMoved(Vector3 position, Vector3 forward)
    {
        float positionDeltaSquared = (position - _lastCameraPosition).LengthSquared();

        if (positionDeltaSquared > PositionThreshold * PositionThreshold)
        {
            return true;
        }

        float dot = _lastCameraForward.Dot(forward);
        float angleDegrees = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)));
        return angleDegrees > RotationThresholdDegrees;
    }

    private void UpdateVisibility()
    {
        global::Godot.Collections.Array<Plane> frustumGodot = _camera!.GetFrustum();
        Span<FrustumPlane> planes = stackalloc FrustumPlane[MaxPlanesCount];
        int planeCount = Math.Min(frustumGodot.Count, MaxPlanesCount);

        for (int i = 0; i < planeCount; i++)
        {
            Plane plane = (Plane)frustumGodot[i];
            planes[i] = new FrustumPlane(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
        }

        Span<FrustumPlane> visiblePlanes = planes[..planeCount];
        int visible = 0;
        int total = 0;

        foreach (ChunkNode child in _worldNode!.GetChunkNodes())
        {
            total++;
            int worldX = child.Coord.X * ChunkData.SizeX;
            int worldZ = child.Coord.Z * ChunkData.SizeZ;

            bool isVisible = FrustumCuller.IsChunkVisible(visiblePlanes, worldX, worldZ);
            child.Visible = isVisible;

            if (isVisible)
            {
                visible++;
            }
        }

        _visibleChunks = visible;
        _totalChunks = total;
    }
}
