#if DEBUG
using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A collapsible section with a title header for the debug menu.
/// Click the header to expand/collapse the content.
/// </summary>
public sealed partial class DebugSection : VBoxContainer
{
    private readonly string _title;
    private readonly bool _startExpanded;

    private VBoxContainer _content = null!;
    private Button _headerButton = null!;
    private bool _isExpanded;

    /// <summary>
    /// The content container where child controls should be added.
    /// </summary>
    public VBoxContainer Content => _content;

    /// <summary>
    /// Creates a debug section.
    /// </summary>
    /// <param name="title">Section title.</param>
    /// <param name="startExpanded">Whether the section starts expanded.</param>
    public DebugSection(string title, bool startExpanded = true)
    {
        _title = title;
        _startExpanded = startExpanded;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        _isExpanded = _startExpanded;

        _headerButton = new Button();
        _headerButton.Flat = true;
        _headerButton.Alignment = HorizontalAlignment.Left;
        _headerButton.AddThemeColorOverride("font_color", DebugTheme.TextAccent);
        _headerButton.AddThemeFontSizeOverride("font_size", DebugTheme.FontSizeSmall);

        StyleBoxEmpty emptyStyle = new();
        _headerButton.AddThemeStyleboxOverride("normal", emptyStyle);
        _headerButton.AddThemeStyleboxOverride("hover", emptyStyle);
        _headerButton.AddThemeStyleboxOverride("pressed", emptyStyle);
        _headerButton.AddThemeStyleboxOverride("focus", emptyStyle);

        _headerButton.Text = GetHeaderText();
        _headerButton.Pressed += OnHeaderPressed;
        AddChild(_headerButton);

        _content = new VBoxContainer();
        _content.Visible = _isExpanded;
        _content.AddThemeConstantOverride("separation", 2);
        AddChild(_content);

        AddThemeConstantOverride("separation", 2);
    }

    private void OnHeaderPressed()
    {
        _isExpanded = !_isExpanded;
        _content.Visible = _isExpanded;
        _headerButton.Text = GetHeaderText();
    }

    private string GetHeaderText()
    {
        string arrow = _isExpanded ? "v" : ">";
        return $"{arrow} {_title}";
    }
}
#endif
