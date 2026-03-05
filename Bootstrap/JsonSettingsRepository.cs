using System;
using System.IO;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;

using Newtonsoft.Json;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Saves and loads <see cref="SettingsData"/> to/from a JSON file in Godot's user data directory.
/// The file path resolves to user://settings.json per platform.
/// </summary>
public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
    };

    private readonly string _filePath;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonSettingsRepository"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public JsonSettingsRepository(ILogger logger)
    {
        _logger = logger;
        string userDataDir = OS.GetUserDataDir();
        _filePath = Path.Combine(userDataDir, "settings.json");
    }

    /// <inheritdoc />
    public void Save(SettingsData settings)
    {
        try
        {
            string json = JsonConvert.SerializeObject(settings, SerializerSettings);

            // Atomic write: write to temp file then move, preventing truncated files on crash
            string tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
            _logger.Debug("JsonSettingsRepository: Settings saved to '{0}'.", _filePath);
        }
        catch (Exception ex)
        {
            _logger.Error("JsonSettingsRepository: Failed to save settings — {0}", ex.Message);
        }
    }

    /// <inheritdoc />
    public SettingsData Load()
    {
        if (!File.Exists(_filePath))
        {
            _logger.Info("JsonSettingsRepository: No settings file found — using defaults.");
            return new SettingsData();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            SettingsData? result = JsonConvert.DeserializeObject<SettingsData>(json, SerializerSettings);

            if (result is null)
            {
                _logger.Warning("JsonSettingsRepository: Deserialized null — using defaults.");
                return new SettingsData();
            }

            _logger.Info("JsonSettingsRepository: Settings loaded from '{0}'.", _filePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error("JsonSettingsRepository: Failed to load settings — {0}", ex.Message);
            return new SettingsData();
        }
    }
}
