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
    private const float RotationThresholdDeg = 1.0f;
    private const int MaxPlanesCount = 6;

    private Camera3D? _camera;
    private WorldNode? _worldNode;

    private Vector3 _lastCameraPosition;
    private Vector3 _lastCameraForward;
    private bool _forceUpdate = true;

    private int _visibleChunks;
    private int _totalChunks;

    public int VisibleChunks => _visibleChunks;
    public int TotalChunks => _totalChunks;

    public void SetCamera(Camera3D camera) => _camera = camera;

    public void SetWorldNode(WorldNode worldNode) => _worldNode = worldNode;

    public override void _Process(double delta)
    {
        if (_camera is null || _worldNode is null || !_camera.IsInsideTree())
            return;

        var camPos = _camera.GlobalPosition;
        var camForward = -_camera.GlobalTransform.Basis.Z;

        if (!_forceUpdate && !HasCameraMoved(camPos, camForward))
            return;

        _forceUpdate = false;
        _lastCameraPosition = camPos;
        _lastCameraForward = camForward;

        UpdateVisibility();
    }

    /// <summary>
    /// Forces a full frustum update on the next frame.
    /// Call when chunks are added/removed.
    /// </summary>
    public void Invalidate() => _forceUpdate = true;

    private bool HasCameraMoved(Vector3 pos, Vector3 forward)
    {
        var posDelta = (pos - _lastCameraPosition).LengthSquared();
        if (posDelta > PositionThreshold * PositionThreshold)
            return true;

        var dot = _lastCameraForward.Dot(forward);
        var angleDeg = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)));
        return angleDeg > RotationThresholdDeg;
    }

    private void UpdateVisibility()
    {
        var frustumGodot = _camera!.GetFrustum();
        Span<FrustumPlane> planes = stackalloc FrustumPlane[MaxPlanesCount];
        var planeCount = System.Math.Min(frustumGodot.Count, MaxPlanesCount);

        for (var i = 0; i < planeCount; i++)
        {
            var p = (Plane)frustumGodot[i];
            planes[i] = new FrustumPlane(p.Normal.X, p.Normal.Y, p.Normal.Z, p.D);
        }

        var visiblePlanes = planes[..planeCount];
        var visible = 0;
        var total = 0;

        foreach (var child in _worldNode!.GetChunkNodes())
        {
            total++;
            var worldX = child.Coord.X * ChunkData.SizeX;
            var worldZ = child.Coord.Z * ChunkData.SizeZ;

            var isVisible = FrustumCuller.IsChunkVisible(visiblePlanes, worldX, worldZ);
            child.Visible = isVisible;

            if (isVisible)
                visible++;
        }

        _visibleChunks = visible;
        _totalChunks = total;
    }
}
