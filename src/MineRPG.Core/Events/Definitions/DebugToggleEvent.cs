#if DEBUG
namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when a debug module's visibility is toggled.
/// The ModuleKey identifies which module was toggled.
/// </summary>
public readonly struct DebugToggleEvent
{
    /// <summary>Unique key identifying the debug module.</summary>
    public string ModuleKey { get; init; }

    /// <summary>Whether the module should be visible.</summary>
    public bool Visible { get; init; }
}
#endif
