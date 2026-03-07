using System.Collections.Generic;

using MineRPG.Core.Registry;

namespace MineRPG.RPG.Items;

/// <summary>
/// Specialized registry for item definitions, loaded from Data/Items/*.json at startup.
/// Provides category and tag-based lookups beyond the base registry.
/// Immutable after <see cref="IRegistry{TKey,TValue}.Freeze"/> is called.
/// </summary>
public sealed class ItemRegistry
{
    private readonly Registry<string, ItemDefinition> _inner = new();
    private readonly Dictionary<ItemCategory, List<ItemDefinition>> _byCategory = new();
    private readonly Dictionary<string, List<ItemDefinition>> _byTag = new();

    /// <summary>
    /// Atlas layout for item icons, built when the registry is frozen.
    /// Returns an empty layout before <see cref="Freeze"/> is called.
    /// </summary>
    public ItemIconAtlasLayout IconAtlasLayout { get; private set; } = new(Array.Empty<string>());

    /// <summary>Number of registered item definitions.</summary>
    public int Count => _inner.Count;

    /// <summary>Whether the registry has been frozen.</summary>
    public bool IsFrozen => _inner.IsFrozen;

    /// <summary>
    /// Registers an item definition. Must be called before <see cref="Freeze"/>.
    /// </summary>
    /// <param name="definition">The item definition to register.</param>
    public void Register(ItemDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        _inner.Register(definition.Id, definition);

        if (!_byCategory.TryGetValue(definition.Category, out List<ItemDefinition>? categoryList))
        {
            categoryList = new List<ItemDefinition>();
            _byCategory[definition.Category] = categoryList;
        }

        categoryList.Add(definition);

        for (int i = 0; i < definition.Tags.Count; i++)
        {
            string tag = definition.Tags[i];

            if (!_byTag.TryGetValue(tag, out List<ItemDefinition>? tagList))
            {
                tagList = new List<ItemDefinition>();
                _byTag[tag] = tagList;
            }

            tagList.Add(definition);
        }
    }

    /// <summary>
    /// Freezes the registry, preventing further registrations.
    /// Builds the icon atlas layout from all registered IconAtlasId values.
    /// </summary>
    public void Freeze()
    {
        IReadOnlyList<ItemDefinition> all = _inner.GetAll();
        List<string> iconIds = new(all.Count);

        for (int i = 0; i < all.Count; i++)
        {
            if (!string.IsNullOrEmpty(all[i].IconAtlasId))
            {
                iconIds.Add(all[i].IconAtlasId);
            }
        }

        IconAtlasLayout = new ItemIconAtlasLayout(iconIds);
        _inner.Freeze();
    }

    /// <summary>
    /// Retrieves an item definition by ID. Throws if not found.
    /// </summary>
    /// <param name="id">The item definition ID.</param>
    /// <returns>The item definition.</returns>
    public ItemDefinition Get(string id) => _inner.Get(id);

    /// <summary>
    /// Tries to retrieve an item definition by ID without throwing.
    /// </summary>
    /// <param name="id">The item definition ID.</param>
    /// <param name="definition">The found definition, or null.</param>
    /// <returns>True if found.</returns>
    public bool TryGet(string id, out ItemDefinition definition) => _inner.TryGet(id, out definition!);

    /// <summary>
    /// Returns all registered item definitions in insertion order.
    /// </summary>
    /// <returns>A read-only list of all definitions.</returns>
    public IReadOnlyList<ItemDefinition> GetAll() => _inner.GetAll();

    /// <summary>
    /// Whether the given ID is registered.
    /// </summary>
    /// <param name="id">The item definition ID.</param>
    /// <returns>True if registered.</returns>
    public bool Contains(string id) => _inner.Contains(id);

    /// <summary>
    /// Returns all item definitions in the specified category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>A read-only list of matching definitions, or empty if none.</returns>
    public IReadOnlyList<ItemDefinition> GetByCategory(ItemCategory category)
    {
        if (_byCategory.TryGetValue(category, out List<ItemDefinition>? list))
        {
            return list;
        }

        return [];
    }

    /// <summary>
    /// Returns all item definitions that have the specified tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>A read-only list of matching definitions, or empty if none.</returns>
    public IReadOnlyList<ItemDefinition> GetByTag(string tag)
    {
        if (_byTag.TryGetValue(tag, out List<ItemDefinition>? list))
        {
            return list;
        }

        return [];
    }
}
