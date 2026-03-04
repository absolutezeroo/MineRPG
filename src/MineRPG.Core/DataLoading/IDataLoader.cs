namespace MineRPG.Core.DataLoading;

/// <summary>
/// Loads typed data objects from persistent storage (JSON files).
/// Injected into systems that need data at startup (registries, etc.).
/// </summary>
public interface IDataLoader
{
    T Load<T>(string filePath) where T : class;

    /// <summary>
    /// Load and deserialize every JSON file in <paramref name="directoryPath"/>.
    /// Returns an empty list if the directory does not exist.
    /// </summary>
    IReadOnlyList<T> LoadAll<T>(string directoryPath, bool recursive = false) where T : class;
}
