#if DEBUG
using System;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 1: Rendering and Optimizations. Displays toggles for each
/// optimization flag with real-time impact metrics.
/// </summary>
public sealed partial class RenderingTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 128;

    private readonly OptimizationFlags _flags;
    private readonly IEventBus _eventBus;
    private readonly PerformanceMonitor _monitor;
    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _impactLabel = null!;

    /// <summary>
    /// Creates the rendering tab.
    /// </summary>
    public RenderingTab(
        OptimizationFlags flags,
        IEventBus eventBus,
        PerformanceMonitor monitor)
    {
        _flags = flags;
        _eventBus = eventBus;
        _monitor = monitor;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        // -- Meshing section --
        DebugSection meshingSection = new("Meshing");
        AddChild(meshingSection);

        AddToggle(meshingSection.Content, "Greedy Meshing",
            () => _flags.GreedyMeshingEnabled,
            value => PublishFlag("GreedyMeshingEnabled", _flags.GreedyMeshingEnabled = value, false));

        AddToggle(meshingSection.Content, "Vertex AO",
            () => _flags.VertexAoEnabled,
            value => PublishFlag("VertexAoEnabled", _flags.VertexAoEnabled = value, false));

        // -- Threading section --
        DebugSection threadingSection = new("Threading");
        AddChild(threadingSection);

        AddToggle(threadingSection.Content, "Async Generation",
            () => _flags.AsyncGenerationEnabled,
            value => PublishFlag("AsyncGenerationEnabled", _flags.AsyncGenerationEnabled = value, false));

        // -- Generation section --
        DebugSection generationSection = new("Generation");
        AddChild(generationSection);

        AddToggle(generationSection.Content, "Cheese Caves",
            () => _flags.CheeseCavesEnabled,
            value => PublishFlag("CheeseCavesEnabled", _flags.CheeseCavesEnabled = value, true));

        AddToggle(generationSection.Content, "Spaghetti Caves",
            () => _flags.SpaghettiCavesEnabled,
            value => PublishFlag("SpaghettiCavesEnabled", _flags.SpaghettiCavesEnabled = value, true));

        AddToggle(generationSection.Content, "Noodle Caves",
            () => _flags.NoodleCavesEnabled,
            value => PublishFlag("NoodleCavesEnabled", _flags.NoodleCavesEnabled = value, true));

        AddToggle(generationSection.Content, "Decorators",
            () => _flags.DecoratorsEnabled,
            value => PublishFlag("DecoratorsEnabled", _flags.DecoratorsEnabled = value, true));

        AddToggle(generationSection.Content, "Ore Distribution",
            () => _flags.OreDistributionEnabled,
            value => PublishFlag("OreDistributionEnabled", _flags.OreDistributionEnabled = value, true));

        AddToggle(generationSection.Content, "Cave Features",
            () => _flags.CaveFeaturesEnabled,
            value => PublishFlag("CaveFeaturesEnabled", _flags.CaveFeaturesEnabled = value, true));

        AddToggle(generationSection.Content, "Surface Rules",
            () => _flags.SurfaceRulesEnabled,
            value => PublishFlag("SurfaceRulesEnabled", _flags.SurfaceRulesEnabled = value, true));

        AddToggle(generationSection.Content, "Biome Blending",
            () => _flags.BiomeBlendingEnabled,
            value => PublishFlag("BiomeBlendingEnabled", _flags.BiomeBlendingEnabled = value, true));

        // -- Rendering section --
        DebugSection renderSection = new("Rendering");
        AddChild(renderSection);

        AddToggle(renderSection.Content, "Fog",
            () => _flags.FogEnabled,
            value => PublishFlag("FogEnabled", _flags.FogEnabled = value, false));

        AddToggle(renderSection.Content, "Wireframe Mode",
            () => _flags.WireframeModeEnabled,
            value => PublishFlag("WireframeModeEnabled", _flags.WireframeModeEnabled = value, false));

        AddToggle(renderSection.Content, "Show Normals",
            () => _flags.ShowNormalsEnabled,
            value => PublishFlag("ShowNormalsEnabled", _flags.ShowNormalsEnabled = value, false));

        // -- Live impact --
        DebugSection impactSection = new("Live Impact");
        AddChild(impactSection);

        _impactLabel = new Label();
        DebugTheme.ApplyLabelStyle(_impactLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        impactSection.Content.AddChild(_impactLabel);

        // -- Reset button --
        DebugButton resetButton = new("Reset All to Default", ResetAllFlags);
        AddChild(resetButton);
    }

    /// <inheritdoc />
    public void UpdateDisplay()
    {
        _builder.Clear();
        _builder.Append("Vertices: ").Append(_monitor.TotalVertices.ToString("N0")).AppendLine();
        _builder.Append("Active Chunks: ").Append(_monitor.ActiveChunks).AppendLine();
        _builder.Append("Visible Chunks: ").Append(_monitor.VisibleChunks).AppendLine();
        _builder.Append("Avg Mesh Time: ").Append(_monitor.AverageMeshTimeMs.ToString("F2")).Append("ms");
        _impactLabel.Text = _builder.ToString();
    }

    private static void AddToggle(
        VBoxContainer parent,
        string label,
        Func<bool> getter,
        Action<bool> setter)
    {
        DebugToggle toggle = new(label, getter, setter);
        parent.AddChild(toggle);
    }

    private void PublishFlag(string flagName, bool newValue, bool requiresRegeneration)
    {
        _eventBus.Publish(new OptimizationFlagChangedEvent
        {
            FlagName = flagName,
            NewValue = newValue,
            RequiresRegeneration = requiresRegeneration,
        });
    }

    private void ResetAllFlags()
    {
        _flags.GreedyMeshingEnabled = true;
        _flags.VertexAoEnabled = true;
        _flags.AsyncGenerationEnabled = true;
        _flags.CheeseCavesEnabled = true;
        _flags.SpaghettiCavesEnabled = true;
        _flags.NoodleCavesEnabled = true;
        _flags.DecoratorsEnabled = true;
        _flags.OreDistributionEnabled = true;
        _flags.CaveFeaturesEnabled = true;
        _flags.SurfaceRulesEnabled = true;
        _flags.BiomeBlendingEnabled = true;
        _flags.FogEnabled = true;
        _flags.WireframeModeEnabled = false;
        _flags.ShowNormalsEnabled = false;

        _eventBus.Publish(new OptimizationFlagChangedEvent
        {
            FlagName = "All",
            NewValue = true,
            RequiresRegeneration = true,
        });
    }
}
#endif
