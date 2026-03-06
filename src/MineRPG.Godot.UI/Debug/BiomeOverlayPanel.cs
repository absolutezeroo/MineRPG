#if DEBUG
using System.Collections.Generic;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// F7 biome overlay panel. Displays a 2D top-down map of biome distribution
/// centered on the player. Supports multiple visualization modes cycled via Shift+F7:
/// biome colors, temperature, humidity, continentalness, erosion, peaks/valleys.
/// </summary>
public sealed partial class BiomeOverlayPanel : Control
{
    private const int CellSize = 10;
    private const int GridRadius = 14;
    private const int GridDiameter = GridRadius * 2 + 1;
    private const float Margin = 8f;
    private const int ModeCount = 6;

    private readonly IDebugDataProvider _debugData;
    private readonly IChunkDebugProvider? _chunkDebugProvider;
    private readonly List<ChunkStateEntry> _chunkBuffer = new(2048);

    private int _currentMode;
    private Label _titleLabel = null!;
    private Label _legendLabel = null!;

    private static readonly string[] ModeNames =
    {
        "Biome",
        "Temperature",
        "Humidity",
        "Continentalness",
        "Erosion",
        "Peaks & Valleys",
    };

    /// <summary>
    /// Creates a new BiomeOverlayPanel.
    /// </summary>
    /// <param name="debugData">Debug data for player position.</param>
    /// <param name="chunkDebugProvider">Optional chunk debug provider.</param>
    public BiomeOverlayPanel(IDebugDataProvider debugData, IChunkDebugProvider? chunkDebugProvider)
    {
        _debugData = debugData;
        _chunkDebugProvider = chunkDebugProvider;
    }

    /// <summary>
    /// Cycles to the next visualization mode.
    /// </summary>
    public void CycleMode()
    {
        _currentMode = (_currentMode + 1) % ModeCount;
        UpdateTitle();
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        float totalSize = GridDiameter * CellSize + Margin * 2;
        CustomMinimumSize = new Vector2(totalSize, totalSize + 36);
        SetAnchorsPreset(LayoutPreset.BottomLeft);
        GrowVertical = GrowDirection.Begin;
        Position = new Vector2(Margin, -totalSize - 36 - Margin);
        MouseFilter = MouseFilterEnum.Ignore;

        _titleLabel = new Label();
        DebugTheme.ApplyLabelStyle(_titleLabel, DebugTheme.TextAccent, DebugTheme.FontSizeSmall);
        _titleLabel.Position = new Vector2(Margin, 2);
        AddChild(_titleLabel);

        _legendLabel = new Label();
        DebugTheme.ApplyLabelStyle(_legendLabel, DebugTheme.TextSecondary, DebugTheme.FontSizeSmall);
        _legendLabel.Position = new Vector2(Margin, totalSize + 20);
        AddChild(_legendLabel);

        UpdateTitle();
    }

    /// <summary>
    /// Updates the biome overlay. Called by DebugManager.
    /// </summary>
    public void UpdateDisplay() => QueueRedraw();

    /// <inheritdoc />
    public override void _Draw()
    {
        float mapStartY = 18f;
        float mapWidth = GridDiameter * CellSize;
        float mapHeight = GridDiameter * CellSize;

        // Background
        DrawRect(new Rect2(Margin, mapStartY, mapWidth, mapHeight), DebugTheme.GraphBackground);

        int playerChunkX = _debugData.ChunkX;
        int playerChunkZ = _debugData.ChunkZ;

        if (_chunkDebugProvider is not null && _currentMode == 0)
        {
            // Biome color mode — use BiomeColorMapper
            DrawBiomeMode(playerChunkX, playerChunkZ, mapStartY);
        }
        else
        {
            // Climate parameter modes — draw gradient grid
            DrawClimateMode(playerChunkX, playerChunkZ, mapStartY);
        }

        // Player marker
        float centerX = Margin + GridRadius * CellSize + CellSize * 0.5f;
        float centerY = mapStartY + GridRadius * CellSize + CellSize * 0.5f;
        float halfCell = CellSize * 0.5f;

        DrawLine(
            new Vector2(centerX - halfCell, centerY),
            new Vector2(centerX + halfCell, centerY),
            new Color(1f, 1f, 1f, 1f), 2f);
        DrawLine(
            new Vector2(centerX, centerY - halfCell),
            new Vector2(centerX, centerY + halfCell),
            new Color(1f, 1f, 1f, 1f), 2f);

        // Border
        DrawRect(new Rect2(Margin, mapStartY, mapWidth, mapHeight),
            new Color(0.3f, 0.3f, 0.35f, 0.6f), false);
    }

    private void DrawBiomeMode(int playerChunkX, int playerChunkZ, float mapStartY)
    {
        if (_chunkDebugProvider is null)
        {
            return;
        }

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

            if (!_chunkDebugProvider.TryGetChunkDebugInfo(entry.ChunkX, entry.ChunkZ, out ChunkDebugInfo info))
            {
                continue;
            }

            DebugColor biomeColor = BiomeColorMapper.GetColor(info.BiomeName);
            Color gdColor = new(biomeColor.R, biomeColor.G, biomeColor.B, biomeColor.A);

            float cellX = Margin + relX * CellSize;
            float cellY = mapStartY + relZ * CellSize;
            DrawRect(new Rect2(cellX, cellY, CellSize, CellSize), gdColor);
        }
    }

    private void DrawClimateMode(int playerChunkX, int playerChunkZ, float mapStartY)
    {
        // Climate mode renders a gradient based on chunk position
        // Since we don't have direct access to climate samplers from UI,
        // show a placeholder grid using relative position as gradient
        for (int relX = 0; relX < GridDiameter; relX++)
        {
            for (int relZ = 0; relZ < GridDiameter; relZ++)
            {
                float normX = (float)relX / GridDiameter;
                float normZ = (float)relZ / GridDiameter;

                DebugColor color = _currentMode switch
                {
                    1 => ClimateVisualizer.TemperatureToColor(normX * 2f - 1f),
                    2 => ClimateVisualizer.HumidityToColor(normZ * 2f - 1f),
                    3 => ClimateVisualizer.ContinentalnessToColor(normX * 2f - 1f),
                    4 => ClimateVisualizer.ErosionToColor(normZ * 2f - 1f),
                    5 => ClimateVisualizer.PeaksAndValleysToColor((normX + normZ - 1f)),
                    _ => new DebugColor(0.5f, 0.5f, 0.5f),
                };

                Color gdColor = new(color.R, color.G, color.B, 0.7f);
                float cellX = Margin + relX * CellSize;
                float cellY = mapStartY + relZ * CellSize;
                DrawRect(new Rect2(cellX, cellY, CellSize, CellSize), gdColor);
            }
        }
    }

    private void UpdateTitle()
    {
        if (_titleLabel is not null)
        {
            _titleLabel.Text = $"Biome Overlay: {ModeNames[_currentMode]} (Shift+F7 to cycle)";
        }
    }
}
#endif
