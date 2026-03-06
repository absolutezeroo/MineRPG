#if DEBUG
using System.Collections.Generic;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// 2D minimap showing chunk states as colored cells centered on the player.
/// Uses <see cref="Control._Draw"/> for efficient rendering.
/// Toggleable via F4 key.
/// </summary>
public sealed partial class ChunkMapPanel : Control
{
    private const int CellSize = 10;
    private const int GridRadius = 14;
    private const int GridDiameter = GridRadius * 2 + 1;
    private const float Margin = 8f;
    private const float TooltipMaxWidth = 200f;
    private const int MaxChunkEntries = 2048;

    private static readonly Color[] StateColors =
    {
        new(0.15f, 0.15f, 0.15f, 0.8f),  // 0 Unloaded - dark gray
        new(0.6f, 0.4f, 0.1f, 0.8f),     // 1 Queued - amber
        new(0.3f, 0.3f, 0.8f, 0.8f),     // 2 Generating - blue
        new(0.2f, 0.6f, 0.8f, 0.8f),     // 3 Generated - cyan
        new(0.8f, 0.6f, 0.2f, 0.8f),     // 4 Meshing - orange
        new(0.3f, 0.8f, 0.3f, 0.8f),     // 5 Ready - green
        new(0.8f, 0.8f, 0.2f, 0.8f),     // 6 Dirty - yellow
        new(0.5f, 0.2f, 0.2f, 0.8f),     // 7 Unloading - red
    };

    private static readonly Color BackgroundColor = new(0.05f, 0.05f, 0.08f, 0.85f);
    private static readonly Color PlayerMarkerColor = new(1f, 1f, 1f, 1f);
    private static readonly Color GridLineColor = new(0.2f, 0.2f, 0.25f, 0.4f);
    private static readonly Color BorderColor = new(0.3f, 0.3f, 0.35f, 0.6f);

    private readonly IDebugDataProvider _debugData;
    private readonly IChunkDebugProvider? _chunkDebugProvider;
    private readonly List<ChunkStateEntry> _chunkBuffer = new(MaxChunkEntries);
    private readonly StringBuilder _tooltipBuilder = new(256);

    private Label? _tooltipLabel;
    private PanelContainer? _tooltipPanel;
    private Label _titleLabel = null!;

    /// <summary>
    /// Creates a new ChunkMapPanel.
    /// </summary>
    /// <param name="debugData">Debug data provider for player position.</param>
    /// <param name="chunkDebugProvider">Optional chunk debug provider for state data.</param>
    public ChunkMapPanel(IDebugDataProvider debugData, IChunkDebugProvider? chunkDebugProvider)
    {
        _debugData = debugData;
        _chunkDebugProvider = chunkDebugProvider;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        float totalSize = GridDiameter * CellSize + Margin * 2;
        CustomMinimumSize = new Vector2(totalSize, totalSize + 20);
        SetAnchorsPreset(LayoutPreset.BottomRight);
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.Begin;
        Position = new Vector2(-totalSize - Margin, -totalSize - 20 - Margin);
        MouseFilter = MouseFilterEnum.Pass;

        _titleLabel = new Label();
        _titleLabel.Text = "Chunk Map";
        DebugTheme.ApplyLabelStyle(_titleLabel, DebugTheme.TextAccent, DebugTheme.FontSizeSmall);
        _titleLabel.Position = new Vector2(Margin, 2);
        AddChild(_titleLabel);

        CreateTooltip();
    }

    /// <summary>
    /// Updates the chunk map display. Called by DebugManager.
    /// </summary>
    public void UpdateDisplay() => QueueRedraw();

    /// <inheritdoc />
    public override void _Draw()
    {
        float mapStartY = 18f;
        float mapWidth = GridDiameter * CellSize;
        float mapHeight = GridDiameter * CellSize;

        // Background
        DrawRect(new Rect2(Margin, mapStartY, mapWidth, mapHeight), BackgroundColor);

        int playerChunkX = _debugData.ChunkX;
        int playerChunkZ = _debugData.ChunkZ;

        // Draw chunk cells
        if (_chunkDebugProvider is not null)
        {
            _chunkBuffer.Clear();
            _chunkDebugProvider.GetAllChunkStates(_chunkBuffer);

            for (int i = 0; i < _chunkBuffer.Count; i++)
            {
                ChunkStateEntry entry = _chunkBuffer[i];
                int relX = entry.ChunkX - playerChunkX + GridRadius;
                int relZ = entry.ChunkZ - playerChunkZ + GridRadius;

                if (relX < 0 || relX >= GridDiameter || relZ < 0 || relZ >= GridDiameter)
                {
                    continue;
                }

                int stateIndex = entry.StateIndex;

                if (stateIndex < 0 || stateIndex >= StateColors.Length)
                {
                    stateIndex = 0;
                }

                Color color = StateColors[stateIndex];
                float cellX = Margin + relX * CellSize;
                float cellY = mapStartY + relZ * CellSize;

                DrawRect(new Rect2(cellX, cellY, CellSize, CellSize), color);
            }
        }

        // Grid lines
        for (int i = 0; i <= GridDiameter; i++)
        {
            float x = Margin + i * CellSize;
            DrawLine(new Vector2(x, mapStartY), new Vector2(x, mapStartY + mapHeight), GridLineColor);

            float y = mapStartY + i * CellSize;
            DrawLine(new Vector2(Margin, y), new Vector2(Margin + mapWidth, y), GridLineColor);
        }

        // Border
        DrawRect(new Rect2(Margin, mapStartY, mapWidth, mapHeight), BorderColor, false);

        // Player marker (center cross)
        float centerX = Margin + GridRadius * CellSize + CellSize * 0.5f;
        float centerY = mapStartY + GridRadius * CellSize + CellSize * 0.5f;
        float halfCell = CellSize * 0.5f;

        DrawLine(
            new Vector2(centerX - halfCell, centerY),
            new Vector2(centerX + halfCell, centerY),
            PlayerMarkerColor, 2f);
        DrawLine(
            new Vector2(centerX, centerY - halfCell),
            new Vector2(centerX, centerY + halfCell),
            PlayerMarkerColor, 2f);
    }

    /// <inheritdoc />
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion)
        {
            return;
        }

        if (_chunkDebugProvider is null || _tooltipPanel is null || _tooltipLabel is null)
        {
            return;
        }

        float mapStartY = 18f;
        Vector2 localPos = motion.Position;
        float relX = localPos.X - Margin;
        float relY = localPos.Y - mapStartY;

        int cellX = (int)(relX / CellSize);
        int cellZ = (int)(relY / CellSize);

        if (cellX < 0 || cellX >= GridDiameter || cellZ < 0 || cellZ >= GridDiameter)
        {
            _tooltipPanel.Visible = false;
            return;
        }

        int chunkX = _debugData.ChunkX + cellX - GridRadius;
        int chunkZ = _debugData.ChunkZ + cellZ - GridRadius;

        if (_chunkDebugProvider.TryGetChunkDebugInfo(chunkX, chunkZ, out ChunkDebugInfo info))
        {
            _tooltipBuilder.Clear();
            _tooltipBuilder.Append("Chunk: ").Append(info.ChunkX).Append(", ").Append(info.ChunkZ).AppendLine();
            _tooltipBuilder.Append("State: ").Append(info.StateName).AppendLine();
            _tooltipBuilder.Append("Biome: ").Append(info.BiomeName).AppendLine();
            _tooltipBuilder.Append("Height: ").Append(info.MinBlockY).Append('-').Append(info.MaxBlockY).AppendLine();
            _tooltipBuilder.Append("Vertices: ").Append(info.VertexCount).AppendLine();
            _tooltipBuilder.Append("Gen: ").Append(info.GenerationTimeMs.ToString("F1")).Append("ms").AppendLine();
            _tooltipBuilder.Append("Mesh: ").Append(info.MeshTimeMs.ToString("F1")).Append("ms");

            if (info.IsModified)
            {
                _tooltipBuilder.AppendLine().Append("[Modified]");
            }

            _tooltipLabel.Text = _tooltipBuilder.ToString();
            _tooltipPanel.Visible = true;
            _tooltipPanel.Position = localPos + new Vector2(12, 12);
        }
        else
        {
            _tooltipBuilder.Clear();
            _tooltipBuilder.Append("Chunk: ").Append(chunkX).Append(", ").Append(chunkZ).AppendLine();
            _tooltipBuilder.Append("State: Unloaded");
            _tooltipLabel.Text = _tooltipBuilder.ToString();
            _tooltipPanel.Visible = true;
            _tooltipPanel.Position = localPos + new Vector2(12, 12);
        }
    }

    private void CreateTooltip()
    {
        _tooltipPanel = new PanelContainer();
        _tooltipPanel.AddThemeStyleboxOverride("panel", DebugTheme.CreatePanelStyle());
        _tooltipPanel.MouseFilter = MouseFilterEnum.Ignore;
        _tooltipPanel.Visible = false;
        _tooltipPanel.CustomMinimumSize = new Vector2(TooltipMaxWidth, 0);
        AddChild(_tooltipPanel);

        _tooltipLabel = new Label();
        DebugTheme.ApplyLabelStyle(_tooltipLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        _tooltipPanel.AddChild(_tooltipLabel);
    }
}
#endif
