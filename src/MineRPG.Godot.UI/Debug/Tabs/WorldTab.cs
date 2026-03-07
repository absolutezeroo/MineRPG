#if DEBUG
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 2: World and Chunks. Displays player position, chunk info,
/// render distance, and world statistics.
/// </summary>
public sealed partial class WorldTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 256;

    private readonly IDebugDataProvider _debugData;
    private readonly PerformanceMonitor _monitor;
    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _positionLabel = null!;
    private Label _chunkInfoLabel = null!;
    private Label _worldInfoLabel = null!;

    /// <summary>
    /// Creates the world tab.
    /// </summary>
    public WorldTab(IDebugDataProvider debugData, PerformanceMonitor monitor)
    {
        _debugData = debugData;
        _monitor = monitor;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        // -- Player section --
        DebugSection playerSection = DebugSection.Create("Player");
        AddChild(playerSection);

        _positionLabel = new Label();
        DebugTheme.ApplyLabelStyle(_positionLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        playerSection.Content.AddChild(_positionLabel);

        // -- Chunk Info section --
        DebugSection chunkSection = DebugSection.Create("Chunk Info");
        AddChild(chunkSection);

        _chunkInfoLabel = new Label();
        DebugTheme.ApplyLabelStyle(_chunkInfoLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        chunkSection.Content.AddChild(_chunkInfoLabel);

        // -- World Info section --
        DebugSection worldSection = DebugSection.Create("World Info");
        AddChild(worldSection);

        _worldInfoLabel = new Label();
        DebugTheme.ApplyLabelStyle(_worldInfoLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        worldSection.Content.AddChild(_worldInfoLabel);
    }

    /// <inheritdoc />
    public void UpdateDisplay(double delta)
    {
        UpdatePositionLabel();
        UpdateChunkInfoLabel();
        UpdateWorldInfoLabel();
    }

    private void UpdatePositionLabel()
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
        _positionLabel.Text = _builder.ToString();
    }

    private void UpdateChunkInfoLabel()
    {
        _builder.Clear();
        _builder.Append("Loaded: ").Append(_debugData.LoadedChunkCount).AppendLine();
        _builder.Append("Visible: ").Append(_debugData.VisibleChunkCount).AppendLine();
        _builder.Append("In Queue: ").Append(_debugData.ChunksInQueue).AppendLine();
        _builder.Append("Render Distance: ").Append(_debugData.RenderDistance);
        _chunkInfoLabel.Text = _builder.ToString();
    }

    private void UpdateWorldInfoLabel()
    {
        _builder.Clear();
        _builder.Append("Total Vertices: ").Append(_debugData.TotalVertices.ToString("N0")).AppendLine();
        _builder.Append("Pool Idle: ").Append(_debugData.PoolIdleCount).AppendLine();
        _builder.Append("Pool Active: ").Append(_debugData.PoolActiveCount).AppendLine();
        _builder.Append("Avg Mesh Time: ").Append(_debugData.AverageMeshTimeMs.ToString("F2")).Append("ms");
        _worldInfoLabel.Text = _builder.ToString();
    }
}
#endif
