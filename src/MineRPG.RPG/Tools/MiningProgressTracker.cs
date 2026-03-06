using MineRPG.Core.Math;

namespace MineRPG.RPG.Tools;

/// <summary>
/// Tracks the progress of an ongoing mining operation.
/// Updated each frame while the player holds the attack button on a block.
/// </summary>
public sealed class MiningProgressTracker
{
    private float _elapsedTime;
    private float _totalBreakTime;

    /// <summary>Whether the player is currently mining a block.</summary>
    public bool IsMining { get; private set; }

    /// <summary>Mining progress from 0.0 (not started) to 1.0 (complete).</summary>
    public float Progress { get; private set; }

    /// <summary>World position of the block being mined.</summary>
    public VoxelPosition3D TargetBlock { get; private set; }

    /// <summary>Raised when a block is fully broken.</summary>
    public event EventHandler<BlockBrokenEventArgs>? BlockBroken;

    /// <summary>Raised when mining is cancelled before completion.</summary>
    public event EventHandler? MiningCancelled;

    /// <summary>Raised when mining progress changes.</summary>
    public event EventHandler<MiningProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    /// Starts mining a block at the given position.
    /// </summary>
    /// <param name="block">The position of the block to mine.</param>
    /// <param name="breakTime">Total time in seconds to break the block.</param>
    public void StartMining(VoxelPosition3D block, float breakTime)
    {
        if (breakTime <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(breakTime), breakTime, "Break time must be positive.");
        }

        IsMining = true;
        TargetBlock = block;
        _totalBreakTime = breakTime;
        _elapsedTime = 0f;
        Progress = 0f;
        ProgressChanged?.Invoke(this, new MiningProgressChangedEventArgs(0f));
    }

    /// <summary>
    /// Updates the mining progress. Should be called each frame while mining.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public void UpdateMining(float deltaTime)
    {
        if (!IsMining)
        {
            return;
        }

        _elapsedTime += deltaTime;
        Progress = Math.Clamp(_elapsedTime / _totalBreakTime, 0f, 1f);
        ProgressChanged?.Invoke(this, new MiningProgressChangedEventArgs(Progress));

        if (IsComplete())
        {
            VoxelPosition3D brokenBlock = TargetBlock;
            IsMining = false;
            Progress = 0f;
            BlockBroken?.Invoke(this, new BlockBrokenEventArgs(brokenBlock));
        }
    }

    /// <summary>
    /// Cancels the current mining operation.
    /// </summary>
    public void CancelMining()
    {
        if (!IsMining)
        {
            return;
        }

        IsMining = false;
        Progress = 0f;
        _elapsedTime = 0f;
        MiningCancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Whether the mining operation has reached completion.
    /// </summary>
    /// <returns>True if progress has reached 1.0.</returns>
    public bool IsComplete()
    {
        return _elapsedTime >= _totalBreakTime;
    }
}
