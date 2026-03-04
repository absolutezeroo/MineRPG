using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators.Trees;

/// <summary>
/// Generates a tree structure in chunk data at a specified position.
/// </summary>
public interface ITreeGenerator
{
    /// <summary>
    /// The unique type identifier for this tree generator (e.g., "oak_tree").
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Places a tree at the given world position into the chunk data.
    /// Only writes blocks within the chunk's local bounds.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="localX">Local X position of the trunk base.</param>
    /// <param name="baseY">Y position of the trunk base.</param>
    /// <param name="localZ">Local Z position of the trunk base.</param>
    /// <param name="random">Seeded random for height and shape variation.</param>
    void Generate(ChunkData data, int localX, int baseY, int localZ, System.Random random);
}
