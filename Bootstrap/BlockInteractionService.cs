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
    public bool TryBreakBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance)
    {
        var result = raycaster.Cast(originX, originY, originZ, dirX, dirY, dirZ, maxDistance);
        if (!result.Hit)
            return false;

        worldNode.BreakBlock(result.HitPosition);
        return true;
    }

    public bool TryPlaceBlock(float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ, float maxDistance, ushort blockId)
    {
        var result = raycaster.Cast(originX, originY, originZ, dirX, dirY, dirZ, maxDistance);
        if (!result.Hit)
            return false;

        worldNode.PlaceBlock(result.AdjacentPosition, blockId);
        return true;
    }
}
