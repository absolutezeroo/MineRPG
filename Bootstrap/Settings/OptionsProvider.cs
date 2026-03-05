using System.Collections.Generic;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;

namespace MineRPG.Game.Bootstrap.Settings;

/// <summary>
/// Facade that implements <see cref="IOptionsProvider"/> by delegating to
/// category-specific applicators. Centralizes persistence via
/// <see cref="ISettingsRepository"/>.
/// </summary>
public sealed class OptionsProvider : IOptionsProvider
{
    private readonly VideoOptionsApplicator _video;
    private readonly AudioOptionsApplicator _audio;
    private readonly GameplayOptionsApplicator _gameplay;
    private readonly ISettingsRepository _settingsRepo;
    private readonly ILogger _logger;
    private Dictionary<string, KeybindData> _cachedKeybinds;

    /// <summary>
    /// Initializes a new instance and applies all settings from the provided snapshot.
    /// </summary>
    /// <param name="playerData">The player data containing movement settings.</param>
    /// <param name="settingsRepo">Repository for persisting settings.</param>
    /// <param name="initialSettings">The initial settings to apply.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public OptionsProvider(
        PlayerData playerData,
        ISettingsRepository settingsRepo,
        SettingsData initialSettings,
        ILogger logger)
    {
        _settingsRepo = settingsRepo;
        _logger = logger;
        _cachedKeybinds = new Dictionary<string, KeybindData>(initialSettings.Keybinds);

        _video = new VideoOptionsApplicator(logger);
        _audio = new AudioOptionsApplicator(logger);
        _gameplay = new GameplayOptionsApplicator(playerData, logger);

        ApplyAllSettings(initialSettings);
    }

    /// <inheritdoc />
    public float MouseSensitivity
    {
        get => _gameplay.MouseSensitivity;
        set
        {
            _gameplay.MouseSensitivity = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public float MasterVolume
    {
        get => _audio.MasterVolume;
        set
        {
            _audio.MasterVolume = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public int RenderDistance
    {
        get => _video.RenderDistance;
        set
        {
            _video.RenderDistance = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public bool VSyncEnabled
    {
        get => _video.VSyncEnabled;
        set
        {
            _video.VSyncEnabled = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public WindowModeOption WindowMode
    {
        get => _video.WindowMode;
        set
        {
            _video.WindowMode = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public MsaaQuality MsaaQuality
    {
        get => _video.MsaaQuality;
        set
        {
            _video.MsaaQuality = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public ShadowQuality ShadowQuality
    {
        get => _video.ShadowQuality;
        set
        {
            _video.ShadowQuality = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public bool SsaoEnabled
    {
        get => _video.SsaoEnabled;
        set
        {
            _video.SsaoEnabled = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public AnisotropicFilteringLevel AnisotropicFiltering
    {
        get => _video.AnisotropicFiltering;
        set
        {
            _video.AnisotropicFiltering = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public float FieldOfView
    {
        get => _video.FieldOfView;
        set
        {
            _video.FieldOfView = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public float Brightness
    {
        get => _video.Brightness;
        set
        {
            _video.Brightness = value;
            SaveSnapshot();
        }
    }

    /// <inheritdoc />
    public void UpdateKeybindsAndSave(Dictionary<string, KeybindData> keybinds)
    {
        _cachedKeybinds = keybinds;
        SaveSnapshot();
    }

    private void ApplyAllSettings(SettingsData settings)
    {
        VSyncEnabled = settings.VSyncEnabled;
        WindowMode = settings.WindowMode;
        MsaaQuality = settings.MsaaQuality;
        ShadowQuality = settings.ShadowQuality;
        SsaoEnabled = settings.SsaoEnabled;
        AnisotropicFiltering = settings.AnisotropicFiltering;
        Brightness = settings.Brightness;
        MasterVolume = settings.MasterVolume;
        MouseSensitivity = settings.MouseSensitivity;
        RenderDistance = settings.RenderDistance;
        FieldOfView = settings.FieldOfView;

        _logger.Info("OptionsProvider: Applied all settings from snapshot.");
    }

    private void SaveSnapshot()
    {
        SettingsData snapshot = BuildSnapshot();
        _settingsRepo.Save(snapshot);
    }

    private SettingsData BuildSnapshot()
    {
        return new SettingsData
        {
            MouseSensitivity = MouseSensitivity,
            MasterVolume = MasterVolume,
            RenderDistance = RenderDistance,
            VSyncEnabled = VSyncEnabled,
            WindowMode = WindowMode,
            MsaaQuality = MsaaQuality,
            ShadowQuality = ShadowQuality,
            SsaoEnabled = SsaoEnabled,
            AnisotropicFiltering = AnisotropicFiltering,
            FieldOfView = FieldOfView,
            Brightness = Brightness,
            Keybinds = _cachedKeybinds,
        };
    }
}
