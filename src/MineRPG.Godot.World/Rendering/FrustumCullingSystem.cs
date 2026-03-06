using System;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.DI;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Spatial;

using MineRPG.Godot.World.Chunks;

namespace MineRPG.Godot.World.Rendering;

/// <summary>
/// Extracts the camera frustum planes each frame and sets chunk and sub-chunk visibility.
/// Performs four-level culling:
/// 1. BFS occlusion: eliminates chunks hidden behind solid terrain (mountains, underground).
/// 2. Chunk-level frustum: hides entire ChunkNodes outside the camera frustum.
/// 3. Sub-chunk frustum: hides individual 16x16x16 MeshInstance3Ds outside the frustum.
/// 4. Vertical occlusion: hides sub-chunks below a solid horizontal barrier.
/// Uses a position/rotation threshold to avoid recalculating when the camera barely moves.
/// </summary>
public sealed partial class FrustumCullingSystem : Node
{
    private const float PositionThreshold = 0.5f;
    private const float RotationThresholdDegrees = 1.0f;
    private const int MaxPlanesCount = 6;

    private Camera3D? _camera;
    private WorldNode? _worldNode;
    private OcclusionCuller? _occlusionCuller;
    private OptimizationFlags? _optimizationFlags;
    private Vector3 _lastCameraPosition;
    private Vector3 _lastCameraForward;
    private bool _shouldForceUpdate = true;

    /// <summary>
    /// Gets the number of chunks currently visible after frustum culling.
    /// </summary>
    public int VisibleChunks { get; private set; }

    /// <summary>
    /// Gets the total number of chunk nodes evaluated during the last culling pass.
    /// </summary>
    public int TotalChunks { get; private set; }

    /// <summary>
    /// Gets the number of sub-chunk mesh instances visible after frustum culling.
    /// </summary>
    public int VisibleSubChunks { get; private set; }

    /// <summary>
    /// Gets the total number of non-empty sub-chunk mesh instances evaluated.
    /// </summary>
    public int TotalSubChunks { get; private set; }

    /// <summary>
    /// Gets the number of sub-chunks hidden by vertical occlusion culling.
    /// </summary>
    public int OccludedSubChunks { get; private set; }

    /// <summary>
    /// Gets the number of chunks culled by BFS occlusion (hidden behind solid terrain).
    /// </summary>
    public int BfsOccludedChunks { get; private set; }

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
    /// Sets the occlusion culler for BFS-based chunk visibility.
    /// </summary>
    /// <param name="occlusionCuller">The occlusion culler instance.</param>
    public void SetOcclusionCuller(OcclusionCuller occlusionCuller) => _occlusionCuller = occlusionCuller;

    /// <summary>
    /// Sets the optimization flags for runtime toggle of culling features.
    /// </summary>
    /// <param name="flags">The optimization flags instance.</param>
    public void SetOptimizationFlags(OptimizationFlags flags) => _optimizationFlags = flags;

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
        float cameraY = _camera!.GlobalPosition.Y;
        bool useOcclusionCulling = _occlusionCuller is not null
            && (_optimizationFlags is null || _optimizationFlags.OcclusionCullingEnabled);

        int visibleChunks = 0;
        int totalChunks = 0;
        int visibleSubChunks = 0;
        int totalSubChunks = 0;
        int occludedSubChunks = 0;
        int bfsOccludedChunks = 0;

        foreach (ChunkNode child in _worldNode!.GetChunkNodes())
        {
            totalChunks++;
            int worldX = child.Coord.X * ChunkData.SizeX;
            int worldZ = child.Coord.Z * ChunkData.SizeZ;

            // Level 0: BFS occlusion test (cheapest — just a HashSet lookup)
            if (useOcclusionCulling && !_occlusionCuller!.IsChunkVisible(child.Coord))
            {
                child.Visible = false;
                bfsOccludedChunks++;
                continue;
            }

            // Level 1: Chunk-level frustum test (16x256x16 AABB)
            bool isChunkVisible = FrustumCuller.IsChunkVisible(visiblePlanes, worldX, worldZ);
            child.Visible = isChunkVisible;

            if (!isChunkVisible)
            {
                continue;
            }

            visibleChunks++;

            // Level 2+3: Sub-chunk frustum test + vertical occlusion
            MeshInstance3D?[] subChunkMeshes = child.SubChunkMeshInstances;
            SubChunkInfo[]? metadata = child.SubChunkMetadata;

            ushort occlusionMask = 0;

            if (metadata is not null)
            {
                occlusionMask = FrustumCuller.ComputeVerticalOcclusionMask(metadata, cameraY);
            }

            for (int i = 0; i < subChunkMeshes.Length; i++)
            {
                MeshInstance3D? meshInstance = subChunkMeshes[i];

                if (meshInstance is null || meshInstance.Mesh is null)
                {
                    continue;
                }

                totalSubChunks++;

                // Check vertical occlusion first (cheaper than frustum test)
                if ((occlusionMask & (1 << i)) != 0)
                {
                    meshInstance.Visible = false;
                    occludedSubChunks++;
                    continue;
                }

                int subChunkMinY = i * SubChunkConstants.SubChunkSize;

                bool isSubChunkVisible = FrustumCuller.IsSubChunkVisible(
                    visiblePlanes, worldX, subChunkMinY, worldZ, SubChunkConstants.SubChunkSize);

                meshInstance.Visible = isSubChunkVisible;

                if (isSubChunkVisible)
                {
                    visibleSubChunks++;
                }
            }
        }

        VisibleChunks = visibleChunks;
        TotalChunks = totalChunks;
        VisibleSubChunks = visibleSubChunks;
        TotalSubChunks = totalSubChunks;
        OccludedSubChunks = occludedSubChunks;
        BfsOccludedChunks = bfsOccludedChunks;
    }
}
