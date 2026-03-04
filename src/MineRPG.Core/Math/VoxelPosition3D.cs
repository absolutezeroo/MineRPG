namespace MineRPG.Core.Math;

/// <summary>
/// A 3D integer position within a voxel chunk.
/// </summary>
/// <param name="X">X coordinate.</param>
/// <param name="Y">Y coordinate.</param>
/// <param name="Z">Z coordinate.</param>
public readonly record struct VoxelPosition3D(int X, int Y, int Z);
