using System;
using System.Collections.Generic;
using System.IO;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// Panel that lists existing saved worlds and provides a form to create new ones.
/// Publishes <see cref="WorldLoadRequestedEvent"/> when a world is selected or created.
/// </summary>
public sealed partial class WorldSelectionPanelNode : Control
{
    private const float PanelWidth = 500f;
    private const float ButtonHeight = 42f;
    private const float FieldHeight = 36f;
    private const int HeaderFontSize = 28;
    private const int LabelFontSize = 16;
    private const int ButtonFontSize = 18;

    private static readonly Color PanelBgColor = new(0.15f, 0.12f, 0.1f, 0.95f);
    private static readonly Color SectionBgColor = new(0.2f, 0.17f, 0.14f, 0.9f);
    private static readonly Color WorldEntryColor = new(0.25f, 0.22f, 0.18f, 0.85f);
    private static readonly Color WorldEntryHoverColor = new(0.35f, 0.3f, 0.25f, 0.9f);
    private static readonly Color HeaderColor = new(1f, 1f, 1f, 1f);
    private static readonly Color SubTextColor = new(0.7f, 0.7f, 0.65f, 1f);

    /// <summary>
    /// Emitted when the player clicks the Back button.
    /// </summary>
    [Signal]
    public delegate void BackRequestedEventHandler();

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private WorldRepository _worldRepository = null!;
    private string _savesRoot = string.Empty;
    private VBoxContainer _worldListContainer = null!;
    private LineEdit _nameField = null!;
    private LineEdit _seedField = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();
        _worldRepository = ServiceLocator.Instance.Get<WorldRepository>();

        string dataRoot = ProjectSettings.GlobalizePath("res://Data");
        _savesRoot = Path.Combine(
            Path.GetDirectoryName(dataRoot) ?? dataRoot, "Saves");

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Center panel via CenterContainer
        CenterContainer panelCenter = new();
        panelCenter.SetAnchorsPreset(LayoutPreset.FullRect);
        panelCenter.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(panelCenter);

        PanelContainer panelContainer = new();
        panelContainer.CustomMinimumSize = new Vector2(PanelWidth, 500f);

        StyleBoxFlat panelStyle = new();
        panelStyle.BgColor = PanelBgColor;
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = new Color(0.3f, 0.25f, 0.2f, 1f);
        panelStyle.SetContentMarginAll(16);
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
        panelCenter.AddChild(panelContainer);

        VBoxContainer mainLayout = new();
        mainLayout.AddThemeConstantOverride("separation", 12);
        panelContainer.AddChild(mainLayout);

        // Header
        Label header = new();
        header.Text = "Select World";
        header.HorizontalAlignment = HorizontalAlignment.Center;
        header.AddThemeColorOverride("font_color", HeaderColor);
        header.AddThemeFontSizeOverride("font_size", HeaderFontSize);
        mainLayout.AddChild(header);

        // Separator
        HSeparator separator = new();
        mainLayout.AddChild(separator);

        // World list section
        _worldListContainer = new VBoxContainer();
        _worldListContainer.AddThemeConstantOverride("separation", 4);
        mainLayout.AddChild(_worldListContainer);

        // Separator
        HSeparator separator2 = new();
        mainLayout.AddChild(separator2);

        // Create world section
        Label createHeader = new();
        createHeader.Text = "Create New World";
        createHeader.AddThemeColorOverride("font_color", HeaderColor);
        createHeader.AddThemeFontSizeOverride("font_size", 20);
        mainLayout.AddChild(createHeader);

        // World name
        HBoxContainer nameRow = new();
        nameRow.AddThemeConstantOverride("separation", 8);
        mainLayout.AddChild(nameRow);

        Label nameLabel = new();
        nameLabel.Text = "Name:";
        nameLabel.CustomMinimumSize = new Vector2(60f, 0f);
        nameLabel.AddThemeFontSizeOverride("font_size", LabelFontSize);
        nameRow.AddChild(nameLabel);

        _nameField = new LineEdit();
        _nameField.PlaceholderText = "New World";
        _nameField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _nameField.CustomMinimumSize = new Vector2(0f, FieldHeight);
        nameRow.AddChild(_nameField);

        // Seed
        HBoxContainer seedRow = new();
        seedRow.AddThemeConstantOverride("separation", 8);
        mainLayout.AddChild(seedRow);

        Label seedLabel = new();
        seedLabel.Text = "Seed:";
        seedLabel.CustomMinimumSize = new Vector2(60f, 0f);
        seedLabel.AddThemeFontSizeOverride("font_size", LabelFontSize);
        seedRow.AddChild(seedLabel);

        _seedField = new LineEdit();
        _seedField.PlaceholderText = "Leave empty for random";
        _seedField.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _seedField.CustomMinimumSize = new Vector2(0f, FieldHeight);
        seedRow.AddChild(_seedField);

        // Buttons row
        HBoxContainer buttonRow = new();
        buttonRow.AddThemeConstantOverride("separation", 8);
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        mainLayout.AddChild(buttonRow);

        Button createButton = new();
        createButton.Text = "Create & Play";
        createButton.CustomMinimumSize = new Vector2(160f, ButtonHeight);
        createButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        createButton.Pressed += OnCreatePressed;
        buttonRow.AddChild(createButton);

        Button backButton = new();
        backButton.Text = "Back";
        backButton.CustomMinimumSize = new Vector2(120f, ButtonHeight);
        backButton.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        backButton.Pressed += OnBackPressed;
        buttonRow.AddChild(backButton);

        RefreshWorldList();

        _logger.Info("WorldSelectionPanelNode ready.");
    }

    /// <summary>
    /// Refreshes the list of saved worlds from disk.
    /// </summary>
    public void RefreshWorldList()
    {
        // Clear existing entries
        foreach (Node child in _worldListContainer.GetChildren())
        {
            child.QueueFree();
        }

        IReadOnlyList<WorldMeta> worlds = _worldRepository.ListAll(_savesRoot);

        if (worlds.Count == 0)
        {
            Label emptyLabel = new();
            emptyLabel.Text = "No saved worlds found.";
            emptyLabel.AddThemeColorOverride("font_color", SubTextColor);
            emptyLabel.AddThemeFontSizeOverride("font_size", LabelFontSize);
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _worldListContainer.AddChild(emptyLabel);
            return;
        }

        foreach (WorldMeta world in worlds)
        {
            Button worldButton = new();
            worldButton.Text = $"{world.Name}  (seed: {world.Seed})";
            worldButton.CustomMinimumSize = new Vector2(0f, 40f);
            worldButton.AddThemeFontSizeOverride("font_size", LabelFontSize);

            StyleBoxFlat worldStyle = new();
            worldStyle.BgColor = WorldEntryColor;
            worldStyle.SetBorderWidthAll(1);
            worldStyle.BorderColor = new Color(0.3f, 0.25f, 0.2f, 0.8f);
            worldStyle.SetContentMarginAll(6);
            worldButton.AddThemeStyleboxOverride("normal", worldStyle);

            StyleBoxFlat hoverStyle = new();
            hoverStyle.BgColor = WorldEntryHoverColor;
            hoverStyle.SetBorderWidthAll(1);
            hoverStyle.BorderColor = new Color(0.5f, 0.45f, 0.35f, 0.9f);
            hoverStyle.SetContentMarginAll(6);
            worldButton.AddThemeStyleboxOverride("hover", hoverStyle);

            WorldMeta capturedWorld = world;
            worldButton.Pressed += () => OnWorldSelected(capturedWorld);
            _worldListContainer.AddChild(worldButton);
        }
    }

    private void OnWorldSelected(WorldMeta world)
    {
        _logger.Info("WorldSelectionPanel: Selected world '{0}' (seed={1})", world.Name, world.Seed);
        _eventBus.Publish(new WorldLoadRequestedEvent { Meta = world });
    }

    private void OnCreatePressed()
    {
        string worldName = _nameField.Text.Trim();

        if (string.IsNullOrWhiteSpace(worldName))
        {
            worldName = "New World";
        }

        int seed;

        if (string.IsNullOrWhiteSpace(_seedField.Text))
        {
            seed = (int)(GD.Randi() & 0x7FFFFFFF);
        }
        else if (!int.TryParse(_seedField.Text.Trim(), out seed))
        {
            // Use string hash as seed for non-numeric input
            seed = _seedField.Text.Trim().GetHashCode(StringComparison.Ordinal);
        }

        WorldMeta meta = new()
        {
            Name = worldName,
            Seed = seed,
            CreatedAt = DateTime.UtcNow,
            LastPlayedAt = DateTime.UtcNow,
        };

        _worldRepository.SaveMeta(_savesRoot, meta);
        _logger.Info("WorldSelectionPanel: Created world '{0}' (seed={1})", worldName, seed);
        _eventBus.Publish(new WorldLoadRequestedEvent { Meta = meta });
    }

    private void OnBackPressed()
    {
        EmitSignal(SignalName.BackRequested);
    }
}
