namespace MineRPG.Core.Interfaces.Gameplay;

/// <summary>
/// Abstraction for block mining and placing so bridge nodes can interact
/// with the world without directly referencing MineRPG.World.
/// Implemented in MineRPG.Game where all references are available.
/// </summary>
public interface IBlockInteractionService
{
    /// <summary>
    /// Advances a progressive mining operation toward the block at the given ray.
    /// Should be called every physics frame while the attack button is held.
    /// Automatically cancels if the target block changes or the ray misses.
    /// </summary>
    /// <param name="originX">Ray origin X coordinate.</param>
    /// <param name="originY">Ray origin Y coordinate.</param>
    /// <param name="originZ">Ray origin Z coordinate.</param>
    /// <param name="dirX">Ray direction X component.</param>
    /// <param name="dirY">Ray direction Y component.</param>
    /// <param name="dirZ">Ray direction Z component.</param>
    /// <param name="maxDistance">Maximum ray distance.</param>
    /// <param name="deltaTime">Elapsed time in seconds since the last frame.</param>
    void TickMining(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance, float deltaTime);

    /// <summary>
    /// Cancels any active mining operation immediately.
    /// Publishes a MiningProgressChangedEvent with IsActive=false.
    /// </summary>
    void CancelMining();

    /// <summary>
    /// Cast a ray and place a block adjacent to the first solid block hit.
    /// The block type is determined by the currently held hotbar item.
    /// Returns false if the held item is not placeable or the ray misses.
    /// </summary>
    /// <param name="originX">Ray origin X coordinate.</param>
    /// <param name="originY">Ray origin Y coordinate.</param>
    /// <param name="originZ">Ray origin Z coordinate.</param>
    /// <param name="dirX">Ray direction X component.</param>
    /// <param name="dirY">Ray direction Y component.</param>
    /// <param name="dirZ">Ray direction Z component.</param>
    /// <param name="maxDistance">Maximum ray distance.</param>
    /// <returns>True if a block was placed.</returns>
    bool TryPlaceBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance);
}
