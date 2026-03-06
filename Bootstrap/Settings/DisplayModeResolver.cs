using System;

using Godot;

using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;

namespace MineRPG.Game.Bootstrap.Settings;

/// <summary>
/// Resolves and applies display mode, MSAA, and shadow quality settings
/// to Godot rendering APIs. Provides mapping between game enums and
/// Godot engine values.
/// </summary>
internal sealed class DisplayModeResolver
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a display mode resolver.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public DisplayModeResolver(ILogger logger)
    {
        _logger = logger;
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

    /// <summary>
    /// Gets the main viewport from the scene tree.
    /// </summary>
    /// <returns>The root viewport.</returns>
    public static Viewport GetMainViewport()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
        {
            return tree.Root;
        }

        throw new InvalidOperationException(
            "DisplayModeResolver: Cannot access main viewport — SceneTree not available.");
    }
}
