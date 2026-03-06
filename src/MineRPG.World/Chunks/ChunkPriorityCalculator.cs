using System;
using System.Runtime.CompilerServices;

using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Computes loading priority for chunks based on visibility and distance.
/// Chunks in front of the player (within the camera frustum) get higher
/// priority than chunks behind. Combined with distance, this ensures the
/// player sees a complete world in front before filling in behind them.
///
/// Priority levels (lower = more urgent):
/// 0: Player's chunk + 8 immediate neighbors
/// 1: In frustum, sorted by distance
/// 2: Out of frustum but close (distance &lt; 8)
/// 3: Out of frustum, mid-range
/// 4: Out of frustum, distant (behind the player)
///
/// Pure C# — no Godot dependency.
/// </summary>
public static class ChunkPriorityCalculator
{
    /// <summary>Priority for the player's chunk and its 8 immediate neighbors.</summary>
    public const int PriorityImmediate = 0;

    /// <summary>Priority for chunks inside the frustum, sorted by distance.</summary>
    public const int PriorityFrustumVisible = 100;

    /// <summary>Priority for chunks outside the frustum but close.</summary>
    public const int PriorityNearOutOfFrustum = 200;

    /// <summary>Priority for chunks outside the frustum at medium distance.</summary>
    public const int PriorityMidOutOfFrustum = 300;

    /// <summary>Priority for distant chunks behind the player.</summary>
    public const int PriorityFarBehind = 400;

    private const int NearDistance = 8;
    private const float FrustumDotThreshold = 0f;

    /// <summary>
    /// Computes the loading priority for a chunk.
    /// Lower values are loaded first.
    /// </summary>
    /// <param name="chunkCoord">The chunk to evaluate.</param>
    /// <param name="playerChunk">The player's current chunk coordinate.</param>
    /// <param name="forwardX">The camera forward direction X (normalized, XZ plane).</param>
    /// <param name="forwardZ">The camera forward direction Z (normalized, XZ plane).</param>
    /// <returns>An integer priority (lower = more urgent). Range [0, ~500].</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputePriority(
        ChunkCoord chunkCoord,
        ChunkCoord playerChunk,
        float forwardX,
        float forwardZ)
    {
        int distance = chunkCoord.ChebyshevDistance(playerChunk);

        // Immediate neighborhood: always top priority
        if (distance <= 1)
        {
            return PriorityImmediate + distance;
        }

        // Direction from player to chunk (unnormalized is fine for dot product sign)
        float directionX = chunkCoord.X - playerChunk.X;
        float directionZ = chunkCoord.Z - playerChunk.Z;

        // Dot product with camera forward to determine if chunk is in front
        float dot = directionX * forwardX + directionZ * forwardZ;
        bool isInFront = dot > FrustumDotThreshold;

        if (isInFront)
        {
            // In front of player: high priority, ordered by distance
            return PriorityFrustumVisible + distance;
        }

        // Behind or to the side of the player
        if (distance < NearDistance)
        {
            return PriorityNearOutOfFrustum + distance;
        }

        if (distance < NearDistance * 2)
        {
            return PriorityMidOutOfFrustum + distance;
        }

        return PriorityFarBehind + distance;
    }
}
