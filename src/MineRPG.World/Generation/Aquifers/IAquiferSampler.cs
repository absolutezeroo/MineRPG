namespace MineRPG.World.Generation.Aquifers;

/// <summary>
/// Determines the fluid state of underground cavities.
/// Returns the block ID to place in a carved position (water, lava, or air).
/// </summary>
public interface IAquiferSampler
{
    /// <summary>
    /// Returns the block ID to place at a carved underground position.
    /// Returns 0 (air) if the cavity is dry, or a fluid block ID if flooded.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="surfaceY">Surface height at this column.</param>
    /// <returns>Block ID: 0 for air, water ID, or lava ID.</returns>
    public ushort GetFluidBlock(int worldX, int worldY, int worldZ, int surfaceY);
}
