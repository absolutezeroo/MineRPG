using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Placeholder equipment screen. Will host the full equipment UI in a future milestone.
/// </summary>
public sealed partial class EquipmentScreenNode : Control
{
    private ILogger _logger = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        Button closeButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/CloseButton");
        closeButton.Pressed += OnClosePressed;

        _logger.Info("EquipmentScreenNode ready.");
    }

    private void OnClosePressed()
    {
        Visible = false;
    }
}
