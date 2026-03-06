namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when a tool breaks due to durability depletion.
/// </summary>
public readonly struct ToolBrokenEvent
{
    /// <summary>The item definition ID of the broken tool.</summary>
    public string ItemId { get; init; }

    /// <summary>The hotbar slot index where the tool was equipped.</summary>
    public int SlotIndex { get; init; }
}
