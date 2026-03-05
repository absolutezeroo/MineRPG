namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when a player successfully mines a block to completion.
/// Distinct from <see cref="MineRPG.World.Events.BlockChangedEvent"/>
/// which is a world-layer event about any block mutation.
/// This event carries context for future quest tracking and loot drops.
/// </summary>
public readonly struct BlockMinedEvent
{
    /// <summary>World X coordinate of the mined block.</summary>
    public int X { get; init; }

    /// <summary>World Y coordinate of the mined block.</summary>
    public int Y { get; init; }

    /// <summary>World Z coordinate of the mined block.</summary>
    public int Z { get; init; }

    /// <summary>Block type identifier of the block that was mined.</summary>
    public ushort BlockId { get; init; }

    /// <summary>Whether the correct tool was used (affects future loot drops).</summary>
    public bool UsedCorrectTool { get; init; }
}
