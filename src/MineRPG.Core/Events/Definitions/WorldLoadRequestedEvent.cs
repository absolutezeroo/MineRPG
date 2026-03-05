using MineRPG.Core.DataLoading;

namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player selects a world to load from the main menu.
/// </summary>
public readonly struct WorldLoadRequestedEvent
{
    /// <summary>Gets the metadata of the world to load.</summary>
    public WorldMeta Meta { get; init; }
}
