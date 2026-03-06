using System.Runtime.CompilerServices;

namespace MineRPG.World.Meshing;

/// <summary>
/// Determines the appropriate LOD level for a chunk based on its distance from
/// the player. Uses hysteresis thresholds to prevent LOD flip-flopping when
/// the player is near a boundary.
///
/// LOD 0: distance &lt; 16 chunks (full detail)
/// LOD 1: distance 16-32 chunks (2x downsampled)
/// LOD 2: distance 32-64 chunks (4x downsampled)
///
/// Hysteresis: LOD increases at threshold+1, decreases at threshold-1.
/// </summary>
public static class LodPolicy
{
    /// <summary>Distance threshold to upgrade from LOD 0 to LOD 1.</summary>
    public const int Lod0ToLod1Distance = 17;

    /// <summary>Distance threshold to downgrade from LOD 1 to LOD 0.</summary>
    public const int Lod1ToLod0Distance = 15;

    /// <summary>Distance threshold to upgrade from LOD 1 to LOD 2.</summary>
    public const int Lod1ToLod2Distance = 33;

    /// <summary>Distance threshold to downgrade from LOD 2 to LOD 1.</summary>
    public const int Lod2ToLod1Distance = 31;

    /// <summary>
    /// Downsampling factor for LOD 1 (2x2x2 blocks → 1 mega-block).
    /// </summary>
    public const int Lod1Factor = 2;

    /// <summary>
    /// Downsampling factor for LOD 2 (4x4x4 blocks → 1 mega-block).
    /// </summary>
    public const int Lod2Factor = 4;

    /// <summary>
    /// Computes the desired LOD level based on distance, ignoring hysteresis.
    /// Use this when a chunk is first loaded (no previous LOD to consider).
    /// </summary>
    /// <param name="chebyshevDistance">Chebyshev distance from the player in chunks.</param>
    /// <returns>The initial LOD level for the given distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LodLevel GetInitialLod(int chebyshevDistance)
    {
        if (chebyshevDistance >= Lod1ToLod2Distance)
        {
            return LodLevel.Lod2;
        }

        if (chebyshevDistance >= Lod0ToLod1Distance)
        {
            return LodLevel.Lod1;
        }

        return LodLevel.Lod0;
    }

    /// <summary>
    /// Computes the desired LOD level with hysteresis based on the current LOD.
    /// Prevents flip-flopping when the player moves near a LOD boundary.
    /// </summary>
    /// <param name="chebyshevDistance">Chebyshev distance from the player in chunks.</param>
    /// <param name="currentLod">The chunk's current LOD level.</param>
    /// <returns>The updated LOD level (may be the same as current).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LodLevel GetLodWithHysteresis(int chebyshevDistance, LodLevel currentLod)
    {
        switch (currentLod)
        {
            case LodLevel.Lod0:
                if (chebyshevDistance >= Lod0ToLod1Distance)
                {
                    return chebyshevDistance >= Lod1ToLod2Distance
                        ? LodLevel.Lod2
                        : LodLevel.Lod1;
                }

                return LodLevel.Lod0;

            case LodLevel.Lod1:
                if (chebyshevDistance < Lod1ToLod0Distance)
                {
                    return LodLevel.Lod0;
                }

                if (chebyshevDistance >= Lod1ToLod2Distance)
                {
                    return LodLevel.Lod2;
                }

                return LodLevel.Lod1;

            case LodLevel.Lod2:
                if (chebyshevDistance < Lod2ToLod1Distance)
                {
                    return chebyshevDistance < Lod1ToLod0Distance
                        ? LodLevel.Lod0
                        : LodLevel.Lod1;
                }

                return LodLevel.Lod2;

            default:
                return LodLevel.Lod0;
        }
    }

    /// <summary>
    /// Returns the downsampling factor for a given LOD level.
    /// LOD 0 = 1 (no downsampling), LOD 1 = 2, LOD 2 = 4.
    /// </summary>
    /// <param name="lod">The LOD level.</param>
    /// <returns>The block-grouping factor per axis.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDownsampleFactor(LodLevel lod)
    {
        return lod switch
        {
            LodLevel.Lod0 => 1,
            LodLevel.Lod1 => Lod1Factor,
            LodLevel.Lod2 => Lod2Factor,
            _ => 1,
        };
    }
}
