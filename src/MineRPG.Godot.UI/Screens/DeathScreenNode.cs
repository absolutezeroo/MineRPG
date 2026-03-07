using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Full-screen death overlay shown when the player dies.
/// Displays "You Died" text and a Respawn button.
/// On click, publishes <see cref="RespawnRequestedEvent"/>.
/// </summary>
public sealed partial class DeathScreenNode : Control
{
    private const int TitleFontSize = 48;
    private const int SubtitleFontSize = 16;
    private const int ButtonFontSize = 20;

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;

    private Label _deathPositionLabel = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        // Must process even when tree is paused so respawn works
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        BuildLayout();

        _eventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        _eventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);

        _logger.Info("DeathScreenNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        _eventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        _deathPositionLabel.Text = $"Died at ({evt.PositionX:F0}, {evt.PositionY:F0}, {evt.PositionZ:F0})";
        Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void OnPlayerRespawned(PlayerRespawnedEvent evt)
    {
        Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void OnRespawnPressed() => _eventBus.Publish(new RespawnRequestedEvent());

    private void BuildLayout()
    {
        // Dark overlay background
        ColorRect overlay = new();
        overlay.Name = "Overlay";
        overlay.Color = new Color(0.0f, 0.0f, 0.0f, 0.75f);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(overlay);

        // Centered content container
        VBoxContainer content = new();
        content.Name = "Content";
        content.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        content.GrowHorizontal = GrowDirection.Both;
        content.GrowVertical = GrowDirection.Both;
        content.OffsetLeft = -200f;
        content.OffsetRight = 200f;
        content.OffsetTop = -120f;
        content.OffsetBottom = 120f;
        content.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddThemeConstantOverride("separation", 16);
        content.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(content);

        // "You Died" title
        Label titleLabel = new();
        titleLabel.Name = "TitleLabel";
        titleLabel.Text = "You Died";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", TitleFontSize);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.15f, 0.15f, 1.0f));
        titleLabel.MouseFilter = MouseFilterEnum.Ignore;
        content.AddChild(titleLabel);

        // Death position subtitle
        _deathPositionLabel = new Label();
        _deathPositionLabel.Name = "DeathPositionLabel";
        _deathPositionLabel.Text = "";
        _deathPositionLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _deathPositionLabel.AddThemeFontSizeOverride("font_size", SubtitleFontSize);
        _deathPositionLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f, 1.0f));
        _deathPositionLabel.MouseFilter = MouseFilterEnum.Ignore;
        content.AddChild(_deathPositionLabel);

        // Spacer
        Control spacer = new();
        spacer.Name = "Spacer";
        spacer.CustomMinimumSize = new Vector2(0f, 20f);
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        content.AddChild(spacer);

        // Respawn button
        Button respawnButton = new();
        respawnButton.Name = "RespawnButton";
        respawnButton.Text = "Respawn";
        respawnButton.CustomMinimumSize = new Vector2(180f, 50f);
        respawnButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        respawnButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        respawnButton.Pressed += OnRespawnPressed;
        content.AddChild(respawnButton);
    }
}
