namespace MineRPG.World.Meshing;

/// <summary>
/// Level of detail for chunk mesh resolution.
/// Higher LOD values mean fewer vertices and lower visual fidelity.
/// </summary>
public enum LodLevel
{
    /// <summary>Full resolution: 1 block = 1 block. Used for nearby chunks.</summary>
    Lod0 = 0,

    /// <summary>Half resolution: 2x2x2 blocks merged into 1 mega-block. Used for mid-range chunks.</summary>
    Lod1 = 1,

    /// <summary>Quarter resolution: 4x4x4 blocks merged into 1 mega-block. Used for distant chunks.</summary>
    Lod2 = 2,
}
