namespace MineRPG.RPG.Tools;

/// <summary>
/// Event data for when mining progress changes.
/// </summary>
public sealed class MiningProgressChangedEventArgs : EventArgs
{
    /// <summary>The current mining progress from 0.0 to 1.0.</summary>
    public float Progress { get; }

    /// <summary>
    /// Creates event data for a mining progress change.
    /// </summary>
    /// <param name="progress">The current mining progress from 0.0 to 1.0.</param>
    public MiningProgressChangedEventArgs(float progress)
    {
        Progress = progress;
    }
}
