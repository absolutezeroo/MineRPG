using System.Collections.Generic;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Loads typed data objects from persistent storage (JSON files).
/// Injected into systems that need data at startup (registries, etc.).
/// </summary>
public interface IDataLoader
{
    /// <summary>
    /// Load and deserialize a single file into <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target deserialization type.</typeparam>
    /// <param name="filePath">Path to the JSON file, absolute or relative to the data root.</param>
    /// <returns>The deserialized object.</returns>
    T Load<T>(string filePath) where T : class;

    /// <summary>
    /// Load and deserialize every JSON file in <paramref name="directoryPath"/>.
    /// Returns an empty list if the directory does not exist.
    /// </summary>
    /// <typeparam name="T">The target deserialization type.</typeparam>
    /// <param name="directoryPath">Path to the directory, absolute or relative to the data root.</param>
    /// <param name="recursive">Whether to search subdirectories recursively.</param>
    /// <returns>A list of deserialized objects.</returns>
    IReadOnlyList<T> LoadAll<T>(string directoryPath, bool recursive = false) where T : class;
}
