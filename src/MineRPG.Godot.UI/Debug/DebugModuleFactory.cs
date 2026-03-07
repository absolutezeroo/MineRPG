#if DEBUG
using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Lazily creates debug panels on first toggle. Scene-based panels are
/// instantiated from PackedScene and configured via SetDependencies.
/// Code-only panels (custom _Draw) are still created with new().
/// </summary>
internal sealed class DebugModuleFactory
{
    private const string DebugMenuScenePath = "res://Scenes/UI/Debug/DebugMenuPanel.tscn";
    private const string DebugHudScenePath = "res://Scenes/UI/Debug/DebugHudPanel.tscn";

    private readonly IDebugDataProvider _debugData;
    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly PipelineMetrics _pipelineMetrics;
    private readonly OptimizationFlags _optimizationFlags;
    private readonly IEventBus _eventBus;
    private readonly IChunkDebugProvider? _chunkDebugProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates the module factory with all required dependencies.
    /// </summary>
    /// <param name="debugData">Debug data provider for world/player info.</param>
    /// <param name="sampler">Performance sampler for frame timing.</param>
    /// <param name="performanceMonitor">Core performance metrics.</param>
    /// <param name="pipelineMetrics">Extended pipeline metrics.</param>
    /// <param name="optimizationFlags">Optimization flags for rendering toggles.</param>
    /// <param name="eventBus">Event bus for publishing debug events.</param>
    /// <param name="chunkDebugProvider">Optional chunk debug provider.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public DebugModuleFactory(
        IDebugDataProvider debugData,
        PerformanceSampler sampler,
        PerformanceMonitor performanceMonitor,
        PipelineMetrics pipelineMetrics,
        OptimizationFlags optimizationFlags,
        IEventBus eventBus,
        IChunkDebugProvider? chunkDebugProvider,
        ILogger logger)
    {
        _debugData = debugData;
        _sampler = sampler;
        _performanceMonitor = performanceMonitor;
        _pipelineMetrics = pipelineMetrics;
        _optimizationFlags = optimizationFlags;
        _eventBus = eventBus;
        _chunkDebugProvider = chunkDebugProvider;
        _logger = logger;
    }

    /// <summary>Creates a new DebugMenuPanel from scene.</summary>
    /// <returns>The created panel.</returns>
    public DebugMenuPanel CreateDebugMenuPanel()
    {
        PackedScene scene = GD.Load<PackedScene>(DebugMenuScenePath);
        DebugMenuPanel panel = scene.Instantiate<DebugMenuPanel>();
        panel.Name = "DebugMenuPanel";
        panel.SetDependencies(
            _debugData, _sampler, _performanceMonitor,
            _pipelineMetrics, _optimizationFlags, _eventBus, _chunkDebugProvider);
        _logger.Debug("DebugModuleFactory: DebugMenuPanel created from scene.");
        return panel;
    }

    /// <summary>Creates a new DebugHudPanel from scene.</summary>
    /// <param name="camera">Optional camera for look direction display.</param>
    /// <returns>The created panel.</returns>
    public DebugHudPanel CreateHudPanel(Camera3D? camera)
    {
        PackedScene scene = GD.Load<PackedScene>(DebugHudScenePath);
        DebugHudPanel panel = scene.Instantiate<DebugHudPanel>();
        panel.Name = "DebugHudPanel";
        panel.SetDependencies(_debugData, _sampler, _performanceMonitor, _pipelineMetrics);
        panel.SetCamera(camera);
        _logger.Debug("DebugModuleFactory: DebugHudPanel created from scene.");
        return panel;
    }

    /// <summary>Creates a new ChunkMapPanel.</summary>
    /// <returns>The created panel.</returns>
    public ChunkMapPanel CreateChunkMapPanel()
    {
        ChunkMapPanel panel = new(_debugData, _chunkDebugProvider);
        panel.Name = "ChunkMapPanel";
        _logger.Debug("DebugModuleFactory: ChunkMapPanel created.");
        return panel;
    }

    /// <summary>Creates a new BiomeOverlayPanel.</summary>
    /// <returns>The created panel.</returns>
    public BiomeOverlayPanel CreateBiomeOverlayPanel()
    {
        BiomeOverlayPanel panel = new(_debugData, _chunkDebugProvider);
        panel.Name = "BiomeOverlayPanel";
        _logger.Debug("DebugModuleFactory: BiomeOverlayPanel created.");
        return panel;
    }

    /// <summary>Creates a new PerformanceGraphPanel.</summary>
    /// <returns>The created panel.</returns>
    public PerformanceGraphPanel CreatePerfGraphPanel()
    {
        PerformanceGraphPanel panel = new(_sampler);
        panel.Name = "PerformanceGraphPanel";
        _logger.Debug("DebugModuleFactory: PerformanceGraphPanel created.");
        return panel;
    }

    /// <summary>Creates a new BlockInspectorPanel.</summary>
    /// <param name="camera">Optional camera for ray direction.</param>
    /// <returns>The created panel.</returns>
    public BlockInspectorPanel CreateBlockInspectorPanel(Camera3D? camera)
    {
        BlockInspectorPanel panel = new();
        panel.Name = "BlockInspectorPanel";
        panel.SetCamera(camera);
        _logger.Debug("DebugModuleFactory: BlockInspectorPanel created.");
        return panel;
    }
}
#endif
