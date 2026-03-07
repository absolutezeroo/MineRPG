using System;
using System.Collections.Generic;
using System.IO;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Panel that lists existing saved worlds and provides a form to create new ones.
/// Layout is defined in Scenes/UI/WorldSelection.tscn; this script contains only
/// signal handlers, world list population, and event bus publishing.
/// Publishes <see cref="WorldLoadRequestedEvent"/> when a world is selected or created.
/// </summary>
public sealed partial class WorldSelectionPanelNode : Control
{
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

        GameTheme.Apply(this);

        _worldListContainer = GetNode<VBoxContainer>(
            "CenterContainer/PanelContainer/VBoxContainer/ScrollContainer/WorldList");
        _nameField = GetNode<LineEdit>(
            "CenterContainer/PanelContainer/VBoxContainer/CreateSection/CreateVBox/NameRow/NameField");
        _seedField = GetNode<LineEdit>(
            "CenterContainer/PanelContainer/VBoxContainer/CreateSection/CreateVBox/SeedRow/SeedField");

        Label header = GetNode<Label>(
            "CenterContainer/PanelContainer/VBoxContainer/Header");
        header.ThemeTypeVariation = ThemeTypeVariations.PanelTitleLabel;

        Button createButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/CreateSection/CreateVBox/CreateButton");
        createButton.Pressed += OnCreatePressed;

        Button backButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/BackButton");
        backButton.Pressed += OnBackPressed;

        RefreshWorldList();

        _logger.Info("WorldSelectionPanelNode ready.");
    }

    /// <summary>
    /// Refreshes the list of saved worlds from disk.
    /// </summary>
    public void RefreshWorldList()
    {
        foreach (Node child in _worldListContainer.GetChildren())
        {
            child.QueueFree();
        }

        IReadOnlyList<WorldMeta> worlds = _worldRepository.ListAll(_savesRoot);

        if (worlds.Count == 0)
        {
            Label emptyLabel = new();
            emptyLabel.Text = "No saved worlds found.";
            emptyLabel.ThemeTypeVariation = ThemeTypeVariations.SubduedBodyLabel;
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _worldListContainer.AddChild(emptyLabel);
            return;
        }

        foreach (WorldMeta world in worlds)
        {
            Button worldButton = new();
            worldButton.Text = $"{world.Name}  (seed: {world.Seed})";
            worldButton.CustomMinimumSize = new Vector2(0f, 40f);
            worldButton.AddThemeStyleboxOverride("normal", GameTheme.CreateWorldEntryStyle());
            worldButton.AddThemeStyleboxOverride("hover", GameTheme.CreateWorldEntryHoverStyle());

            WorldMeta capturedWorld = world;
            worldButton.Pressed += () => OnWorldSelected(capturedWorld);
            _worldListContainer.AddChild(worldButton);
        }
    }

    private void OnWorldSelected(WorldMeta world)
    {
        _logger.Info(
            "WorldSelectionPanel: Selected world '{0}' (seed={1})", world.Name, world.Seed);
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
            seed = _seedField.Text.Trim().GetHashCode(StringComparison.Ordinal);
        }

        WorldMeta meta = new()
        {
            WorldId = Guid.NewGuid().ToString("N"),
            Name = worldName,
            Seed = seed,
            CreatedAt = DateTime.UtcNow,
            LastPlayedAt = DateTime.UtcNow,
        };

        _worldRepository.SaveMeta(_savesRoot, meta);
        _logger.Info(
            "WorldSelectionPanel: Created world '{0}' (seed={1})", worldName, seed);
        _eventBus.Publish(new WorldLoadRequestedEvent { Meta = meta });
    }

    private void OnBackPressed() => EmitSignal(SignalName.BackRequested);
}
