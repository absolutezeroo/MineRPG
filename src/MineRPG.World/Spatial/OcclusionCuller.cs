using System.Collections.Generic;
using System.Runtime.CompilerServices;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// Performs BFS-based occlusion culling at the chunk column level.
/// Starting from the player's chunk, propagates visibility to neighboring
/// chunks only if the current chunk's <see cref="ChunkVisibilityMatrix"/>
/// allows sight to pass through from the entry face to the exit face.
///
/// Combined with frustum culling, this eliminates chunks hidden behind
/// mountains, underground terrain, or other solid structures.
///
/// The BFS runs when the player changes chunk. Results are cached until
/// the next chunk change or until a block edit invalidates a visibility matrix.
///
/// Pure C# — no Godot dependency. Thread-safe for read access to results.
/// </summary>
public sealed class OcclusionCuller
{
    private const int MaxQueueCapacity = 32768;

    private readonly Dictionary<ChunkCoord, ChunkVisibilityMatrix> _matrices = new();
    private readonly HashSet<ChunkCoord> _visibleChunks = new();

    private ChunkCoord _lastPlayerChunk = new(int.MinValue, int.MinValue);
    private bool _isDirty = true;

    // Pre-allocated BFS structures to avoid per-frame allocation
    private readonly Queue<BfsEntry> _bfsQueue = new();
    private readonly HashSet<long> _visitedFaces = new();

    /// <summary>
    /// Gets the set of chunks determined to be visible by the last BFS pass.
    /// Empty until the first update. Thread-safe for read-only enumeration
    /// when called from the same thread as Update.
    /// </summary>
    public IReadOnlySet<ChunkCoord> VisibleChunks => _visibleChunks;

    /// <summary>
    /// Gets the number of chunks with stored visibility matrices.
    /// </summary>
    public int MatrixCount => _matrices.Count;

    /// <summary>
    /// Stores or updates the visibility matrix for a chunk.
    /// Call this after meshing completes (on the main thread or synchronized).
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="matrix">The computed visibility matrix.</param>
    public void SetMatrix(ChunkCoord coord, ChunkVisibilityMatrix matrix)
    {
        _matrices[coord] = matrix;
        _isDirty = true;
    }

    /// <summary>
    /// Removes the visibility matrix for an unloaded chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    public void RemoveMatrix(ChunkCoord coord)
    {
        _matrices.Remove(coord);
        _isDirty = true;
    }

    /// <summary>
    /// Forces a full BFS recompute on the next Update call.
    /// </summary>
    public void Invalidate()
    {
        _isDirty = true;
    }

    /// <summary>
    /// Checks whether a chunk is in the visible set from the last BFS pass.
    /// Returns true if occlusion culling is not active (no matrices available).
    /// </summary>
    /// <param name="coord">The chunk coordinate to test.</param>
    /// <returns>True if the chunk should be rendered.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsChunkVisible(ChunkCoord coord)
    {
        if (_visibleChunks.Count == 0)
        {
            return true;
        }

        return _visibleChunks.Contains(coord);
    }

    /// <summary>
    /// Runs the BFS if the player changed chunk or visibility data changed.
    /// </summary>
    /// <param name="playerChunk">The player's current chunk coordinate.</param>
    /// <param name="renderDistance">The current render distance for BFS bounds.</param>
    public void Update(ChunkCoord playerChunk, int renderDistance)
    {
        bool playerMoved = playerChunk != _lastPlayerChunk;

        if (!playerMoved && !_isDirty)
        {
            return;
        }

        _lastPlayerChunk = playerChunk;
        _isDirty = false;

        RunBfs(playerChunk, renderDistance);
    }

    private void RunBfs(ChunkCoord playerChunk, int renderDistance)
    {
        _visibleChunks.Clear();
        _bfsQueue.Clear();
        _visitedFaces.Clear();

        // The player's chunk is always visible, entered from "all faces"
        _visibleChunks.Add(playerChunk);

        // Seed: propagate from player chunk to all 4 neighbors
        EnqueueNeighbor(playerChunk, ChunkVisibilityMatrix.FaceNorth, renderDistance);
        EnqueueNeighbor(playerChunk, ChunkVisibilityMatrix.FaceSouth, renderDistance);
        EnqueueNeighbor(playerChunk, ChunkVisibilityMatrix.FaceEast, renderDistance);
        EnqueueNeighbor(playerChunk, ChunkVisibilityMatrix.FaceWest, renderDistance);

        while (_bfsQueue.Count > 0)
        {
            BfsEntry current = _bfsQueue.Dequeue();
            _visibleChunks.Add(current.Coord);

            // Get this chunk's visibility matrix
            if (!_matrices.TryGetValue(current.Coord, out ChunkVisibilityMatrix matrix))
            {
                // No matrix available: assume fully transparent (conservative)
                matrix = ChunkVisibilityMatrix.AllVisible;
            }

            // Try to propagate to each exit face
            for (int exitFace = 0; exitFace < ChunkVisibilityMatrix.FaceCount; exitFace++)
            {
                if (!matrix.CanSeeThrough(current.EntryFace, exitFace))
                {
                    continue;
                }

                EnqueueNeighbor(current.Coord, exitFace, renderDistance);
            }
        }
    }

    private void EnqueueNeighbor(ChunkCoord fromCoord, int exitFace, int renderDistance)
    {
        ChunkCoord neighborCoord = GetNeighborCoord(fromCoord, exitFace);
        int entryFace = ChunkVisibilityMatrix.OppositeFace(exitFace);

        // Dedup: don't visit the same chunk from the same entry face twice
        long visitKey = ((long)neighborCoord.X << 32) | ((long)(neighborCoord.Z & 0xFFFF) << 16)
                       | (long)(ushort)entryFace;

        if (!_visitedFaces.Add(visitKey))
        {
            return;
        }

        // Bounds check: don't go beyond render distance
        int distance = neighborCoord.ChebyshevDistance(_lastPlayerChunk);

        if (distance > renderDistance)
        {
            return;
        }

        // Guard against runaway BFS
        if (_bfsQueue.Count >= MaxQueueCapacity)
        {
            _visibleChunks.Add(neighborCoord);
            return;
        }

        _bfsQueue.Enqueue(new BfsEntry(neighborCoord, entryFace));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ChunkCoord GetNeighborCoord(ChunkCoord coord, int face)
    {
        return face switch
        {
            ChunkVisibilityMatrix.FaceNorth => coord.North,
            ChunkVisibilityMatrix.FaceSouth => coord.South,
            ChunkVisibilityMatrix.FaceEast => coord.East,
            ChunkVisibilityMatrix.FaceWest => coord.West,
            _ => coord,
        };
    }

    private readonly struct BfsEntry
    {
        public ChunkCoord Coord { get; }
        public int EntryFace { get; }

        public BfsEntry(ChunkCoord coord, int entryFace)
        {
            Coord = coord;
            EntryFace = entryFace;
        }
    }
}
