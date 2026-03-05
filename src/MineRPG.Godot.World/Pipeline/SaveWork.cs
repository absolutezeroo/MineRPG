using MineRPG.Core.Math;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Work item for a background chunk save. Carries the coordinate and a
/// pre-copied block snapshot. The snapshot array is owned by this work item.
/// </summary>
internal readonly record struct SaveWork(ChunkCoord Coord, ushort[] BlockSnapshot);
