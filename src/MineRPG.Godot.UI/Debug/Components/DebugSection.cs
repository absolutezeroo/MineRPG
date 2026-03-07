#if DEBUG
using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A collapsible section with a title header for the debug menu.
/// Click the header to expand/collapse the content.
/// Layout is defined in Scenes/UI/Debug/Widgets/DebugSection.tscn.
/// </summary>
public sealed partial class DebugSection : VBoxContainer
{
    private const string ScenePath = "res://Scenes/UI/Debug/Widgets/DebugSection.tscn";

    private static PackedScene? _sceneCache;

    [Export] private Button _headerButton = null!;

    private string _title = string.Empty;
    private bool _isExpanded;

    /// <summary>
    /// The content container where child controls should be added.
    /// </summary>
    public VBoxContainer Content { get; private set; } = null!;

    /// <summary>
    /// Creates and initializes a DebugSection from the scene template.
    /// </summary>
    /// <param name="title">Section title.</param>
    /// <param name="startExpanded">Whether the section starts expanded.</param>
    /// <returns>The configured section instance.</returns>
    public static DebugSection Create(string title, bool startExpanded = true)
    {
        _sceneCache ??= GD.Load<PackedScene>(ScenePath);
        DebugSection instance = _sceneCache.Instantiate<DebugSection>();
        instance._title = title;
        instance._isExpanded = startExpanded;
        return instance;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Content = GetNode<VBoxContainer>("Content");
        Content.Visible = _isExpanded;

        StyleBoxEmpty emptyStyle = new();
        _headerButton.AddThemeStyleboxOverride("normal", emptyStyle);
        _headerButton.AddThemeStyleboxOverride("hover", emptyStyle);
        _headerButton.AddThemeStyleboxOverride("pressed", emptyStyle);
        _headerButton.AddThemeStyleboxOverride("focus", emptyStyle);

        _headerButton.Text = GetHeaderText();
        _headerButton.Pressed += OnHeaderPressed;
    }

    private void OnHeaderPressed()
    {
        _isExpanded = !_isExpanded;
        Content.Visible = _isExpanded;
        _headerButton.Text = GetHeaderText();
    }

    private string GetHeaderText()
    {
        string arrow = _isExpanded ? "v" : ">";
        return $"{arrow} {_title}";
    }
}
#endif
