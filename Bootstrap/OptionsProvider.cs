using System;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;

using Environment = Godot.Environment;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Bridges <see cref="IOptionsProvider"/> to Godot engine APIs and PlayerData.
/// Applies all settings from <see cref="SettingsData"/> on construction
/// and persists changes immediately via <see cref="ISettingsRepository"/>.
/// </summary>
public sealed class OptionsProvider : IOptionsProvider
{
    private const int MinRenderDistance = 4;
    private const int MaxRenderDistance = 64;
    private const float MinFov = 40f;
    private const float MaxFov = 120f;
    private const float MinBrightness = 0.5f;
    private const float MaxBrightness = 2.0f;

    private static readonly StringName MasterBusName = new("Master");

    private readonly PlayerData _playerData;
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
        _playerData = playerData;
        _settingsRepo = settingsRepo;
        _logger = logger;
        _cachedKeybinds = new Dictionary<string, KeybindData>(initialSettings.Keybinds);

        ApplyAllSettings(initialSettings);
    }

    /// <inheritdoc />
    public float MouseSensitivity
    {
        get => _playerData.MovementSettings.MouseSensitivity;
        set
        {
            _playerData.MovementSettings.MouseSensitivity = value;
            SaveSnapshot();
            _logger.Debug("OptionsProvider: MouseSensitivity={0}", value);
        }
    }

    /// <inheritdoc />
    public float MasterVolume
    {
        get
        {
            int busIndex = AudioServer.GetBusIndex(MasterBusName);
            return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
        }
        set
        {
            int busIndex = AudioServer.GetBusIndex(MasterBusName);
            AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(value));
            SaveSnapshot();
            _logger.Debug("OptionsProvider: MasterVolume={0}", value);
        }
    }

    /// <inheritdoc />
    public int RenderDistance
    {
        get
        {
            if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(
                out ChunkLoadingScheduler? scheduler))
            {
                return scheduler.CurrentRenderDistance;
            }

            return ChunkLoadingScheduler.DefaultRenderDistance;
        }
        set
        {
            int clamped = Math.Clamp(value, MinRenderDistance, MaxRenderDistance);

            if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(
                out ChunkLoadingScheduler? scheduler))
            {
                scheduler.SetRenderDistance(clamped);
            }

            SaveSnapshot();
            _logger.Debug("OptionsProvider: RenderDistance={0}", clamped);
        }
    }

    /// <inheritdoc />
    public bool VSyncEnabled
    {
        get => DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
        set
        {
            DisplayServer.WindowSetVsyncMode(
                value ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
            SaveSnapshot();
            _logger.Debug("OptionsProvider: VSyncEnabled={0}", value);
        }
    }

    /// <inheritdoc />
    public WindowModeOption WindowMode
    {
        get
        {
            DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();

            if (mode == DisplayServer.WindowMode.Fullscreen
                || mode == DisplayServer.WindowMode.ExclusiveFullscreen)
            {
                return WindowModeOption.Fullscreen;
            }

            bool isBorderless = DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.Borderless);

            if (isBorderless)
            {
                return WindowModeOption.Borderless;
            }

            return WindowModeOption.Windowed;
        }
        set
        {
            switch (value)
            {
                case WindowModeOption.Fullscreen:
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                    break;

                case WindowModeOption.Borderless:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
                    break;

                case WindowModeOption.Windowed:
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(value), value, "Unhandled WindowModeOption");
            }

            SaveSnapshot();
            _logger.Debug("OptionsProvider: WindowMode={0}", value);
        }
    }

    /// <inheritdoc />
    public MsaaQuality MsaaQuality
    {
        get
        {
            Viewport.Msaa msaa = GetMainViewport().Msaa3D;

            return msaa switch
            {
                Viewport.Msaa.Disabled => MsaaQuality.Disabled,
                Viewport.Msaa.Msaa2X => MsaaQuality.Msaa2x,
                Viewport.Msaa.Msaa4X => MsaaQuality.Msaa4x,
                Viewport.Msaa.Msaa8X => MsaaQuality.Msaa8x,
                _ => MsaaQuality.Disabled,
            };
        }
        set
        {
            Viewport.Msaa godotMsaa = value switch
            {
                MsaaQuality.Disabled => Viewport.Msaa.Disabled,
                MsaaQuality.Msaa2x => Viewport.Msaa.Msaa2X,
                MsaaQuality.Msaa4x => Viewport.Msaa.Msaa4X,
                MsaaQuality.Msaa8x => Viewport.Msaa.Msaa8X,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Unhandled MsaaQuality"),
            };

            GetMainViewport().Msaa3D = godotMsaa;
            SaveSnapshot();
            _logger.Debug("OptionsProvider: MsaaQuality={0}", value);
        }
    }

    /// <inheritdoc />
    public ShadowQuality ShadowQuality
    {
        get
        {
            int filterQuality = (int)ProjectSettings.GetSetting(
                "rendering/lights_and_shadows/directional_shadow/soft_shadow_filter_quality");

            return filterQuality switch
            {
                0 or 1 => ShadowQuality.Low,
                2 => ShadowQuality.Medium,
                3 => ShadowQuality.High,
                _ => ShadowQuality.Ultra,
            };
        }
        set
        {
            int shadowSize = value switch
            {
                ShadowQuality.Low => 1024,
                ShadowQuality.Medium => 2048,
                ShadowQuality.High => 4096,
                ShadowQuality.Ultra => 8192,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Unhandled ShadowQuality"),
            };

            int filterQuality = value switch
            {
                ShadowQuality.Low => 0,
                ShadowQuality.Medium => 2,
                ShadowQuality.High => 3,
                ShadowQuality.Ultra => 4,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Unhandled ShadowQuality"),
            };

            RenderingServer.DirectionalShadowAtlasSetSize(shadowSize, true);
            ProjectSettings.SetSetting(
                "rendering/lights_and_shadows/directional_shadow/soft_shadow_filter_quality",
                filterQuality);
            SaveSnapshot();
            _logger.Debug("OptionsProvider: ShadowQuality={0}", value);
        }
    }

    /// <inheritdoc />
    public bool SsaoEnabled
    {
        get => GetWorldEnvironment()?.SsaoEnabled ?? false;
        set
        {
            Environment? env = GetWorldEnvironment();

            if (env is not null)
            {
                env.SsaoEnabled = value;
            }

            SaveSnapshot();
            _logger.Debug("OptionsProvider: SsaoEnabled={0}", value);
        }
    }

    /// <inheritdoc />
    public AnisotropicFilteringLevel AnisotropicFiltering
    {
        get
        {
            int level = (int)ProjectSettings.GetSetting(
                "rendering/textures/default_filters/anisotropic_filtering_level");

            return level switch
            {
                0 or 1 => AnisotropicFilteringLevel.Disabled,
                2 => AnisotropicFilteringLevel.AF2x,
                4 => AnisotropicFilteringLevel.AF4x,
                8 => AnisotropicFilteringLevel.AF8x,
                16 => AnisotropicFilteringLevel.AF16x,
                _ => AnisotropicFilteringLevel.Disabled,
            };
        }
        set
        {
            int level = value switch
            {
                AnisotropicFilteringLevel.Disabled => 0,
                AnisotropicFilteringLevel.AF2x => 2,
                AnisotropicFilteringLevel.AF4x => 4,
                AnisotropicFilteringLevel.AF8x => 8,
                AnisotropicFilteringLevel.AF16x => 16,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Unhandled AnisotropicFilteringLevel"),
            };

            ProjectSettings.SetSetting(
                "rendering/textures/default_filters/anisotropic_filtering_level", level);
            SaveSnapshot();
            _logger.Debug("OptionsProvider: AnisotropicFiltering={0}", value);
        }
    }

    /// <inheritdoc />
    public float FieldOfView
    {
        get
        {
            if (ServiceLocator.Instance.TryGet<Camera3D>(out Camera3D? camera))
            {
                return camera.Fov;
            }

            return 75f;
        }
        set
        {
            float clamped = Math.Clamp(value, MinFov, MaxFov);

            if (ServiceLocator.Instance.TryGet<Camera3D>(out Camera3D? camera))
            {
                camera.Fov = clamped;
            }

            SaveSnapshot();
            _logger.Debug("OptionsProvider: FieldOfView={0}", clamped);
        }
    }

    /// <inheritdoc />
    public float Brightness
    {
        get => GetWorldEnvironment()?.AdjustmentBrightness ?? 1.0f;
        set
        {
            float clamped = Math.Clamp(value, MinBrightness, MaxBrightness);
            Environment? env = GetWorldEnvironment();

            if (env is not null)
            {
                env.AdjustmentEnabled = true;
                env.AdjustmentBrightness = clamped;
            }

            SaveSnapshot();
            _logger.Debug("OptionsProvider: Brightness={0}", clamped);
        }
    }

    private void ApplyAllSettings(SettingsData settings)
    {
        // Apply engine settings first (order matters for some)
        VSyncEnabled = settings.VSyncEnabled;
        WindowMode = settings.WindowMode;
        MsaaQuality = settings.MsaaQuality;
        ShadowQuality = settings.ShadowQuality;
        SsaoEnabled = settings.SsaoEnabled;
        AnisotropicFiltering = settings.AnisotropicFiltering;
        Brightness = settings.Brightness;

        // Audio
        MasterVolume = settings.MasterVolume;

        // Player-dependent
        MouseSensitivity = settings.MouseSensitivity;

        // Chunk scheduler may not be ready yet; will use default as fallback
        RenderDistance = settings.RenderDistance;

        // Camera3D may not be registered yet; stored in file, applied when slider is used
        FieldOfView = settings.FieldOfView;

        _logger.Info("OptionsProvider: Applied all settings from snapshot.");
    }

    private void SaveSnapshot()
    {
        SettingsData snapshot = BuildSnapshot();
        _settingsRepo.Save(snapshot);
    }

    /// <summary>
    /// Updates the in-memory keybinds cache and persists the full settings snapshot.
    /// Called by ControlsTabPanel after rebinding a key.
    /// </summary>
    /// <param name="keybinds">The updated keybind dictionary.</param>
    public void UpdateKeybindsAndSave(Dictionary<string, KeybindData> keybinds)
    {
        _cachedKeybinds = keybinds;
        SaveSnapshot();
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

    private static Viewport GetMainViewport()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
        {
            return tree.Root;
        }

        throw new InvalidOperationException(
            "OptionsProvider: Cannot access main viewport — SceneTree not available.");
    }

    private static Environment? GetWorldEnvironment()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return null;
        }

        return tree.Root.World3D?.Environment;
    }
}
