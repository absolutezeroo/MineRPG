using Godot;

using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Options tab for Graphics settings: display, rendering, and quality controls.
/// Layout is defined in Scenes/UI/Options/GraphicsTab.tscn.
/// </summary>
public sealed partial class GraphicsTabPanel : OptionsTabPanel
{
    private static readonly string[] WindowModeLabels = ["Windowed", "Fullscreen", "Borderless"];
    private static readonly string[] MsaaLabels = ["Off", "2x", "4x", "8x"];
    private static readonly string[] ShadowLabels = ["Low", "Medium", "High", "Ultra"];
    private static readonly string[] AfLabels = ["Off", "2x", "4x", "8x", "16x"];

    [Export] private OptionButton _windowModeDropdown = null!;
    [Export] private CheckButton _vsyncToggle = null!;
    [Export] private HSlider _renderDistanceSlider = null!;
    [Export] private Label _renderDistanceValue = null!;
    [Export] private HSlider _fovSlider = null!;
    [Export] private Label _fovValue = null!;
    [Export] private HSlider _brightnessSlider = null!;
    [Export] private Label _brightnessValue = null!;
    [Export] private OptionButton _msaaDropdown = null!;
    [Export] private OptionButton _shadowDropdown = null!;
    [Export] private CheckButton _ssaoToggle = null!;
    [Export] private OptionButton _afDropdown = null!;

    /// <inheritdoc />
    protected override void InitializeTab()
    {
        PopulateDropdowns();
        SetInitialValues();
        ConnectSignals();
    }

    private void PopulateDropdowns()
    {
        PopulateDropdown(_windowModeDropdown, WindowModeLabels);
        PopulateDropdown(_msaaDropdown, MsaaLabels);
        PopulateDropdown(_shadowDropdown, ShadowLabels);
        PopulateDropdown(_afDropdown, AfLabels);
    }

    private void SetInitialValues()
    {
        _windowModeDropdown.Selected = (int)Options.WindowMode;
        _vsyncToggle.ButtonPressed = Options.VSyncEnabled;

        _renderDistanceSlider.Value = Options.RenderDistance;
        _renderDistanceValue.Text = Options.RenderDistance.ToString();

        _fovSlider.Value = Options.FieldOfView;
        _fovValue.Text = $"{(int)Options.FieldOfView}\u00b0";

        _brightnessSlider.Value = Options.Brightness;
        _brightnessValue.Text = Options.Brightness.ToString("F2");

        _msaaDropdown.Selected = (int)Options.MsaaQuality;
        _shadowDropdown.Selected = (int)Options.ShadowQuality;
        _ssaoToggle.ButtonPressed = Options.SsaoEnabled;
        _afDropdown.Selected = (int)Options.AnisotropicFiltering;
    }

    private void ConnectSignals()
    {
        _windowModeDropdown.ItemSelected += OnWindowModeChanged;
        _vsyncToggle.Toggled += OnVSyncToggled;
        _renderDistanceSlider.ValueChanged += OnRenderDistanceChanged;
        _fovSlider.ValueChanged += OnFovChanged;
        _brightnessSlider.ValueChanged += OnBrightnessChanged;
        _msaaDropdown.ItemSelected += OnMsaaChanged;
        _shadowDropdown.ItemSelected += OnShadowQualityChanged;
        _ssaoToggle.Toggled += OnSsaoToggled;
        _afDropdown.ItemSelected += OnAfChanged;
    }

    private static void PopulateDropdown(OptionButton dropdown, string[] labels)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            dropdown.AddItem(labels[i], i);
        }
    }

    private void OnWindowModeChanged(long index) => Options.WindowMode = (WindowModeOption)(int)index;

    private void OnVSyncToggled(bool pressed) => Options.VSyncEnabled = pressed;

    private void OnRenderDistanceChanged(double value)
    {
        Options.RenderDistance = (int)value;
        _renderDistanceValue.Text = ((int)value).ToString();
    }

    private void OnFovChanged(double value)
    {
        Options.FieldOfView = (float)value;
        _fovValue.Text = $"{(int)value}\u00b0";
    }

    private void OnBrightnessChanged(double value)
    {
        Options.Brightness = (float)value;
        _brightnessValue.Text = ((float)value).ToString("F2");
    }

    private void OnMsaaChanged(long index) => Options.MsaaQuality = (MsaaQuality)(int)index;

    private void OnShadowQualityChanged(long index) => Options.ShadowQuality = (ShadowQuality)(int)index;

    private void OnSsaoToggled(bool pressed) => Options.SsaoEnabled = pressed;

    private void OnAfChanged(long index) => Options.AnisotropicFiltering = (AnisotropicFilteringLevel)(int)index;
}
