namespace MineRPG.Core.DataLoading;

/// <summary>
/// Resolves data file paths relative to a configured root directory.
/// The root is set once at application startup by GameBootstrapper.
/// </summary>
public static class DataPath
{
    private static volatile string _root = Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>
    /// Deprecated: prefer passing dataRoot via JsonDataLoader constructor.
    /// Kept for backward compatibility with code that reads DataPath.Root directly.
    /// </summary>
    public static string Root => _root;

    /// <summary>
    /// Deprecated: prefer passing dataRoot via JsonDataLoader constructor.
    /// Configure the data root. Call from GameBootstrapper before loading any data.
    /// </summary>
    public static void SetRoot(string absolutePath)
    {
        if (!Directory.Exists(absolutePath))
            throw new DirectoryNotFoundException(
                $"Data root directory does not exist: '{absolutePath}'");

        _root = absolutePath;
    }

    public static string Combine(params string[] relativePath)
        => Path.Combine(_root, Path.Combine(relativePath));

    public static string SubDirectory(string name)
        => Path.Combine(_root, name);
}
