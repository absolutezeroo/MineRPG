using System.Collections.Concurrent;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Work item for a background remesh operation. Carries the entry, coord,
/// and target result queue.
/// </summary>
internal readonly record struct RemeshWork(
    ChunkEntry Entry,
    ChunkCoord Coord,
    ConcurrentQueue<ChunkEntry> TargetQueue);
