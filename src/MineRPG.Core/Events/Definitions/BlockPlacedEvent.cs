namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when a player places a block in the world.
/// </summary>
public readonly struct BlockPlacedEvent
{
    /// <summary>World X coordinate where the block was placed.</summary>
    public int X { get; init; }

    /// <summary>World Y coordinate where the block was placed.</summary>
    public int Y { get; init; }

    /// <summary>World Z coordinate where the block was placed.</summary>
    public int Z { get; init; }

    /// <summary>Block type identifier that was placed.</summary>
    public ushort BlockId { get; init; }

    /// <summary>Item ID that was consumed to place the block.</summary>
    public string ItemId { get; init; }
}
