using MineRPG.Core.Math;

using MineRPG.Godot.World.Chunks;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Deferred scene-tree cleanup for a chunk node. Applied on the main thread
/// within a frame budget by <see cref="ChunkNodeCleaner"/>.
/// </summary>
internal readonly record struct NodeCleanupWork(ChunkCoord Coord, ChunkNode Node);
