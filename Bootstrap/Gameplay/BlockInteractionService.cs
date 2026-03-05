using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.World.Blocks;
using MineRPG.World.Spatial;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Concrete implementation of IBlockInteractionService.
/// Lives in MineRPG.Game where all project references are available.
/// Bridges between the player (Godot.Entities) and the world (Godot.World).
/// </summary>
public sealed class BlockInteractionService(
    IVoxelRaycaster raycaster,
    WorldNode worldNode,
    BlockRegistry blockRegistry,
    PlayerData playerData,
    ILogger logger) : IBlockInteractionService
{
    /// <summary>Half-width of the player capsule on X/Z axes.</summary>
    private const float PlayerHalfWidth = 0.3f;

    /// <summary>Half-height of the player capsule on Y axis.</summary>
    private const float PlayerHalfHeight = 0.9f;

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

        BlockDefinition block = blockRegistry.Get(result.BlockId);

        if (block.Hardness < 0f)
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

        WorldPosition target = result.AdjacentPosition;

        if (BlockOverlapsPlayer(target))
        {
            logger.Debug("TryPlaceBlock: rejected — block at {0} overlaps player", target);
            return false;
        }

        worldNode.PlaceBlock(target, blockId);
        return true;
    }

    /// <summary>
    /// Checks whether a block at the given world position would overlap the player's body.
    /// Uses an AABB approximation of the player capsule (radius 0.3, height 1.8).
    /// PositionY is the capsule center (Godot CharacterBody3D with centered CollisionShape3D).
    /// </summary>
    private bool BlockOverlapsPlayer(WorldPosition blockPos)
    {
        float px = playerData.PositionX;
        float py = playerData.PositionY;
        float pz = playerData.PositionZ;

        // Block occupies [bx, bx+1) x [by, by+1) x [bz, bz+1)
        // Player AABB: [px-hw, px+hw) x [py-hh, py+hh) x [pz-hw, pz+hw)
        int bx = blockPos.X;
        int by = blockPos.Y;
        int bz = blockPos.Z;

        return bx < px + PlayerHalfWidth && px - PlayerHalfWidth < bx + 1
            && by < py + PlayerHalfHeight && py - PlayerHalfHeight < by + 1
            && bz < pz + PlayerHalfWidth && pz - PlayerHalfWidth < bz + 1;
    }
}
