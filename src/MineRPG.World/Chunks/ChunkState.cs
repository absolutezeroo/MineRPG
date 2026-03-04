namespace MineRPG.World.Chunks;

public enum ChunkState
{
    Unloaded,
    Queued,
    Generating,
    Generated,
    Meshing,
    Ready,
    Dirty,
}
