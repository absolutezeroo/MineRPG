namespace MineRPG.World.Meshing;

/// <summary>
/// The six face directions. Integer value matches VoxelMath.FaceDirections index.
/// </summary>
public enum FaceDirection
{
    /// <summary>+X direction.</summary>
    Right = 0,

    /// <summary>-X direction.</summary>
    Left = 1,

    /// <summary>+Y direction.</summary>
    Up = 2,

    /// <summary>-Y direction.</summary>
    Down = 3,

    /// <summary>+Z direction.</summary>
    Front = 4,

    /// <summary>-Z direction.</summary>
    Back = 5,
}
