namespace MineRPG.Entities.Player;

/// <summary>
/// Runtime state for the player's active block-mining operation.
/// Tracks the target block, accumulated progress, and current crack stage.
/// Uses raw integer coordinates to avoid depending on MineRPG.World.
/// </summary>
public sealed class MiningState
{
    private const int CrackStageCount = 10;

    /// <summary>Whether a mining operation is currently in progress.</summary>
    public bool IsActive { get; private set; }

    /// <summary>World X coordinate of the block being mined.</summary>
    public int TargetX { get; private set; }

    /// <summary>World Y coordinate of the block being mined.</summary>
    public int TargetY { get; private set; }

    /// <summary>World Z coordinate of the block being mined.</summary>
    public int TargetZ { get; private set; }

    /// <summary>Mining progress from 0.0 (not started) to 1.0 (complete).</summary>
    public float Progress { get; private set; }

    /// <summary>Visual crack stage index from 0 (no crack) to 10 (broken).</summary>
    public int CrackStage => (int)(Progress * CrackStageCount);

    /// <summary>Whether the current mining operation has reached 100% progress.</summary>
    public bool IsComplete => Progress >= 1f;

    /// <summary>
    /// Starts a new mining operation targeting the block at the given coordinates.
    /// Resets progress to zero.
    /// </summary>
    /// <param name="x">World X coordinate of the target block.</param>
    /// <param name="y">World Y coordinate of the target block.</param>
    /// <param name="z">World Z coordinate of the target block.</param>
    public void Start(int x, int y, int z)
    {
        TargetX = x;
        TargetY = y;
        TargetZ = z;
        Progress = 0f;
        IsActive = true;
    }

    /// <summary>
    /// Advances progress toward completion.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick in seconds.</param>
    /// <param name="totalMineTime">Total time required to mine this block in seconds.</param>
    public void Advance(float deltaTime, float totalMineTime)
    {
        if (!IsActive || totalMineTime <= 0f)
        {
            return;
        }

        Progress += deltaTime / totalMineTime;

        if (Progress > 1f)
        {
            Progress = 1f;
        }
    }

    /// <summary>
    /// Returns true if the given coordinates match the current mining target.
    /// </summary>
    /// <param name="x">World X coordinate to check.</param>
    /// <param name="y">World Y coordinate to check.</param>
    /// <param name="z">World Z coordinate to check.</param>
    /// <returns>True if these coordinates are the current mining target.</returns>
    public bool IsTargeting(int x, int y, int z) => IsActive && TargetX == x && TargetY == y && TargetZ == z;

    /// <summary>
    /// Cancels the current mining operation and resets all state.
    /// </summary>
    public void Cancel()
    {
        IsActive = false;
        Progress = 0f;
    }
}
