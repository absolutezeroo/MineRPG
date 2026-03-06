#if DEBUG
using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Lazily creates debug panels on first toggle. Each panel is created once
/// and added as a child of the owning control node.
/// </summary>
internal sealed class DebugModuleFactory
{
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

    /// <summary>Creates a new DebugMenuPanel.</summary>
    /// <returns>The created panel.</returns>
    public DebugMenuPanel CreateDebugMenuPanel()
    {
        DebugMenuPanel panel = new(
            _debugData, _sampler, _performanceMonitor,
            _pipelineMetrics, _optimizationFlags, _eventBus, _chunkDebugProvider);
        panel.Name = "DebugMenuPanel";
        _logger.Debug("DebugModuleFactory: DebugMenuPanel created.");
        return panel;
    }

    /// <summary>Creates a new DebugHudPanel.</summary>
    /// <param name="camera">Optional camera for look direction display.</param>
    /// <returns>The created panel.</returns>
    public DebugHudPanel CreateHudPanel(Camera3D? camera)
    {
        DebugHudPanel panel = new(_debugData, _sampler, _performanceMonitor, _pipelineMetrics);
        panel.Name = "DebugHudPanel";
        panel.SetCamera(camera);
        _logger.Debug("DebugModuleFactory: DebugHudPanel created.");
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
