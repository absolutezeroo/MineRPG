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
    /// Returns true if a block was broken.
    /// </summary>
    bool TryBreakBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance);

    /// <summary>
    /// Cast a ray and place a block adjacent to the first solid block hit.
    /// Returns true if a block was placed.
    /// </summary>
    bool TryPlaceBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance, ushort blockId);
}
