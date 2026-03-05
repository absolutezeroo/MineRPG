using System;
using System.IO;

using Newtonsoft.Json;

using MineRPG.Core.Logging;

namespace MineRPG.Core.DataLoading;

/// <summary>
/// Reads and writes player save data JSON files.
/// Each world stores one player save at: {worldSaveDirectory}/player_save.json
/// Mirrors the conventions of <see cref="WorldRepository"/>.
/// </summary>
public sealed class PlayerRepository
{
    private const string SaveFileName = "player_save.json";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PlayerRepository"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public PlayerRepository(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns the absolute path to the player save file for the given world directory.
    /// </summary>
    /// <param name="worldSaveDirectory">Absolute path to the world's save directory.</param>
    /// <returns>The absolute path to player_save.json.</returns>
    public static string GetSavePath(string worldSaveDirectory)
        => Path.Combine(worldSaveDirectory, SaveFileName);

    /// <summary>
    /// Saves the player state to disk. Creates the directory if needed.
    /// </summary>
    /// <param name="worldSaveDirectory">Absolute path to the world's save directory.</param>
    /// <param name="data">The player save data to persist.</param>
    public void Save(string worldSaveDirectory, PlayerSaveData data)
    {
        Directory.CreateDirectory(worldSaveDirectory);

        string savePath = GetSavePath(worldSaveDirectory);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(savePath, json);

        _logger.Info(
            "PlayerRepository: Saved player at ({0:F1}, {1:F1}, {2:F1}).",
            data.PositionX, data.PositionY, data.PositionZ);
    }

    /// <summary>
    /// Attempts to load the player save for the given world directory.
    /// Returns false and null if the file does not exist or cannot be parsed.
    /// </summary>
    /// <param name="worldSaveDirectory">Absolute path to the world's save directory.</param>
    /// <param name="data">The loaded player save, or null on failure.</param>
    /// <returns>True if the save was loaded successfully.</returns>
    public bool TryLoad(string worldSaveDirectory, out PlayerSaveData? data)
    {
        data = null;

        string savePath = GetSavePath(worldSaveDirectory);

        if (!File.Exists(savePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            data = JsonConvert.DeserializeObject<PlayerSaveData>(json);
            return data is not null;
        }
        catch (Exception exception)
        {
            _logger.Warning(
                "PlayerRepository: Failed to load '{0}': {1}",
                savePath, exception.Message);
            return false;
        }
    }
}
