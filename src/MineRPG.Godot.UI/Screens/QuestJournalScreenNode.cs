using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Placeholder quest journal screen. Will host the full quest UI in a future milestone.
/// </summary>
public sealed partial class QuestJournalScreenNode : Control
{
    private ILogger _logger = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        Button closeButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/CloseButton");
        closeButton.Pressed += OnClosePressed;

        _logger.Info("QuestJournalScreenNode ready.");
    }

    private void OnClosePressed() => Visible = false;
}
