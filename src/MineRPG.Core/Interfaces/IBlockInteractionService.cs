namespace MineRPG.Core.Interfaces;

/// <summary>
/// Abstraction for block breaking/placing so bridge nodes can interact
/// with the world without directly referencing MineRPG.World.
/// Implemented in MineRPG.Game where all references are available.
/// </summary>
public interface IBlockInteractionService
{
    /// <summary>
    /// Cast a ray and break the first solid block hit.
    /// </summary>
    /// <param name="originX">Ray origin X coordinate.</param>
    /// <param name="originY">Ray origin Y coordinate.</param>
    /// <param name="originZ">Ray origin Z coordinate.</param>
    /// <param name="dirX">Ray direction X component.</param>
    /// <param name="dirY">Ray direction Y component.</param>
    /// <param name="dirZ">Ray direction Z component.</param>
    /// <param name="maxDistance">Maximum ray distance.</param>
    /// <returns>True if a block was broken.</returns>
    public bool TryBreakBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance);

    /// <summary>
    /// Cast a ray and place a block adjacent to the first solid block hit.
    /// </summary>
    /// <param name="originX">Ray origin X coordinate.</param>
    /// <param name="originY">Ray origin Y coordinate.</param>
    /// <param name="originZ">Ray origin Z coordinate.</param>
    /// <param name="dirX">Ray direction X component.</param>
    /// <param name="dirY">Ray direction Y component.</param>
    /// <param name="dirZ">Ray direction Z component.</param>
    /// <param name="maxDistance">Maximum ray distance.</param>
    /// <param name="blockId">The block type ID to place.</param>
    /// <returns>True if a block was placed.</returns>
    public bool TryPlaceBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance, ushort blockId);
}
