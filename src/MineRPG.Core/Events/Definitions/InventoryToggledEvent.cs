namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the inventory screen is opened or closed.
/// </summary>
public readonly struct InventoryToggledEvent
{
    /// <summary>
    /// Whether the inventory screen is currently open.
    /// </summary>
    public bool IsOpen { get; init; }
}
