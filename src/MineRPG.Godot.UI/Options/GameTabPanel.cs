using Godot;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Options tab for Game settings: Mouse Sensitivity and Master Volume.
/// </summary>
public sealed partial class GameTabPanel : OptionsTabPanel
{
    private const float MinSensitivity = 0.0005f;
    private const float MaxSensitivity = 0.01f;
    private const float MinVolume = 0f;
    private const float MaxVolume = 1f;

    private Label _sensitivityValue = null!;
    private Label _volumeValue = null!;

    /// <inheritdoc />
    protected override void BuildContent(VBoxContainer layout)
    {
        layout.AddChild(CreateSectionHeader("INPUT"));

        HBoxContainer sensitivityRow = CreateSliderRow(
            "Mouse Sensitivity",
            MinSensitivity, MaxSensitivity,
            Options.MouseSensitivity,
            0.0001f,
            out HSlider sensitivitySlider,
            out _sensitivityValue);
        _sensitivityValue.Text = Options.MouseSensitivity.ToString("F4");
        sensitivitySlider.ValueChanged += OnSensitivityChanged;
        layout.AddChild(sensitivityRow);

        layout.AddChild(CreateSectionHeader("AUDIO"));

        HBoxContainer volumeRow = CreateSliderRow(
            "Master Volume",
            MinVolume, MaxVolume,
            Options.MasterVolume,
            0.01f,
            out HSlider volumeSlider,
            out _volumeValue);
        _volumeValue.Text = $"{(int)(Options.MasterVolume * 100)}%";
        volumeSlider.ValueChanged += OnVolumeChanged;
        layout.AddChild(volumeRow);
    }

    private void OnSensitivityChanged(double value)
    {
        Options.MouseSensitivity = (float)value;
        _sensitivityValue.Text = ((float)value).ToString("F4");
    }

    private void OnVolumeChanged(double value)
    {
        Options.MasterVolume = (float)value;
        _volumeValue.Text = $"{(int)(value * 100)}%";
    }
}
