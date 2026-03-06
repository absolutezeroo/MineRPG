using System;
using System.Collections.Generic;

namespace MineRPG.Core.Registry;

/// <summary>
/// Stores tag definitions loaded from Data/Tags/*.json and resolves tag references.
/// Tags are string groups referenced with "#" prefix (e.g. "#mineable_pickaxe").
/// All lookups are case-insensitive.
/// </summary>
public sealed class TagRegistry
{
    /// <summary>The character prefix used to identify tag references (e.g. "#mineable_pickaxe").</summary>
    public const char TagPrefix = '#';

    private readonly Dictionary<string, TagDefinition> _tags =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, HashSet<string>> _valueIndex =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _isFrozen;

    /// <summary>Number of registered tags.</summary>
    public int Count => _tags.Count;

    /// <summary>Whether the registry has been frozen.</summary>
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// Returns true if the string starts with the tag prefix character ('#').
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the value is a tag reference.</returns>
    public static bool IsTagReference(string value) =>
        value.Length > 0 && value[0] == TagPrefix;

    /// <summary>
    /// Strips the tag prefix from a tag reference (e.g. "#mineable_pickaxe" becomes "mineable_pickaxe").
    /// </summary>
    /// <param name="tagReference">The tag reference string with prefix.</param>
    /// <returns>The tag ID without the prefix character.</returns>
    public static string StripTagPrefix(string tagReference) => tagReference.Substring(1);

    /// <summary>
    /// Registers a tag definition. Must be called before <see cref="Freeze"/>.
    /// </summary>
    /// <param name="definition">The tag definition to register.</param>
    /// <exception cref="ArgumentNullException">If definition is null.</exception>
    /// <exception cref="InvalidOperationException">If the registry is frozen or the tag ID is duplicate.</exception>
    public void Register(TagDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (_isFrozen)
        {
            throw new InvalidOperationException(
                $"Cannot register tag '{definition.TagId}': the tag registry is frozen.");
        }

        if (string.IsNullOrWhiteSpace(definition.TagId))
        {
            throw new ArgumentException("Tag ID must not be empty.", nameof(definition));
        }

        if (!_tags.TryAdd(definition.TagId, definition))
        {
            throw new InvalidOperationException(
                $"Tag '{definition.TagId}' is already registered.");
        }

        HashSet<string> valueSet = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < definition.Values.Count; i++)
        {
            valueSet.Add(definition.Values[i]);
        }

        _valueIndex[definition.TagId] = valueSet;
    }

    /// <summary>
    /// Freezes the registry, preventing further registrations.
    /// </summary>
    public void Freeze() => _isFrozen = true;

    /// <summary>
    /// Resolves a reference that may be a tag (prefixed with "#") or a literal value.
    /// If it starts with "#", returns the tag's values. Otherwise returns a single-element list.
    /// </summary>
    /// <param name="tagOrValue">A tag reference (e.g. "#mineable_pickaxe") or a literal value.</param>
    /// <returns>The resolved list of values.</returns>
    /// <exception cref="KeyNotFoundException">If a tag reference is not found.</exception>
    public IReadOnlyList<string> Resolve(string tagOrValue)
    {
        if (string.IsNullOrEmpty(tagOrValue))
        {
            return [];
        }

        if (tagOrValue[0] != TagPrefix)
        {
            return [tagOrValue];
        }

        string tagId = tagOrValue.Substring(1);

        if (!_tags.TryGetValue(tagId, out TagDefinition? definition))
        {
            throw new KeyNotFoundException(
                $"Tag '{tagOrValue}' not found in tag registry.");
        }

        return definition.Values;
    }

    /// <summary>
    /// Resolves a list of references (tags or literal values) into a flat deduplicated list.
    /// </summary>
    /// <param name="references">A list of tag references and/or literal values.</param>
    /// <returns>A flat, deduplicated list of resolved values.</returns>
    public IReadOnlyList<string> ResolveAll(IReadOnlyList<string> references)
    {
        if (references.Count == 0)
        {
            return [];
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        List<string> result = new();

        for (int i = 0; i < references.Count; i++)
        {
            IReadOnlyList<string> resolved = Resolve(references[i]);

            for (int j = 0; j < resolved.Count; j++)
            {
                if (seen.Add(resolved[j]))
                {
                    result.Add(resolved[j]);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks whether a value is contained in the specified tag.
    /// </summary>
    /// <param name="value">The value to look up.</param>
    /// <param name="tagId">The tag ID (without "#" prefix).</param>
    /// <returns>True if the value belongs to the tag; false if the tag does not exist or value is not in it.</returns>
    public bool Contains(string value, string tagId)
    {
        if (!_valueIndex.TryGetValue(tagId, out HashSet<string>? valueSet))
        {
            return false;
        }

        return valueSet.Contains(value);
    }

    /// <summary>
    /// Returns the tag definition for the given ID, or null if not found.
    /// </summary>
    /// <param name="tagId">The tag ID (without "#" prefix).</param>
    /// <param name="definition">The found definition, or null.</param>
    /// <returns>True if the tag exists.</returns>
    public bool TryGet(string tagId, out TagDefinition? definition) => _tags.TryGetValue(tagId, out definition);

    /// <summary>
    /// Returns all registered tag definitions.
    /// </summary>
    /// <returns>A collection of all tag definitions.</returns>
    public IEnumerable<TagDefinition> GetAll() => _tags.Values;
}
