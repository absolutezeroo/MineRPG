using MineRPG.Core.DataLoading;

namespace MineRPG.Core.Interfaces;

/// <summary>
/// Persists and loads user settings to/from a config file.
/// Implemented at the Game/composition level where file I/O paths are known.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>Saves the provided settings to disk immediately.</summary>
    /// <param name="settings">The settings snapshot to persist.</param>
    void Save(SettingsData settings);

    /// <summary>Loads settings from disk. Returns defaults if the file is missing or invalid.</summary>
    /// <returns>The loaded settings, or a default instance if unavailable.</returns>
    SettingsData Load();
}
