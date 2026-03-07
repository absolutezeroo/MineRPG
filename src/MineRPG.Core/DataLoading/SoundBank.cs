using System.Collections.Generic;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Container for a list of <see cref="SoundBankEntry"/> items.
/// Loaded as a single JSON file via <see cref="JsonDataLoader.Load{T}"/>.
/// </summary>
public sealed class SoundBank
{
    /// <summary>The list of sound entries in this bank.</summary>
    public List<SoundBankEntry> Sounds { get; set; } = new();
}
