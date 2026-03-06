namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Per-frame timing breakdown by subsystem.
/// Used by the stacked area graph to visualize where frame time is spent.
/// </summary>
public readonly struct FrameTimeBreakdown
{
    /// <summary>Physics processing time in milliseconds.</summary>
    public double PhysicsMs { get; init; }

    /// <summary>Chunk result drain time in milliseconds.</summary>
    public double ChunkDrainMs { get; init; }

    /// <summary>Node cleanup time in milliseconds.</summary>
    public double CleanupMs { get; init; }

    /// <summary>Rendering time in milliseconds.</summary>
    public double RenderMs { get; init; }

    /// <summary>Idle time (remainder) in milliseconds.</summary>
    public double IdleMs { get; init; }

    /// <summary>
    /// Total frame time (sum of all components).
    /// </summary>
    public double TotalMs => PhysicsMs + ChunkDrainMs + CleanupMs + RenderMs + IdleMs;
}
