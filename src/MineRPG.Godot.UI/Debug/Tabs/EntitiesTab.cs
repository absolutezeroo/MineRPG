#if DEBUG
using System.Text;

using Godot;

using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 5: Entities and Gameplay. Displays player information
/// and basic gameplay controls.
/// </summary>
public sealed partial class EntitiesTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 256;

    private readonly IDebugDataProvider _debugData;
    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _playerLabel = null!;

    /// <summary>
    /// Creates the entities tab.
    /// </summary>
    public EntitiesTab(IDebugDataProvider debugData)
    {
        _debugData = debugData;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        // -- Player section --
        DebugSection playerSection = new("Player");
        AddChild(playerSection);

        _playerLabel = new Label();
        DebugTheme.ApplyLabelStyle(_playerLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        playerSection.Content.AddChild(_playerLabel);

        // -- Info note --
        Label infoLabel = new();
        infoLabel.Text = "Entity controls will be added as\nentity systems are implemented.";
        DebugTheme.ApplyLabelStyle(infoLabel, DebugTheme.TextSecondary, DebugTheme.FontSizeSmall);
        AddChild(infoLabel);
    }

    /// <inheritdoc />
    public void UpdateDisplay()
    {
        _builder.Clear();
        _builder.Append("Position: ")
            .Append(_debugData.PlayerX.ToString("F1")).Append(", ")
            .Append(_debugData.PlayerY.ToString("F1")).Append(", ")
            .Append(_debugData.PlayerZ.ToString("F1")).AppendLine();
        _builder.Append("Chunk: (")
            .Append(_debugData.ChunkX).Append(", ")
            .Append(_debugData.ChunkZ).Append(')').AppendLine();
        _builder.Append("Biome: ").Append(_debugData.CurrentBiome);
        _playerLabel.Text = _builder.ToString();
    }
}
#endif
