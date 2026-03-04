using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using MineRPG.Core.Logging;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Loads data files from disk and deserializes them with Newtonsoft.Json.
/// </summary>
public sealed class JsonDataLoader : IDataLoader
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
    };

    private readonly ILogger _logger;
    private readonly string _dataRoot;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonDataLoader"/> with an explicit data root.
    /// </summary>
    /// <param name="logger">Logger for load diagnostics.</param>
    /// <param name="dataRoot">Absolute path to the data root directory.</param>
    public JsonDataLoader(ILogger logger, string dataRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataRoot = dataRoot ?? throw new ArgumentNullException(nameof(dataRoot));
    }

    /// <summary>
    /// Backwards-compatible constructor. Falls back to DataPath.Root.
    /// Prefer the two-argument constructor for explicit root injection.
    /// </summary>
    /// <param name="logger">Logger for load diagnostics.</param>
    public JsonDataLoader(ILogger logger)
        : this(logger, DataPath.Root)
    {
    }

    /// <inheritdoc />
    public T Load<T>(string filePath) where T : class
    {
        string resolved = ResolvePath(filePath);

        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException(
                $"Data file not found: '{resolved}'", resolved);
        }

        string json = File.ReadAllText(resolved);
        T? result = JsonConvert.DeserializeObject<T>(json, SerializerSettings);

        if (result is null)
        {
            throw new InvalidOperationException(
                $"Deserializing '{resolved}' into {typeof(T).Name} returned null.");
        }

        _logger.Debug("Loaded {0} from '{1}'", typeof(T).Name, resolved);
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<T> LoadAll<T>(string directoryPath, bool recursive = false) where T : class
    {
        string resolved = ResolvePath(directoryPath);

        if (!Directory.Exists(resolved))
        {
            _logger.Warning("DataLoader: Directory '{0}' does not exist — returning empty list.", resolved);
            return [];
        }

        SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(resolved, "*.json", option);
        List<T> results = new(files.Length);

        foreach (string file in files)
        {
            try
            {
                T item = Load<T>(file);
                results.Add(item);
            }
            catch (Exception ex)
            {
                // Continue loading remaining files — partial failure is recoverable
                _logger.Error("DataLoader: Failed to load '{0}' as {1}", ex, file, typeof(T).Name);
            }
        }

        _logger.Info("DataLoader: Loaded {0} {1} entries from '{2}'", results.Count, typeof(T).Name, resolved);
        return results;
    }

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(_dataRoot, path);
    }
}
