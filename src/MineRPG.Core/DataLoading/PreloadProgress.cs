using System.Threading;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Tracks how many chunks have been meshed during the initial world preload.
/// Thread-safe via <see cref="Interlocked"/> for lock-free increment.
/// Registered in the ServiceLocator by CompositionRoot during world wiring.
/// </summary>
public sealed class PreloadProgress
{
    private int _meshedCount;

    /// <summary>
    /// Initializes a new instance of <see cref="PreloadProgress"/>.
    /// </summary>
    /// <param name="required">Total chunks that must be meshed before gameplay begins.</param>
    public PreloadProgress(int required)
    {
        Required = required;
    }

    /// <summary>Gets the total number of chunks required before gameplay begins.</summary>
    public int Required { get; }

    /// <summary>Gets the number of chunks meshed so far.</summary>
    public int MeshedCount => _meshedCount;

    /// <summary>Gets a value indicating whether the required chunk count has been reached.</summary>
    public bool IsComplete => _meshedCount >= Required;

    /// <summary>
    /// Increments the meshed chunk count by one in a thread-safe manner.
    /// </summary>
    /// <returns>The new meshed count after incrementing.</returns>
    public int Increment() => Interlocked.Increment(ref _meshedCount);
}
