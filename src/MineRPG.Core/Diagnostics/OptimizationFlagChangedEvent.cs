namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Published when an optimization flag changes value.
/// Systems subscribe to react (e.g., trigger remesh when greedy meshing is toggled).
/// </summary>
public readonly struct OptimizationFlagChangedEvent
{
    /// <summary>Name of the flag that changed (e.g., "GreedyMeshingEnabled").</summary>
    public string FlagName { get; init; }

    /// <summary>The new value of the flag.</summary>
    public bool NewValue { get; init; }

    /// <summary>
    /// Whether this change requires chunk regeneration (vs just remeshing).
    /// True for generation-affecting flags like caves, decorators, surface rules.
    /// </summary>
    public bool RequiresRegeneration { get; init; }
}
