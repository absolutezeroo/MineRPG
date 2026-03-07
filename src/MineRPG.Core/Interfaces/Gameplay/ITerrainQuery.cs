namespace MineRPG.Core.Interfaces.Gameplay;

/// <summary>
/// Provides terrain height queries for physics systems that cannot
/// directly reference MineRPG.World (pure-project constraint).
/// Implemented in MineRPG.Game where all references are available.
/// </summary>
public interface ITerrainQuery
{
    /// <summary>
    /// Returns the Y coordinate of the highest solid block surface
    /// at the given world XZ position. Returns <see cref="FallbackY"/>
    /// if the column is not loaded.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>The surface Y in world units (top face of the block).</returns>
    float GetSurfaceY(float worldX, float worldZ);

    /// <summary>
    /// The Y value returned when the chunk at the queried column is not loaded.
    /// Physics should treat this as "ground not available" and freeze the drop.
    /// </summary>
    float FallbackY { get; }
}
