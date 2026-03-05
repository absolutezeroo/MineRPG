using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Abstract base for options tab content panels.
/// Subclasses implement <see cref="BuildContent"/> to populate their rows.
/// Provides factory helpers that match the existing UI palette conventions.
/// </summary>
public abstract partial class OptionsTabPanel : Control
{
    /// <summary>Width of the label column in slider/toggle/dropdown rows.</summary>
    protected const float LabelColumnWidth = 200f;

    /// <summary>Width of the slider control.</summary>
    protected const float SliderWidth = 180f;

    /// <summary>Width of the value readout label next to sliders.</summary>
    protected const float ValueLabelWidth = 70f;

    /// <summary>Default height for control rows.</summary>
    protected const float RowHeight = 28f;

    /// <summary>Font size for labels in option rows.</summary>
    protected const int LabelFontSize = 16;

    /// <summary>Font size for section header labels.</summary>
    protected const int SectionHeaderFontSize = 14;

    /// <summary>Standard text color for labels.</summary>
    protected static readonly Color LabelColor = new(0.85f, 0.85f, 0.85f, 1f);

    /// <summary>Green accent color for section headers.</summary>
    protected static readonly Color SectionHeaderColor = new(0.6f, 0.75f, 0.6f, 1f);

    /// <summary>Yellow accent color for elements in listening/active state.</summary>
    protected static readonly Color ListeningColor = new(1f, 0.85f, 0.3f, 1f);

    /// <summary>The options provider resolved from <see cref="ServiceLocator"/>.</summary>
    protected IOptionsProvider Options { get; private set; } = null!;

    /// <summary>The logger resolved from <see cref="ServiceLocator"/>.</summary>
    protected ILogger Logger { get; private set; } = null!;

    /// <summary>
    /// Resolves services and calls <see cref="BuildContent"/> to populate the tab.
    /// </summary>
    public override void _Ready()
    {
        Options = ServiceLocator.Instance.Get<IOptionsProvider>();
        Logger = ServiceLocator.Instance.Get<ILogger>();

        VBoxContainer layout = new();
        layout.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        layout.SizeFlagsVertical = SizeFlags.ExpandFill;
        layout.AddThemeConstantOverride("separation", 10);
        AddChild(layout);

        BuildContent(layout);
    }

    /// <summary>
    /// Override to add controls to the tab's vertical layout container.
    /// </summary>
    /// <param name="layout">The root vertical layout for this tab's content.</param>
    protected abstract void BuildContent(VBoxContainer layout);

    /// <summary>
    /// Creates a section header label with the green accent color.
    /// </summary>
    /// <param name="text">The section header text.</param>
    /// <returns>A styled label for section headers.</returns>
    protected static Label CreateSectionHeader(string text)
    {
        Label header = new();
        header.Text = text;
        header.AddThemeColorOverride("font_color", SectionHeaderColor);
        header.AddThemeFontSizeOverride("font_size", SectionHeaderFontSize);
        return header;
    }

    /// <summary>
    /// Creates a labeled HSlider row with a value readout label.
    /// </summary>
    /// <param name="labelText">Display label for the setting.</param>
    /// <param name="minValue">Minimum slider value.</param>
    /// <param name="maxValue">Maximum slider value.</param>
    /// <param name="currentValue">Current slider value.</param>
    /// <param name="step">Slider step increment.</param>
    /// <param name="slider">Out: the created HSlider for connecting ValueChanged.</param>
    /// <param name="valueLabel">Out: the value readout label for updates.</param>
    /// <returns>The row container.</returns>
    protected static HBoxContainer CreateSliderRow(
        string labelText,
        float minValue,
        float maxValue,
        float currentValue,
        float step,
        out HSlider slider,
        out Label valueLabel)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);

        Label label = new();
        label.Text = labelText;
        label.CustomMinimumSize = new Vector2(LabelColumnWidth, 0f);
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
        valueLabel.CustomMinimumSize = new Vector2(ValueLabelWidth, 0f);
        valueLabel.AddThemeColorOverride("font_color", LabelColor);
        valueLabel.AddThemeFontSizeOverride("font_size", LabelFontSize);
        row.AddChild(valueLabel);

        return row;
    }

    /// <summary>
    /// Creates a labeled CheckButton toggle row.
    /// </summary>
    /// <param name="labelText">Display label for the setting.</param>
    /// <param name="currentValue">Current toggle state.</param>
    /// <param name="toggle">Out: the created CheckButton for connecting Toggled.</param>
    /// <returns>The row container.</returns>
    protected static HBoxContainer CreateToggleRow(
        string labelText,
        bool currentValue,
        out CheckButton toggle)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);

        Label label = new();
        label.Text = labelText;
        label.CustomMinimumSize = new Vector2(LabelColumnWidth, 0f);
        label.AddThemeColorOverride("font_color", LabelColor);
        label.AddThemeFontSizeOverride("font_size", LabelFontSize);
        row.AddChild(label);

        toggle = new CheckButton();
        toggle.ButtonPressed = currentValue;
        row.AddChild(toggle);

        return row;
    }

    /// <summary>
    /// Creates a labeled OptionButton dropdown row.
    /// </summary>
    /// <param name="labelText">Display label for the setting.</param>
    /// <param name="optionLabels">Array of dropdown option display strings.</param>
    /// <param name="selectedIndex">Currently selected index.</param>
    /// <param name="dropdown">Out: the created OptionButton for connecting ItemSelected.</param>
    /// <returns>The row container.</returns>
    protected static HBoxContainer CreateDropdownRow(
        string labelText,
        string[] optionLabels,
        int selectedIndex,
        out OptionButton dropdown)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);

        Label label = new();
        label.Text = labelText;
        label.CustomMinimumSize = new Vector2(LabelColumnWidth, 0f);
        label.AddThemeColorOverride("font_color", LabelColor);
        label.AddThemeFontSizeOverride("font_size", LabelFontSize);
        row.AddChild(label);

        dropdown = new OptionButton();
        dropdown.CustomMinimumSize = new Vector2(160f, RowHeight);

        for (int i = 0; i < optionLabels.Length; i++)
        {
            dropdown.AddItem(optionLabels[i], i);
        }

        dropdown.Selected = selectedIndex;
        row.AddChild(dropdown);

        return row;
    }
}
