#if DEBUG
using System;
using System.Text;

using Godot;
using Godot.Collections;

using MineRPG.Core.Diagnostics;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 6: System. Displays engine info, GC controls, and debug module status.
/// </summary>
public sealed partial class SystemTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 512;

    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _monitor;
    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _engineLabel = null!;
    private Label _gcLabel = null!;

    /// <summary>
    /// Creates the system tab.
    /// </summary>
    public SystemTab(PerformanceSampler sampler, PerformanceMonitor monitor)
    {
        _sampler = sampler;
        _monitor = monitor;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        // -- Engine info (static, set once) --
        DebugSection engineSection = new("Engine");
        AddChild(engineSection);

        _engineLabel = new Label();
        DebugTheme.ApplyLabelStyle(_engineLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        engineSection.Content.AddChild(_engineLabel);

        BuildEngineInfo();

        // -- GC Control --
        DebugSection gcSection = new("GC Control");
        AddChild(gcSection);

        DebugButton gcButton = new("Force GC Collect", ForceGarbageCollection);
        gcSection.Content.AddChild(gcButton);

        _gcLabel = new Label();
        DebugTheme.ApplyLabelStyle(_gcLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        gcSection.Content.AddChild(_gcLabel);

        // -- Debug info --
        DebugSection debugSection = new("Debug Modules");
        AddChild(debugSection);

        Label debugInfo = new();
        debugInfo.Text = "F1 — Debug Menu\n"
            + "F3 — HUD Overlay\n"
            + "F4 — Chunk Map\n"
            + "F5 — Chunk Borders\n"
            + "F6 — Perf Graphs\n"
            + "F7 — Biome Overlay\n"
            + "F8 — Block Inspector";
        DebugTheme.ApplyLabelStyle(debugInfo, DebugTheme.TextSecondary, DebugTheme.FontSizeSmall);
        debugSection.Content.AddChild(debugInfo);
    }

    /// <inheritdoc />
    public void UpdateDisplay()
    {
        UpdateGCInfo();
    }

    private void BuildEngineInfo()
    {
        _builder.Clear();

        Dictionary engineInfo = Engine.GetVersionInfo();
        string godotVersion = engineInfo.ContainsKey("string")
            ? engineInfo["string"].AsString()
            : "Unknown";

        _builder.Append("Godot: ").Append(godotVersion).AppendLine();
        _builder.Append("Runtime: .NET ").Append(System.Environment.Version).AppendLine();
        _builder.Append("Renderer: ").Append(RenderingServer.GetVideoAdapterName()).AppendLine();
        _builder.Append("OS: ").Append(OS.GetName()).AppendLine();
        _builder.Append("CPU Threads: ").Append(OS.GetProcessorCount());

        _engineLabel.Text = _builder.ToString();
    }

    private void UpdateGCInfo()
    {
        MemoryMetrics memory = _sampler.MemoryMetrics;

        _builder.Clear();
        _builder.Append("GC Heap: ").Append(memory.GcHeapMb.ToString("F1")).Append(" MB").AppendLine();
        _builder.Append("Gen0: ").Append(memory.Gen0Collections)
            .Append("  Gen1: ").Append(memory.Gen1Collections)
            .Append("  Gen2: ").Append(memory.Gen2Collections).AppendLine();
        _builder.Append("Chunk Data: ~").Append(memory.EstimatedChunkDataMb.ToString("F1")).Append(" MB").AppendLine();
        _builder.Append("Mesh Data: ~").Append(memory.EstimatedMeshDataMb.ToString("F1")).Append(" MB");
        _gcLabel.Text = _builder.ToString();
    }

    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
#endif
