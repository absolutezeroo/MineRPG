using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Container for a list of <see cref="SoundBankEntry"/> items.
/// Loaded as a single JSON file via <see cref="JsonDataLoader.Load{T}"/>.
/// </summary>
public sealed class SoundBank
{
    /// <summary>The list of sound entries in this bank.</summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Setter required for JSON deserialization.")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists",
        Justification = "JSON deserialization requires a concrete List<T>.")]
    public List<SoundBankEntry> Sounds { get; set; } = new();
}
