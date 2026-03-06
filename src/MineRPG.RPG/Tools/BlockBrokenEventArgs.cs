using MineRPG.Core.Math;

namespace MineRPG.RPG.Tools;

/// <summary>
/// Event data for when a block is fully broken by mining.
/// </summary>
public sealed class BlockBrokenEventArgs : EventArgs
{
    /// <summary>The world position of the block that was broken.</summary>
    public VoxelPosition3D Position { get; }

    /// <summary>
    /// Creates event data for a broken block.
    /// </summary>
    /// <param name="position">The world position of the broken block.</param>
    public BlockBrokenEventArgs(VoxelPosition3D position)
    {
        Position = position;
    }
}
