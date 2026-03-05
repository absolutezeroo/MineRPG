using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using MineRPG.Core.Logging;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Reads and writes world metadata JSON files.
/// Each world lives at: {savesRoot}/world_{seed}/world_meta.json
/// </summary>
public sealed class WorldRepository
{
    private const string MetaFileName = "world_meta.json";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="WorldRepository"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public WorldRepository(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns the save folder path for the given world ID.
    /// </summary>
    /// <param name="savesRoot">Root directory containing all world saves.</param>
    /// <param name="worldId">The unique world identifier.</param>
    /// <returns>The absolute path to the world's save directory.</returns>
    public static string GetSavePath(string savesRoot, string worldId)
        => Path.Combine(savesRoot, $"world_{worldId}");

    /// <summary>
    /// Lists all worlds found under the saves root directory.
    /// </summary>
    /// <param name="savesRoot">Root directory containing all world saves.</param>
    /// <returns>A list of loaded world metadata entries.</returns>
    public IReadOnlyList<WorldMeta> ListAll(string savesRoot)
    {
        List<WorldMeta> results = new();

        if (!Directory.Exists(savesRoot))
        {
            return results;
        }

        string[] directories = Directory.GetDirectories(savesRoot);

        foreach (string directory in directories)
        {
            string metaPath = Path.Combine(directory, MetaFileName);

            if (!File.Exists(metaPath))
            {
                continue;
            }

            if (TryLoadFromPath(metaPath, out WorldMeta? meta) && meta is not null)
            {
                results.Add(meta);
            }
        }

        return results;
    }

    /// <summary>
    /// Saves metadata for the given world. Creates the directory if needed.
    /// </summary>
    /// <param name="savesRoot">Root directory containing all world saves.</param>
    /// <param name="meta">The world metadata to persist.</param>
    public void SaveMeta(string savesRoot, WorldMeta meta)
    {
        string saveDirectory = GetSavePath(savesRoot, meta.WorldId);
        Directory.CreateDirectory(saveDirectory);

        string metaPath = Path.Combine(saveDirectory, MetaFileName);
        string json = JsonConvert.SerializeObject(meta, Formatting.Indented);
        File.WriteAllText(metaPath, json);

        _logger.Info("WorldRepository: Saved meta for world '{0}' (seed={1}).", meta.Name, meta.Seed);
    }

    /// <summary>
    /// Attempts to load the metadata for a specific world by its unique ID.
    /// </summary>
    /// <param name="savesRoot">Root directory containing all world saves.</param>
    /// <param name="worldId">The unique world identifier.</param>
    /// <param name="meta">The loaded metadata, or null on failure.</param>
    /// <returns>True if the metadata was loaded successfully.</returns>
    public bool TryLoad(string savesRoot, string worldId, out WorldMeta? meta)
    {
        string metaPath = Path.Combine(GetSavePath(savesRoot, worldId), MetaFileName);
        return TryLoadFromPath(metaPath, out meta);
    }

    private bool TryLoadFromPath(string metaPath, out WorldMeta? meta)
    {
        meta = null;

        if (!File.Exists(metaPath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(metaPath);
            meta = JsonConvert.DeserializeObject<WorldMeta>(json);
            return meta is not null;
        }
        catch (Exception exception)
        {
            _logger.Warning("WorldRepository: Failed to load '{0}': {1}", metaPath, exception.Message);
            return false;
        }
    }
}
