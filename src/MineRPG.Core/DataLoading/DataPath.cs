using System;
using System.IO;

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
    /// <param name="absolutePath">The absolute path to the data root directory.</param>
    public static void SetRoot(string absolutePath)
    {
        if (!Directory.Exists(absolutePath))
        {
            throw new DirectoryNotFoundException(
                $"Data root directory does not exist: '{absolutePath}'");
        }

        _root = absolutePath;
    }

    /// <summary>
    /// Combines the data root with one or more relative path segments.
    /// </summary>
    /// <param name="relativePath">Path segments relative to the data root.</param>
    /// <returns>The fully resolved path.</returns>
    public static string Combine(params string[] relativePath) => Path.Combine(_root, Path.Combine(relativePath));

    /// <summary>
    /// Returns the path to a named subdirectory under the data root.
    /// </summary>
    /// <param name="name">The subdirectory name.</param>
    /// <returns>The fully resolved subdirectory path.</returns>
    public static string SubDirectory(string name) => Path.Combine(_root, name);
}
