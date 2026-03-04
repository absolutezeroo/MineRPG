using MineRPG.Core.Interfaces;
using MineRPG.Godot.World;
using MineRPG.World.Spatial;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Concrete implementation of IBlockInteractionService.
/// Lives in MineRPG.Game where all project references are available.
/// Bridges between the player (Godot.Entities) and the world (Godot.World).
/// </summary>
public sealed class BlockInteractionService(IVoxelRaycaster raycaster, WorldNode worldNode) : IBlockInteractionService
{
    /// <summary>
    /// Attempts to break the block at the given ray origin and direction.
    /// </summary>
    /// <param name="originX">Ray origin X coordinate.</param>
    /// <param name="originY">Ray origin Y coordinate.</param>
    /// <param name="originZ">Ray origin Z coordinate.</param>
    /// <param name="dirX">Ray direction X component.</param>
    /// <param name="dirY">Ray direction Y component.</param>
    /// <param name="dirZ">Ray direction Z component.</param>
    /// <param name="maxDistance">Maximum raycast distance.</param>
    /// <returns>True if a block was broken, false otherwise.</returns>
    public bool TryBreakBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance)
    {
        VoxelRaycastResult result = raycaster.Cast(originX, originY, originZ, dirX, dirY, dirZ, maxDistance);
        if (!result.Hit)
        {
            return false;
        }

        worldNode.BreakBlock(result.HitPosition);
        return true;
    }

    /// <summary>
    /// Attempts to place a block at the adjacent position of the hit block.
    /// </summary>
    /// <param name="originX">Ray origin X coordinate.</param>
    /// <param name="originY">Ray origin Y coordinate.</param>
    /// <param name="originZ">Ray origin Z coordinate.</param>
    /// <param name="dirX">Ray direction X component.</param>
    /// <param name="dirY">Ray direction Y component.</param>
    /// <param name="dirZ">Ray direction Z component.</param>
    /// <param name="maxDistance">Maximum raycast distance.</param>
    /// <param name="blockId">The block type ID to place.</param>
    /// <returns>True if a block was placed, false otherwise.</returns>
    public bool TryPlaceBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance, ushort blockId)
    {
        VoxelRaycastResult result = raycaster.Cast(originX, originY, originZ, dirX, dirY, dirZ, maxDistance);
        if (!result.Hit)
        {
            return false;
        }

        worldNode.PlaceBlock(result.AdjacentPosition, blockId);
        return true;
    }
}
