using System;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Persistent metadata for a saved world.
/// Stored as JSON in each world's save directory.
/// </summary>
public sealed class WorldMeta
{
    /// <summary>Gets or sets the unique identifier for the world's save directory.</summary>
    public string WorldId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Gets or sets the display name of the world.</summary>
    public string Name { get; set; } = "New World";

    /// <summary>Gets or sets the world generation seed.</summary>
    public int Seed { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC last-played timestamp.</summary>
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
}