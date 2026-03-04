using MineRPG.Core.Logging;
using Newtonsoft.Json;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Loads data files from disk and deserializes them with Newtonsoft.Json.
/// </summary>
public sealed class JsonDataLoader : IDataLoader
{
    private readonly ILogger _logger;
    private readonly string _dataRoot;

    private static readonly JsonSerializerSettings Settings = new()
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
    };

    public JsonDataLoader(ILogger logger, string dataRoot)
    {
        _logger = logger;
        _dataRoot = dataRoot ?? throw new ArgumentNullException(nameof(dataRoot));
    }

    /// <summary>
    /// Backwards-compatible constructor. Falls back to DataPath.Root.
    /// Prefer the two-argument constructor for explicit root injection.
    /// </summary>
    public JsonDataLoader(ILogger logger)
        : this(logger, DataPath.Root)
    {
    }

    public T Load<T>(string filePath) where T : class
    {
        var resolved = ResolvePath(filePath);

        if (!File.Exists(resolved))
            throw new FileNotFoundException(
                $"Data file not found: '{resolved}'", resolved);

        var json = File.ReadAllText(resolved);
        var result = JsonConvert.DeserializeObject<T>(json, Settings);

        if (result is null)
            throw new InvalidOperationException(
                $"Deserializing '{resolved}' into {typeof(T).Name} returned null.");

        _logger.Debug("Loaded {0} from '{1}'", typeof(T).Name, resolved);
        return result;
    }

    public IReadOnlyList<T> LoadAll<T>(string directoryPath, bool recursive = false) where T : class
    {
        var resolved = ResolvePath(directoryPath);

        if (!Directory.Exists(resolved))
        {
            _logger.Warning("DataLoader: Directory '{0}' does not exist — returning empty list.", resolved);
            return [];
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(resolved, "*.json", option);
        var results = new List<T>(files.Length);

        foreach (var file in files)
        {
            try
            {
                var item = Load<T>(file);
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
            return path;

        return Path.Combine(_dataRoot, path);
    }
}
