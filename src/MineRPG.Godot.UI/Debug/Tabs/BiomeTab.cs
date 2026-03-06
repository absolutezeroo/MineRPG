#if DEBUG
using System.Collections.Generic;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 4: Biomes and Climate. Displays current biome information,
/// climate parameters, and biome coverage statistics.
/// </summary>
public sealed partial class BiomeTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 512;

    private readonly IDebugDataProvider _debugData;
    private readonly IChunkDebugProvider? _chunkDebugProvider;
    private readonly StringBuilder _builder = new(StringBuilderCapacity);
    private readonly List<ChunkStateEntry> _chunkBuffer = new(2048);
    private readonly Dictionary<string, int> _biomeCounts = new();

    private Label _currentBiomeLabel = null!;
    private Label _coverageLabel = null!;

    private double _coverageUpdateTimer;

    /// <summary>
    /// Creates the biome tab.
    /// </summary>
    public BiomeTab(IDebugDataProvider debugData, IChunkDebugProvider? chunkDebugProvider)
    {
        _debugData = debugData;
        _chunkDebugProvider = chunkDebugProvider;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        // -- Current biome --
        DebugSection currentSection = new("Current Location");
        AddChild(currentSection);

        _currentBiomeLabel = new Label();
        DebugTheme.ApplyLabelStyle(_currentBiomeLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        currentSection.Content.AddChild(_currentBiomeLabel);

        // -- Coverage stats --
        DebugSection coverageSection = new("Biome Coverage");
        AddChild(coverageSection);

        _coverageLabel = new Label();
        DebugTheme.ApplyLabelStyle(_coverageLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        coverageSection.Content.AddChild(_coverageLabel);
    }

    /// <inheritdoc />
    public void UpdateDisplay(double delta)
    {
        UpdateCurrentBiome();
        UpdateCoverage(delta);
    }

    private void UpdateCurrentBiome()
    {
        _builder.Clear();
        _builder.Append("Biome: ").Append(_debugData.CurrentBiome).AppendLine();
        _builder.Append("Position: ")
            .Append(_debugData.PlayerX.ToString("F1")).Append(", ")
            .Append(_debugData.PlayerY.ToString("F1")).Append(", ")
            .Append(_debugData.PlayerZ.ToString("F1")).AppendLine();
        _builder.Append("Chunk: (")
            .Append(_debugData.ChunkX).Append(", ")
            .Append(_debugData.ChunkZ).Append(')');
        _currentBiomeLabel.Text = _builder.ToString();
    }

    private void UpdateCoverage(double delta)
    {
        // Update coverage every 2 seconds to reduce overhead
        _coverageUpdateTimer += delta;

        if (_coverageUpdateTimer < 2.0)
        {
            return;
        }

        _coverageUpdateTimer = 0;

        if (_chunkDebugProvider is null)
        {
            _coverageLabel.Text = "No chunk data available.";
            return;
        }

        _chunkBuffer.Clear();
        _chunkDebugProvider.GetAllChunkStates(_chunkBuffer);
        _biomeCounts.Clear();

        int totalWithBiome = 0;

        foreach (ChunkStateEntry entry in _chunkBuffer)
        {
            if (!_chunkDebugProvider.TryGetChunkDebugInfo(entry.ChunkX, entry.ChunkZ, out ChunkDebugInfo info))
            {
                continue;
            }

            if (string.IsNullOrEmpty(info.BiomeName))
            {
                continue;
            }

            totalWithBiome++;

            if (_biomeCounts.TryGetValue(info.BiomeName, out int count))
            {
                _biomeCounts[info.BiomeName] = count + 1;
            }
            else
            {
                _biomeCounts[info.BiomeName] = 1;
            }
        }

        _builder.Clear();
        _builder.Append("Total chunks with biome: ").Append(totalWithBiome).AppendLine();

        if (totalWithBiome == 0)
        {
            _coverageLabel.Text = _builder.ToString();
            return;
        }

        // Sort by count descending - use simple insertion approach
        List<KeyValuePair<string, int>> sorted = new(_biomeCounts);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        int maxDisplay = System.Math.Min(sorted.Count, 12);

        for (int i = 0; i < maxDisplay; i++)
        {
            KeyValuePair<string, int> entry = sorted[i];
            float percent = (float)entry.Value / totalWithBiome * 100f;
            _builder.Append("  ").Append(entry.Key).Append(": ")
                .Append(entry.Value).Append(" (").Append(percent.ToString("F1")).Append("%)").AppendLine();
        }

        if (sorted.Count > maxDisplay)
        {
            _builder.Append("  ... ").Append(sorted.Count - maxDisplay).Append(" more biomes");
        }

        _coverageLabel.Text = _builder.ToString();
    }
}
#endif
