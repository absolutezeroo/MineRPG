using System;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;
using MineRPG.Godot.World.Chunks;

using Environment = Godot.Environment;

namespace MineRPG.Game.Bootstrap.Settings;

/// <summary>
/// Applies video/graphics settings to Godot engine APIs:
/// render distance, FOV, fullscreen, VSync, MSAA, shadows, SSAO,
/// anisotropic filtering, and brightness.
/// </summary>
internal sealed class VideoOptionsApplicator
{
    private const int MinRenderDistance = 4;
    private const int MaxRenderDistance = 64;
    private const float MinFov = 40f;
    private const float MaxFov = 120f;
    private const float MinBrightness = 0.5f;
    private const float MaxBrightness = 2.0f;

    private readonly ILogger _logger;

    /// <summary>
    /// Creates a video options applicator.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public VideoOptionsApplicator(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>Gets or sets the chunk render distance.</summary>
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

            _logger.Debug("VideoOptions: RenderDistance={0}", clamped);
        }
    }

    /// <summary>Gets or sets whether VSync is enabled.</summary>
    public bool VSyncEnabled
    {
        get => DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
        set
        {
            DisplayServer.WindowSetVsyncMode(
                value ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
            _logger.Debug("VideoOptions: VSyncEnabled={0}", value);
        }
    }

    /// <summary>Gets or sets the window display mode.</summary>
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

            _logger.Debug("VideoOptions: WindowMode={0}", value);
        }
    }

    /// <summary>Gets or sets the MSAA quality.</summary>
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
            _logger.Debug("VideoOptions: MsaaQuality={0}", value);
        }
    }

    /// <summary>Gets or sets the shadow quality preset.</summary>
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
            _logger.Debug("VideoOptions: ShadowQuality={0}", value);
        }
    }

    /// <summary>Gets or sets whether SSAO is enabled.</summary>
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

            _logger.Debug("VideoOptions: SsaoEnabled={0}", value);
        }
    }

    /// <summary>Gets or sets the anisotropic filtering level.</summary>
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
            _logger.Debug("VideoOptions: AnisotropicFiltering={0}", value);
        }
    }

    /// <summary>Gets or sets the camera field of view in degrees.</summary>
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

            _logger.Debug("VideoOptions: FieldOfView={0}", clamped);
        }
    }

    /// <summary>Gets or sets the brightness multiplier.</summary>
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

            _logger.Debug("VideoOptions: Brightness={0}", clamped);
        }
    }

    private static Viewport GetMainViewport()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
        {
            return tree.Root;
        }

        throw new InvalidOperationException(
            "VideoOptionsApplicator: Cannot access main viewport — SceneTree not available.");
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
