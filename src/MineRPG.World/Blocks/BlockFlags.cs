using System;

namespace MineRPG.World.Blocks;

/// <summary>
/// Behavioral flags for a block type. Combined via bitwise OR.
/// </summary>
[Flags]
public enum BlockFlags
{
    /// <summary>No flags set.</summary>
    None         = 0,

    /// <summary>Block has physical collision.</summary>
    Solid        = 1 << 0,

    /// <summary>Block allows light to pass through.</summary>
    Transparent  = 1 << 1,

    /// <summary>Block is a liquid (water, lava).</summary>
    Liquid       = 1 << 2,

    /// <summary>Block emits light.</summary>
    Emissive     = 1 << 3,

    /// <summary>Block can be interacted with (chests, doors).</summary>
    Interactable = 1 << 4,
}
