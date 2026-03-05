using Godot;

using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Options tab for Graphics settings: display, rendering, and quality controls.
/// </summary>
public sealed partial class GraphicsTabPanel : OptionsTabPanel
{
    private const int MinRenderDistance = 4;
    private const int MaxRenderDistance = 64;
    private const float MinFov = 40f;
    private const float MaxFov = 120f;
    private const float MinBrightness = 0.5f;
    private const float MaxBrightness = 2.0f;

    private static readonly string[] WindowModeLabels = ["Windowed", "Fullscreen", "Borderless"];
    private static readonly string[] MsaaLabels = ["Off", "2x", "4x", "8x"];
    private static readonly string[] ShadowLabels = ["Low", "Medium", "High", "Ultra"];
    private static readonly string[] AfLabels = ["Off", "2x", "4x", "8x", "16x"];

    private Label _renderDistanceValue = null!;
    private Label _fovValue = null!;
    private Label _brightnessValue = null!;

    /// <inheritdoc />
    protected override void BuildContent(VBoxContainer layout)
    {
        layout.AddChild(CreateSectionHeader("DISPLAY"));

        HBoxContainer windowModeRow = CreateDropdownRow(
            "Window Mode",
            WindowModeLabels,
            (int)Options.WindowMode,
            out OptionButton windowModeDropdown);
        windowModeDropdown.ItemSelected += OnWindowModeChanged;
        layout.AddChild(windowModeRow);

        HBoxContainer vsyncRow = CreateToggleRow(
            "VSync",
            Options.VSyncEnabled,
            out CheckButton vsyncToggle);
        vsyncToggle.Toggled += OnVSyncToggled;
        layout.AddChild(vsyncRow);

        layout.AddChild(CreateSectionHeader("RENDERING"));

        HBoxContainer renderDistRow = CreateSliderRow(
            "Render Distance",
            MinRenderDistance, MaxRenderDistance,
            Options.RenderDistance,
            1f,
            out HSlider renderDistSlider,
            out _renderDistanceValue);
        _renderDistanceValue.Text = Options.RenderDistance.ToString();
        renderDistSlider.ValueChanged += OnRenderDistanceChanged;
        layout.AddChild(renderDistRow);

        HBoxContainer fovRow = CreateSliderRow(
            "Field of View",
            MinFov, MaxFov,
            Options.FieldOfView,
            1f,
            out HSlider fovSlider,
            out _fovValue);
        _fovValue.Text = $"{(int)Options.FieldOfView}\u00b0";
        fovSlider.ValueChanged += OnFovChanged;
        layout.AddChild(fovRow);

        HBoxContainer brightnessRow = CreateSliderRow(
            "Brightness",
            MinBrightness, MaxBrightness,
            Options.Brightness,
            0.05f,
            out HSlider brightnessSlider,
            out _brightnessValue);
        _brightnessValue.Text = Options.Brightness.ToString("F2");
        brightnessSlider.ValueChanged += OnBrightnessChanged;
        layout.AddChild(brightnessRow);

        layout.AddChild(CreateSectionHeader("QUALITY"));

        HBoxContainer msaaRow = CreateDropdownRow(
            "MSAA",
            MsaaLabels,
            (int)Options.MsaaQuality,
            out OptionButton msaaDropdown);
        msaaDropdown.ItemSelected += OnMsaaChanged;
        layout.AddChild(msaaRow);

        HBoxContainer shadowRow = CreateDropdownRow(
            "Shadow Quality",
            ShadowLabels,
            (int)Options.ShadowQuality,
            out OptionButton shadowDropdown);
        shadowDropdown.ItemSelected += OnShadowQualityChanged;
        layout.AddChild(shadowRow);

        HBoxContainer ssaoRow = CreateToggleRow(
            "SSAO",
            Options.SsaoEnabled,
            out CheckButton ssaoToggle);
        ssaoToggle.Toggled += OnSsaoToggled;
        layout.AddChild(ssaoRow);

        HBoxContainer afRow = CreateDropdownRow(
            "Anisotropic Filtering",
            AfLabels,
            (int)Options.AnisotropicFiltering,
            out OptionButton afDropdown);
        afDropdown.ItemSelected += OnAfChanged;
        layout.AddChild(afRow);
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
