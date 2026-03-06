using System.Collections.Generic;

namespace MineRPG.Core.Registry;

/// <summary>
/// A named group of string identifiers, loaded from Data/Tags/*.json.
/// Tags are referenced with a "#" prefix in data files (e.g. "#mineable_pickaxe").
/// </summary>
public sealed class TagDefinition
{
    /// <summary>Unique identifier for this tag (e.g. "mineable_pickaxe").</summary>
    public string TagId { get; init; } = "";

    /// <summary>The string identifiers grouped under this tag.</summary>
    public IReadOnlyList<string> Values { get; init; } = [];
}
