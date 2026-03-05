using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// Options panel accessible from the pause menu.
/// Provides sliders for mouse sensitivity, master volume, and render distance.
/// </summary>
public sealed partial class OptionsPanelNode : Control
{
    private const float PanelWidth = 400f;
    private const float SliderWidth = 200f;
    private const int TitleFontSize = 28;
    private const int LabelFontSize = 16;
    private const int ButtonFontSize = 18;
    private const float ButtonHeight = 42f;

    private const float MinSensitivity = 0.0005f;
    private const float MaxSensitivity = 0.01f;
    private const float MinVolume = 0f;
    private const float MaxVolume = 1f;
    private const int MinRenderDistance = 4;
    private const int MaxRenderDistance = 64;

    private static readonly Color PanelBgColor = new(0.18f, 0.15f, 0.12f, 0.95f);
    private static readonly Color TitleColor = new(1f, 1f, 1f, 1f);
    private static readonly Color LabelColor = new(0.85f, 0.85f, 0.85f, 1f);

    /// <summary>
    /// Emitted when the player clicks the Back button.
    /// </summary>
    [Signal]
    public delegate void BackRequestedEventHandler();

    private IOptionsProvider _options = null!;
    private ILogger _logger = null!;
    private Label _sensitivityValue = null!;
    private Label _volumeValue = null!;
    private Label _distanceValue = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _options = ServiceLocator.Instance.Get<IOptionsProvider>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Center panel via CenterContainer
        CenterContainer panelCenter = new();
        panelCenter.SetAnchorsPreset(LayoutPreset.FullRect);
        panelCenter.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(panelCenter);

        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(PanelWidth, 360f);

        StyleBoxFlat panelStyle = new();
        panelStyle.BgColor = PanelBgColor;
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = new Color(0.3f, 0.25f, 0.2f, 1f);
        panelStyle.SetContentMarginAll(16);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        panelCenter.AddChild(panel);

        VBoxContainer layout = new();
        layout.AddThemeConstantOverride("separation", 14);
        panel.AddChild(layout);

        // Title
        Label title = new();
        title.Text = "Options";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", TitleColor);
        title.AddThemeFontSizeOverride("font_size", TitleFontSize);
        layout.AddChild(title);

        HSeparator separator = new();
        layout.AddChild(separator);

        // Mouse Sensitivity
        HBoxContainer sensitivityRow = CreateSliderRow(
            "Mouse Sensitivity",
            MinSensitivity, MaxSensitivity,
            _options.MouseSensitivity,
            0.0001f,
            out HSlider sensitivitySlider,
            out _sensitivityValue);
        _sensitivityValue.Text = _options.MouseSensitivity.ToString("F4");
        sensitivitySlider.ValueChanged += OnSensitivityChanged;
        layout.AddChild(sensitivityRow);

        // Master Volume
        HBoxContainer volumeRow = CreateSliderRow(
            "Master Volume",
            MinVolume, MaxVolume,
            _options.MasterVolume,
            0.01f,
            out HSlider volumeSlider,
            out _volumeValue);
        _volumeValue.Text = $"{(int)(_options.MasterVolume * 100)}%";
        volumeSlider.ValueChanged += OnVolumeChanged;
        layout.AddChild(volumeRow);

        // Render Distance
        HBoxContainer distanceRow = CreateSliderRow(
            "Render Distance",
            MinRenderDistance, MaxRenderDistance,
            _options.RenderDistance,
            1f,
            out HSlider distanceSlider,
            out _distanceValue);
        _distanceValue.Text = _options.RenderDistance.ToString();
        distanceSlider.ValueChanged += OnRenderDistanceChanged;
        layout.AddChild(distanceRow);

        // Back button
        Button backButton = new();
        backButton.Text = "Back";
        backButton.CustomMinimumSize = new Vector2(120f, ButtonHeight);
        backButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        backButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        backButton.Pressed += OnBackPressed;
        layout.AddChild(backButton);

        _logger.Info("OptionsPanelNode ready.");
    }

    private static HBoxContainer CreateSliderRow(
        string labelText,
        float minValue, float maxValue, float currentValue, float step,
        out HSlider slider, out Label valueLabel)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 12);

        Label label = new();
        label.Text = labelText;
        label.CustomMinimumSize = new Vector2(160f, 0f);
        label.AddThemeColorOverride("font_color", LabelColor);
        label.AddThemeFontSizeOverride("font_size", LabelFontSize);
        row.AddChild(label);

        slider = new HSlider();
        slider.MinValue = minValue;
        slider.MaxValue = maxValue;
        slider.Value = currentValue;
        slider.Step = step;
        slider.CustomMinimumSize = new Vector2(SliderWidth, 24f);
        slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(slider);

        valueLabel = new Label();
        valueLabel.CustomMinimumSize = new Vector2(60f, 0f);
        valueLabel.AddThemeColorOverride("font_color", LabelColor);
        valueLabel.AddThemeFontSizeOverride("font_size", LabelFontSize);
        row.AddChild(valueLabel);

        return row;
    }

    private void OnSensitivityChanged(double value)
    {
        _options.MouseSensitivity = (float)value;
        _sensitivityValue.Text = ((float)value).ToString("F4");
    }

    private void OnVolumeChanged(double value)
    {
        _options.MasterVolume = (float)value;
        _volumeValue.Text = $"{(int)(value * 100)}%";
    }

    private void OnRenderDistanceChanged(double value)
    {
        _options.RenderDistance = (int)value;
        _distanceValue.Text = ((int)value).ToString();
    }

    private void OnBackPressed() => EmitSignal(SignalName.BackRequested);
}
