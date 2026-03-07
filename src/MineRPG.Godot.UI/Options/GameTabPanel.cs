using Godot;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Options tab for Game settings: Mouse Sensitivity and Master Volume.
/// Layout is defined in Scenes/UI/Options/GameTab.tscn.
/// </summary>
public sealed partial class GameTabPanel : OptionsTabPanel
{
    [Export] private HSlider _sensitivitySlider = null!;
    [Export] private Label _sensitivityValue = null!;
    [Export] private HSlider _volumeSlider = null!;
    [Export] private Label _volumeValue = null!;

    /// <inheritdoc />
    protected override void InitializeTab()
    {
        _sensitivitySlider.Value = Options.MouseSensitivity;
        _sensitivityValue.Text = Options.MouseSensitivity.ToString("F4");
        _sensitivitySlider.ValueChanged += OnSensitivityChanged;

        _volumeSlider.Value = Options.MasterVolume;
        _volumeValue.Text = $"{(int)(Options.MasterVolume * 100)}%";
        _volumeSlider.ValueChanged += OnVolumeChanged;
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
